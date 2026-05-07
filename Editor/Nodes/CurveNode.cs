using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class CurveNode : GraphNode
{
    public AnimationCurve Curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public CurveNode(Vector2 position) : base(position, "Curve")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Outputs.Add(new Port("Height", PortType.Heightmap));
    }

    protected override float ExtraHeight => 20f;
    protected override Color HeaderColor => new Color(0.40f, 0.20f, 0.40f, 1f);

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 50 * scale;
        Curve = UnityEditor.EditorGUI.CurveField(new Rect(rect.x, rect.y, rect.width, h), "Curve", Curve);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = Curve.Evaluate(input[x, y]);
            }
        }

        return result;
    }
}

}
