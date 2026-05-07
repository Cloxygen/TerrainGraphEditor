using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class MultiplyNode : GraphNode
{
    public float Factor = 1.0f; // Made public so reflection picks it up

    public MultiplyNode(Vector2 position) : base(position, "Multiply")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Outputs.Add(new Port("Height", PortType.Heightmap));
    }

    protected override float ExtraHeight => 20f;
    protected override Color HeaderColor => new Color(0.15f, 0.35f, 0.25f, 1f);

#if UNITY_EDITOR
    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 80 * scale;
        Factor = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, rect.y, rect.width, h), "Factor", Factor);
    }
#endif

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = input[x, y] * Factor;
            }
        }

        return result;
    }
}


}
