using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TerrainGraph.Authoring
{
public partial class TerrainGraphWindow
{
    // --- Coordinate API ---

    private Vector2 ScreenToGraph(Vector2 screenPos)
    {
        return (screenPos - Pivot) / Zoom + Pivot - Pan;
    }

    private Vector2 GraphToScreen(Vector2 graphPos)
    {
        return (graphPos + Pan - Pivot) * Zoom + Pivot;
    }

    private Rect GraphToScreenRect(Rect graphRect)
    {
        Vector2 screenPos  = (graphRect.position + Pan - Pivot) * Zoom + Pivot;
        Vector2 screenSize = graphRect.size * Zoom;
        return new Rect(screenPos, screenSize);
    }

    // --- Input Handling ---

    private void HandleInputs()
    {
        HandleZoom();
        HandlePan();
        HandleContextMenu();
        HandlePortConnecting();
        HandleNodeDragging();
        HandleDeleteKey();
    }

    private void HandleZoom()
    {
        Event e = Event.current;
        if (e.type == EventType.ScrollWheel)
        {
            _zoom = Mathf.Clamp(_zoom - e.delta.y * ZoomSpeed * _zoom, MinZoom, MaxZoom);
            e.Use();
        }
    }

    private void HandlePan()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            _panOffset += e.delta / Zoom;
            e.Use();
        }
    }

    private void HandleContextMenu()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Vector2 graphPos = ScreenToGraph(e.mousePosition);

            GraphNode hit = null;
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (_nodes[i].Rect.Contains(graphPos))
                {
                    hit = _nodes[i];
                    break;
                }
            }

            GenericMenu menu = new GenericMenu();
            if (hit != null)
            {
                GraphNode captured = hit;
                menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(captured));
            }
            else
            {
                menu.AddItem(new GUIContent("Clean Graph/Prune Abandoned Wires"), false, PruneConnections);
                menu.AddSeparator("Clean Graph/");
                
                menu.AddItem(new GUIContent("Generators/Perlin Noise"), false, () => AddNode<PerlinNoiseNode>(graphPos));
                menu.AddItem(new GUIContent("Generators/Voronoi (Cells)"), false, () => AddNode<VoronoiNode>(graphPos));
                menu.AddSeparator("Generators/");
                
                menu.AddItem(new GUIContent("Math/Add"),               false, () => AddNode<AddNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Subtract"),          false, () => AddNode<SubtractNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Multiply"),          false, () => AddNode<MultiplyNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Power (Exponent)"),  false, () => AddNode<PowerNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Invert"),            false, () => AddNode<InvertNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Normalize"),         false, () => AddNode<NormalizeNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Blend (Lerp)"),      false, () => AddNode<BlendNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Clamp"),             false, () => AddNode<ClampNode>(graphPos));
                menu.AddItem(new GUIContent("Math/Curve"),             false, () => AddNode<CurveNode>(graphPos));
                
                menu.AddItem(new GUIContent("Filters/Directional Warp"), false, () => AddNode<DirectionalWarpNode>(graphPos));
                menu.AddItem(new GUIContent("Filters/Terrace"),          false, () => AddNode<TerraceNode>(graphPos));
                menu.AddItem(new GUIContent("Filters/Erosion/Thermal"),   false, () => AddNode<ThermalErosionNode>(graphPos));
                menu.AddItem(new GUIContent("Filters/Erosion/Flow"),      false, () => AddNode<FlowErosionNode>(graphPos));
                menu.AddItem(new GUIContent("Filters/Erosion/Slope Blur"), false, () => AddNode<SlopeBlurNode>(graphPos));
                
                menu.AddItem(new GUIContent("Masks/Height Mask"),      false, () => AddNode<HeightMaskNode>(graphPos));
                menu.AddItem(new GUIContent("Masks/Slope Mask"),       false, () => AddNode<SlopeMaskNode>(graphPos));
                
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Output/Terrain Output"),  false, () => AddNode<TerrainOutputNode>(graphPos));
            }

            menu.ShowAsContext();
            e.Use();
        }
    }

    private void AddNode<T>(Vector2 graphPos) where T : GraphNode
    {
        T node = (T)System.Activator.CreateInstance(typeof(T), graphPos);
        _nodes.Add(node);
        GetThumbnailContext(out Vector2 offset, out float spacing);
        node.ComputePreview(_graphAsset, offset, spacing);
        RefreshGraph();
    }

    private void HandleDeleteKey()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown &&
            (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) &&
            _selectedNode != null)
        {
            DeleteNode(_selectedNode);
            e.Use();
        }
    }

    private void DeleteNode(GraphNode node)
    {
        string guidToDelete = node.GUID;

        // Clear any links in other nodes pointing to this node
        foreach (var other in _nodes)
        {
            foreach (var port in other.Inputs)
            {
                if (port.SourceNodeGUID == guidToDelete)
                {
                    port.SourceNodeGUID = null;
                    port.SourcePortIndex = -1;
                }
            }
        }

        _nodes.Remove(node);
        if (_activeNode  == node) _activeNode  = null;
        if (_selectedNode == node) _selectedNode = null;
        RefreshGraph();
    }

    private void HandlePortConnecting()
    {
        Event   e        = Event.current;
        Vector2 graphPos = ScreenToGraph(e.mousePosition);

        if (e.type == EventType.MouseDown)
        {
            if (e.button == 0) // Left click: Start new connection
            {
                foreach (var node in _nodes)
                {
                    int idx = node.HitTestOutputPort(graphPos);
                    if (idx >= 0)
                    {
                        _pendingFromNode = node;
                        _pendingFromPort = idx;
                        _pendingMousePos = e.mousePosition;
                        e.Use();
                        return;
                    }
                }
            }
            else if (e.button == 1) // Right click: Disconnect port
            {
                foreach (var node in _nodes)
                {
                    int outIdx = node.HitTestOutputPort(graphPos);
                    if (outIdx >= 0)
                    {
                        // To "properly" disconnect an output, we must find every node that points to it
                        string sourceGuid = node.GUID;
                        foreach (var target in _nodes)
                        {
                            foreach (var port in target.Inputs)
                            {
                                if (port.SourceNodeGUID == sourceGuid && port.SourcePortIndex == outIdx)
                                {
                                    port.SourceNodeGUID = null;
                                    port.SourcePortIndex = -1;
                                }
                            }
                        }
                        RefreshGraph();
                        e.Use();
                        return;
                    }

                    int inIdx = node.HitTestInputPort(graphPos);
                    if (inIdx >= 0)
                    {
                        node.Inputs[inIdx].SourceNodeGUID = null;
                        node.Inputs[inIdx].SourcePortIndex = -1;
                        RefreshGraph();
                        e.Use();
                        return;
                    }
                }
            }
        }

        if (e.type == EventType.MouseDrag && _pendingFromNode != null)
        {
            _pendingMousePos = e.mousePosition;
            e.Use();
            return;
        }

        if (e.type == EventType.MouseUp && e.button == 0 && _pendingFromNode != null)
        {
            foreach (var node in _nodes)
            {
                if (node == _pendingFromNode) continue;

                int idx = node.HitTestInputPort(graphPos);
                if (idx >= 0)
                {
                    Port fromPort = _pendingFromNode.Outputs[_pendingFromPort];
                    Port toPort   = node.Inputs[idx];

                    if (fromPort.Type == toPort.Type)
                    {
                        toPort.SourceNodeGUID = _pendingFromNode.GUID;
                        toPort.SourcePortIndex = _pendingFromPort;
                        RefreshGraph();
                    }
                    break;
                }
            }

            _pendingFromNode = null;
            e.Use();
            Repaint();
        }
    }

    private void HandleNodeDragging()
    {
        Event   e        = Event.current;
        Vector2 graphPos = ScreenToGraph(e.mousePosition);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    _activeNode = null;
                    for (int i = _nodes.Count - 1; i >= 0; i--)
                    {
                        if (_nodes[i].Rect.Contains(graphPos))
                        {
                            _activeNode = _nodes[i];
                            break;
                        }
                    }

                    _selectedNode = _activeNode;
                    foreach (var node in _nodes)
                        node.IsSelected = (node == _selectedNode);

                    if (_activeNode != null)
                    {
                        _nodes.Remove(_activeNode);
                        _nodes.Add(_activeNode);
                        e.Use();
                    }
                    Repaint();
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && _activeNode != null)
                {
                    _activeNode.Rect.position += e.delta / Zoom;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0)
                {
                    if (_activeNode != null)
                        RefreshGraph();
                    _activeNode = null;
                    e.Use();
                }
                break;
        }
    }
}

}
