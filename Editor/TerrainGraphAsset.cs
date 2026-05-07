using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[CreateAssetMenu(fileName = "New Terrain Graph", menuName = "Terrain Graph/Graph Asset")]
public class TerrainGraphAsset : ScriptableObject
{
    // ==========================================
    // PHASE 1: AUTHORING (Designing the Graph)
    // ==========================================
    // This asset acts as the "Blueprint" or "Recipe" for the terrain.
    // At this stage, no actual terrain data exists, just a collection of instructions.

    // The collection of all serialized nodes (blocks) and wires that make up this graph.
    [SerializeReference]
    public List<GraphNode>  Nodes = new List<GraphNode>();

    public int GlobalResolution = 1024;

    [System.NonSerialized]
    public bool IsBaking = false;
    
    [System.NonSerialized]
    public Dictionary<string, float[,]> BakeCache = new Dictionary<string, float[,]>();


    // ==========================================
    // DATA RETRIEVAL (Used by Phase 2)
    // ==========================================

    /// Evaluates the graph by traversing backwards using the linked port system.
    public float[,] GetInputValue(GraphNode targetNode, int targetInputPort, int width, int height, Vector2 worldOffset, float spacing)
    {
        if (targetInputPort < 0 || targetInputPort >= targetNode.Inputs.Count)
            return new float[width, height];


        Port inputPort = targetNode.Inputs[targetInputPort];

        if (inputPort.ConnectedNode != null)
        {
            // If we are baking and the evaluator has this node's output, return it instantly!
            if (IsBaking && BakeCache != null && BakeCache.ContainsKey(inputPort.ConnectedNode.GUID))
            {
                return BakeCache[inputPort.ConnectedNode.GUID];
            }

            // Otherwise, evaluate recursively (used for Previews or if cache is missing)
            return inputPort.ConnectedNode.Evaluate(inputPort.SourcePortIndex, width, height, worldOffset, spacing, this);
        }


        // 3. No link? Return a blank map.
        return new float[width, height];
    }

    public GraphNode GetNodeByGUID(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        return Nodes.Find(n => n.GUID == guid);
    }


    /// Samples a 2D float array using Bilinear Interpolation for smooth resampling.
    public static float SampleBilinear(float[,] map, float x, float y)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        // Clamp the input coordinates to map boundaries
        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, h - 1);

        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);

        int x1 = Mathf.Min(x0 + 1, w - 1);
        int y1 = Mathf.Min(y0 + 1, h - 1);

        float tx = x - x0;
        float ty = y - y0;

        float v00 = map[x0, y0];
        float v10 = map[x1, y0];
        float v01 = map[x0, y1];
        float v11 = map[x1, y1];

        float top = Mathf.Lerp(v00, v10, tx);
        float bottom = Mathf.Lerp(v01, v11, tx);

        return Mathf.Lerp(top, bottom, ty);
    }
}

}
