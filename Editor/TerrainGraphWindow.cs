using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;


namespace TerrainGraph.Authoring
{
public partial class TerrainGraphWindow : EditorWindow
{
    private const float MinorGridSpacing = 20f;
    private const float MajorGridSpacing = 100f;
    private const float MinZoom          = 0.1f;
    private const float MaxZoom          = 2.0f;
    private const float ZoomSpeed        = 0.05f;
    private const string PrefKey         = "TerrainGraph_ActiveAsset";

    private Vector2             _panOffset;
    private float               _zoom = 1.0f;
    private TerrainGraphAsset   _graphAsset;
    private List<GraphNode>     _nodes => _graphAsset?.Nodes;

    private GraphNode           _activeNode;    // null after mouse release
    private GraphNode           _selectedNode;  // persists for keyboard actions

    // Pending connection (drag in progress)
    private GraphNode _pendingFromNode;
    private int       _pendingFromPort;   // output port index
    private Vector2   _pendingMousePos;   // screen space

    // --- View State ---
    private Vector2 Pivot => position.size / 2f;
    private float   Zoom  => _zoom;
    private Vector2 Pan   => _panOffset;

    public static TerrainGraphWindow Instance;

    [MenuItem("Tools/Terrain Graph")]
    public static void Open()
    {
        Instance = GetWindow<TerrainGraphWindow>("Terrain Graph");
    }

    public static TerrainGraphAsset GetActiveGraph() => Instance?._graphAsset;


    private void OnEnable()
    {
        Instance = this;
        // Load the last active asset from EditorPrefs
        string guid = EditorPrefs.GetString(PrefKey, "");
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                _graphAsset = AssetDatabase.LoadAssetAtPath<TerrainGraphAsset>(path);
            }
        }

        if (_graphAsset != null)
        {
            PruneConnections();
            MigrateAsset();
        }
    }

    private void MigrateAsset()
    {
        if (_graphAsset == null) return;
        bool changed = false;

        foreach (var node in _nodes)
        {
            if (node == null) continue;
            if (string.IsNullOrEmpty(node.GUID))
            {
                node.GUID = System.Guid.NewGuid().ToString();
                changed = true;
            }
        }

        if (changed) EditorUtility.SetDirty(_graphAsset);
    }

    private void OnGUI()
    {
        if (_graphAsset != null)
        {
            EditorGUI.BeginChangeCheck();

            DrawGrid(MinorGridSpacing, new Color(1f, 1f, 1f, 0.08f));
            DrawGrid(MajorGridSpacing, new Color(1f, 1f, 1f, 0.18f));
            DrawConnections();
            DrawNodes();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_graphAsset);
                TriggerPreviewUpdate();
            }
        }

        // Draw toolbar on top
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Button("Create New Graph", EditorStyles.toolbarButton))
        {
            CreateNewAsset();
        }

        GUILayout.Space(10);
        
        EditorGUI.BeginChangeCheck();
        _graphAsset = (TerrainGraphAsset)EditorGUILayout.ObjectField(_graphAsset, typeof(TerrainGraphAsset), false, GUILayout.Width(250));
        if (EditorGUI.EndChangeCheck())
        {
            if (_graphAsset != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_graphAsset));
                EditorPrefs.SetString(PrefKey, guid);
                PruneConnections();
                MigrateAsset();
                TriggerPreviewUpdate();
            }
            else
            {
                EditorPrefs.SetString(PrefKey, "");
            }
        }

        GUILayout.FlexibleSpace();

        if (_graphAsset != null)
        {
            // 1. Resolution Dropdown
            string[] resLabels = { "128", "256", "512", "1024", "2048", "4096" };
            int[] resValues = { 128, 256, 512, 1024, 2048, 4096 };
            int resIndex = System.Array.IndexOf(resValues, _graphAsset.GlobalResolution);
            if (resIndex == -1) resIndex = 2; // Default 512
            
            GUILayout.Label("Global Res:", EditorStyles.miniLabel);
            int nextResIndex = EditorGUILayout.Popup(resIndex, resLabels, EditorStyles.toolbarDropDown, GUILayout.Width(60));
            if (nextResIndex != resIndex)
            {
                _graphAsset.GlobalResolution = resValues[nextResIndex];
                RefreshGraph();
            }

            if (GUILayout.Button("Manual Refresh", EditorStyles.toolbarButton))
            {
                RefreshGraph();
            }

            if (GUILayout.Button("Bake Data Map", EditorStyles.toolbarButton))
            {
                UpdateDataMap();
            }
        }
        GUILayout.EndHorizontal();

        if (_graphAsset == null)
        {
            DrawEmptyState();
            return;
        }

        // Handle graph dragging and selection AFTER rendering,
        // so that Editor fields (like sliders) can consume clicks first.
        Event e = Event.current;
        if (e.isMouse && e.mousePosition.y < EditorStyles.toolbar.fixedHeight)
        {
            // Ignore toolbar
        }
        else
        {
            HandleInputs();
        }

        if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.ScrollWheel)
            Repaint();
    }

    private void TriggerPreviewUpdate()
    {
        if (_graphAsset == null) return;

        GetThumbnailContext(out Vector2 offset, out float spacing);
        foreach (var node in _nodes)
        {
            if (node == null) continue;
            node.ComputePreview(_graphAsset, offset, spacing);
        }
    }

    private void GetThumbnailContext(out Vector2 offset, out float spacing)
    {
        offset = Vector2.zero;
        spacing = 1000f / 128f;
    }

    /// Refresh the graph UI and previews. Does NOT update the scene mesh.
    public void RefreshGraph()
    {
        if (_graphAsset == null) return;
        
        // Ensure direct pointers are resolved for O(1) preview evaluation
        foreach (var node in _graphAsset.Nodes)
        {
            if (node == null) continue;
            foreach (var port in node.Inputs)
                port.ConnectedNode = _graphAsset.GetNodeByGUID(port.SourceNodeGUID);
        }

        EditorUtility.SetDirty(_graphAsset);
        TriggerPreviewUpdate();
        Repaint();
    }

    private TerrainGraph.Baking.TerrainBaker _activeEvaluator;

    /// Performs a full evaluation of the graph and updates the TerrainDataMap.
    public void UpdateDataMap()
    {
        if (_graphAsset == null || _activeEvaluator != null) return;
        
        RefreshGraph();

        _activeEvaluator = new TerrainGraph.Baking.TerrainBaker(_graphAsset);
        _activeEvaluator.BeginBake();

        if (_activeEvaluator.Status == TerrainGraph.Baking.TerrainBaker.BakeStatus.Calculating)
        {
            EditorApplication.update += EditorUpdateBake;
        }
        else
        {
            _activeEvaluator = null;
        }
    }

    private void EditorUpdateBake()
    {
        if (_activeEvaluator == null)
        {
            EditorApplication.update -= EditorUpdateBake;
            EditorUtility.ClearProgressBar();
            return;
        }

        bool cancel = EditorUtility.DisplayCancelableProgressBar(
            "Baking Data Map", 
            _activeEvaluator.CurrentActivity, 
            _activeEvaluator.Progress);
        
        if (cancel || _activeEvaluator.Tick())
        {
            EditorApplication.update -= EditorUpdateBake;
            EditorUtility.ClearProgressBar();
            _activeEvaluator = null;
            Repaint();
        }
    }







    private void PruneConnections()
    {
        if (_graphAsset == null) return;

        // Prune nodes that Unity may have partially destroyed
        _graphAsset.Nodes.RemoveAll(n => n == null);

        // Prune links that point to non-existent nodes
        foreach (var node in _nodes)
        {
            foreach (var port in node.Inputs)
            {
                if (!string.IsNullOrEmpty(port.SourceNodeGUID))
                {
                    if (_graphAsset.GetNodeByGUID(port.SourceNodeGUID) == null)
                    {
                        port.SourceNodeGUID = null;
                        port.SourcePortIndex = -1;
                    }
                }
            }
        }

        TriggerPreviewUpdate();
        Repaint();
    }

    private void CreateNewAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create New Terrain Graph", "New Terrain Graph", "asset", "Save your Terrain Graph");
        if (string.IsNullOrEmpty(path)) return;

        TerrainGraphAsset newAsset = CreateInstance<TerrainGraphAsset>();
        AssetDatabase.CreateAsset(newAsset, path);
        AssetDatabase.SaveAssets();

        _graphAsset = newAsset;
        string guid = AssetDatabase.AssetPathToGUID(path);
        EditorPrefs.SetString(PrefKey, guid);
        TriggerPreviewUpdate();
    }


}
}
