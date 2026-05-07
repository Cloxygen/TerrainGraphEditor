using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class SlopeMaskNode : GraphNode
{
    public float MinSlope = 0f;
    public float MaxSlope = 45f;
    public float Fade = 5f;

    public SlopeMaskNode(Vector2 position) : base(position, "Slope Mask")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Outputs.Add(new Port("Mask", PortType.Mask));
    }

    protected override float ExtraHeight => 60f;
    protected override Color HeaderColor => new Color(0.42f, 0.25f, 0.18f, 1f);

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 70 * scale;
        float y = rect.y;

        MinSlope = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Min Slope", MinSlope); y += s;
        MaxSlope = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Max Slope", MaxSlope); y += s;
        Fade     = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Fade",      Fade);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float hL = x > 0 ? input[x - 1, y] : input[x, y];
                float hR = x < width - 1 ? input[x + 1, y] : input[x, y];
                float hD = y > 0 ? input[x, y - 1] : input[x, y];
                float hU = y < height - 1 ? input[x, y + 1] : input[x, y];

                float dx = (hR - hL) * 0.5f;
                float dy = (hU - hD) * 0.5f;

                float slopeDerivative = Mathf.Sqrt(dx*dx + dy*dy);
                float slopeAngle = Mathf.Atan(slopeDerivative * 100f) * Mathf.Rad2Deg;

                float lowerBound = Mathf.InverseLerp(MinSlope - Fade, MinSlope, slopeAngle);
                float upperBound = 1.0f - Mathf.InverseLerp(MaxSlope, MaxSlope + Fade, slopeAngle);
                
                result[x, y] = Mathf.Min(lowerBound, upperBound);
            }
        }

        return result;
    }
}

}
