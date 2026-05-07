using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class DirectionalWarpNode : GraphNode
{
    public float Intensity = 20f;

    public DirectionalWarpNode(Vector2 position) : base(position, "Directional Warp")
    {
        Inputs.Add(new Port("Source", PortType.Heightmap));
        Inputs.Add(new Port("Warp",   PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
    }

    protected override float ExtraHeight => 25f;
    protected override Color HeaderColor => new Color(0.2f, 0.45f, 0.45f, 1f); // Teal

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 70 * scale;
        Intensity = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, rect.y, rect.width, h), "Intensity", Intensity);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] sourceData = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] warpData   = graph.GetInputValue(this, 1, width, height, worldOffset, spacing);
        float[,] result     = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Sobel-style gradient calculation for Warp Map
                int xL = Mathf.Max(0, x - 1);
                int xR = Mathf.Min(width - 1, x + 1);
                int yU = Mathf.Max(0, y - 1);
                int yD = Mathf.Min(height - 1, y + 1);

                float dx = (warpData[xR, y] - warpData[xL, y]) * 0.5f;
                float dy = (warpData[x, yD] - warpData[x, yU]) * 0.5f;

                // Warp coordinates (scaled by spacing to keep Intensity consistent in world units)
                float warpX = x + (dx * Intensity) / spacing;
                float warpY = y + (dy * Intensity) / spacing;

                result[x, y] = TerrainGraphAsset.SampleBilinear(sourceData, warpX, warpY);
            }
        }

        return result;
    }
}

}
