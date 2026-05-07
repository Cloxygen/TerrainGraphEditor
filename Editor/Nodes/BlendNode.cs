using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class BlendNode : GraphNode
{
    public BlendNode(Vector2 position) : base(position, "Blend")
    {
        Inputs.Add(new Port("Height A", PortType.Heightmap));
        Inputs.Add(new Port("Height B", PortType.Heightmap));
        Inputs.Add(new Port("Mask", PortType.Mask));
        Outputs.Add(new Port("Height", PortType.Heightmap));
    }

    protected override Color HeaderColor => new Color(0.35f, 0.20f, 0.45f, 1f); // purple

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] mapA = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] mapB = graph.GetInputValue(this, 1, width, height, worldOffset, spacing);
        float[,] mask = graph.GetInputValue(this, 2, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];



        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = Mathf.Lerp(mapA[x, y], mapB[x, y], Mathf.Clamp01(mask[x, y]));
            }
        }

        return result;
    }
}

}
