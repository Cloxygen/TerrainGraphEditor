using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TerrainGraph.Authoring
{
public partial class TerrainGraphWindow
{
    // --- Rendering ---

    private void DrawEmptyState()
    {
        GUIStyle centeredLabel = new GUIStyle(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray }
        };
        
        Rect area = new Rect(0, 0, position.width, position.height);
        GUILayout.BeginArea(area);
        GUILayout.FlexibleSpace();
        GUILayout.Label("No Terrain Graph Asset Selected", centeredLabel);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Graph", GUILayout.Width(150), GUILayout.Height(30)))
        {
            CreateNewAsset();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    private void DrawGrid(float spacing, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;

        float scaledSpacing  = spacing * Zoom;
        Vector2 screenOrigin = (Pan - Pivot) * Zoom + Pivot;
        float xOffset = screenOrigin.x % scaledSpacing;
        float yOffset = screenOrigin.y % scaledSpacing;

        for (float x = xOffset; x < position.width; x += scaledSpacing)
            Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, position.height, 0));

        for (float y = yOffset; y < position.height; y += scaledSpacing)
            Handles.DrawLine(new Vector3(0, y, 0), new Vector3(position.width, y, 0));

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void DrawConnections()
    {
        Handles.BeginGUI();

        foreach (var node in _nodes)
        {
            if (node == null) continue;
            for (int i = 0; i < node.Inputs.Count; i++)
            {
                Port inputPort = node.Inputs[i];
                if (!string.IsNullOrEmpty(inputPort.SourceNodeGUID))
                {
                    GraphNode sourceNode = _graphAsset.GetNodeByGUID(inputPort.SourceNodeGUID);
                    if (sourceNode != null && inputPort.SourcePortIndex >= 0 && inputPort.SourcePortIndex < sourceNode.Outputs.Count)
                    {
                        Vector2 from = GraphToScreen(sourceNode.GetOutputPortPos(inputPort.SourcePortIndex));
                        Vector2 to   = GraphToScreen(node.GetInputPortPos(i));
                        Color color  = GraphNode.GetPortColor(sourceNode.Outputs[inputPort.SourcePortIndex].Type);
                        DrawBezier(from, to, color);
                    }

                }
            }
        }

        if (_pendingFromNode != null && _pendingFromPort >= 0 && _pendingFromPort < _pendingFromNode.Outputs.Count)
        {
            Vector2 from  = GraphToScreen(_pendingFromNode.GetOutputPortPos(_pendingFromPort));
            Color   color = GraphNode.GetPortColor(_pendingFromNode.Outputs[_pendingFromPort].Type);
            DrawBezier(from, _pendingMousePos, color * new Color(1f, 1f, 1f, 0.6f));
        }


        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void DrawBezier(Vector2 start, Vector2 end, Color color)
    {
        float dx = Mathf.Clamp(Mathf.Abs(end.x - start.x) * 0.5f, 30f, 200f);
        Vector3 startTan = new Vector3(start.x + dx, start.y, 0f);
        Vector3 endTan   = new Vector3(end.x   - dx, end.y,   0f);
        Handles.DrawBezier(start, end, startTan, endTan, color, null, 2f);
    }

    private void DrawNodes()
    {
        foreach (GraphNode node in _nodes)
        {
            if (node == null) continue;
            Rect screenRect = GraphToScreenRect(node.Rect);
            node.Draw(screenRect);
        }
    }
}

}
