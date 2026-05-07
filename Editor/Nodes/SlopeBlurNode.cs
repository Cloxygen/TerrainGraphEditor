using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class SlopeBlurNode : GraphNode
{
    public float Strength = 1.0f;
    public int Radius = 2;

    public SlopeBlurNode(Vector2 position) : base(position, "Slope Blur (Weathering)")
    {
        Inputs.Add(new Port("Input", PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
    }

    protected override float ExtraHeight => 45f;
    protected override Color HeaderColor => new Color(0.35f, 0.35f, 0.35f, 1f); // Grey

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 80 * scale;
        float y = rect.y;

        Strength = UnityEditor.EditorGUI.Slider(new Rect(rect.x, y, rect.width, h), "Strength", Strength, 0f, 2f); y += s;
        Radius   = UnityEditor.EditorGUI.IntSlider(new Rect(rect.x, y, rect.width, h), "Radius",   Radius, 1, 5);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] output = new float[width, height];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 1. Calculate gradient magnitude
                int xL = Mathf.Max(0, x - 1);
                int xR = Mathf.Min(width - 1, x + 1);
                int yU = Mathf.Max(0, y - 1);
                int yD = Mathf.Min(height - 1, y + 1);

                float dx = (input[xR, y] - input[xL, y]) * 0.5f;
                float dy = (input[x, yD] - input[x, yU]) * 0.5f;
                float grad = Mathf.Sqrt(dx * dx + dy * dy) * 10f; // Scale for influence

                // 2. Sample blurred neighborhood
                float blurred = 0f;
                float weightSum = 0f;
                for (int ky = -Radius; ky <= Radius; ky++)
                {
                    for (int kx = -Radius; kx <= Radius; kx++)
                    {
                        int sx = Mathf.Clamp(x + kx, 0, width - 1);
                        int sy = Mathf.Clamp(y + ky, 0, height - 1);
                        blurred += input[sx, sy];
                        weightSum += 1f;
                    }
                }
                blurred /= weightSum;

                // 3. Blend based on gradient
                output[x, y] = Mathf.Lerp(input[x, y], blurred, Mathf.Clamp01(grad * Strength));
            }
        }

        return output;
    }
}

}
