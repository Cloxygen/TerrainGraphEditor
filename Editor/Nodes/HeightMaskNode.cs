using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class HeightMaskNode : GraphNode
{
    public float MinHeight = 0.2f;
    public float MaxHeight = 0.8f;
    public float Fade = 0.1f;

    public HeightMaskNode(Vector2 position) : base(position, "Height Mask")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Outputs.Add(new Port("Mask", PortType.Mask));
    }

    protected override float ExtraHeight => 60f;
    protected override Color HeaderColor => new Color(0.42f, 0.35f, 0.18f, 1f);

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 70 * scale;
        float y = rect.y;

        MinHeight = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Min Height", MinHeight); y += s;
        MaxHeight = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Max Height", MaxHeight); y += s;
        Fade      = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Fade",       Fade);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float val = input[x, y];
                
                float lowerBound = Mathf.InverseLerp(MinHeight - Fade, MinHeight, val);
                float upperBound = 1.0f - Mathf.InverseLerp(MaxHeight, MaxHeight + Fade, val);
                
                result[x, y] = Mathf.Min(lowerBound, upperBound);
            }
        }

        return result;
    }
}

}
