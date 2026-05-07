using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class PowerNode : GraphNode
{
    public float Power = 2.0f;

    public PowerNode(Vector2 position) : base(position, "Power (Exponent)")
    {
        Inputs.Add(new Port("Input", PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
    }

    protected override float ExtraHeight => 25f;
    protected override Color HeaderColor => new Color(0.15f, 0.25f, 0.45f, 1f); // Dark Blue

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 70 * scale;
        Power = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, rect.y, rect.width, h), "Power", Power);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = Mathf.Pow(Mathf.Max(0, input[x, y]), Power);
            }
        }

        return result;
    }
}

}
