using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class ClampNode : GraphNode
{
    public float Min = 0.0f; // Renamed to min so label fits better
    public float Max = 1.0f;

    public ClampNode(Vector2 position) : base(position, "Clamp")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Outputs.Add(new Port("Height", PortType.Heightmap));
    }

    protected override float ExtraHeight => 40f;
    protected override Color HeaderColor => new Color(0.18f, 0.34f, 0.36f, 1f);

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 50 * scale;
        float y = rect.y;

        Min = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Min", Min); y += s;
        Max = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Max", Max);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = Mathf.Clamp(input[x, y], Min, Max);
            }
        }
        return result;
    }
}


}
