using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class NormalizeNode : GraphNode
{
    public NormalizeNode(Vector2 position) : base(position, "Normalize")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Outputs.Add(new Port("Height", PortType.Heightmap));
    }

    protected override Color HeaderColor => new Color(0.20f, 0.40f, 0.35f, 1f);

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];

        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float val = input[x, y];
                if (val < min) min = val;
                if (val > max) max = val;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = Mathf.InverseLerp(min, max, input[x, y]);
            }
        }

        return result;
    }
}


}
