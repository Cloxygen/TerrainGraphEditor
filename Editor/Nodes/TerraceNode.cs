using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class TerraceNode : GraphNode
{
    public float Steps = 10f;
    public float Strength = 1.0f;

    public TerraceNode(Vector2 position) : base(position, "Terrace")
    {
        Inputs.Add(new Port("Input", PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
    }

    protected override float ExtraHeight => 45f;
    protected override Color HeaderColor => new Color(0.45f, 0.35f, 0.25f, 1f); // Brownish

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 70 * scale;
        float y = rect.y;

        Steps    = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Steps",    Steps); y += s;
        Strength = UnityEditor.EditorGUI.Slider(new Rect(rect.x, y, rect.width, h), "Strength", Strength, 0f, 1f);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = new float[width, height];


        if (Steps <= 0) return input;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float val = input[x, y];
                float terraced = Mathf.Floor(val * Steps) / Steps;
                result[x, y] = Mathf.Lerp(val, terraced, Strength);
            }
        }

        return result;
    }
}

}
