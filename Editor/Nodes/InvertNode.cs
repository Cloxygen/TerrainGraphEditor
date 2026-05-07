using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class InvertNode : GraphNode
{
    public InvertNode(Vector2 position) : base(position, "Invert")
    {
        Inputs.Add(new Port("Input", PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
    }

    protected override Color HeaderColor => new Color(0.25f, 0.25f, 0.25f, 1f); // nearly black

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = 1.0f - input[x, y];
            }
        }

        return result;
    }
}

}
