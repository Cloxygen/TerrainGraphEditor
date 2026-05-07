using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class VoronoiNode : GraphNode
{
    public float Scale = 10f;
    public int Seed = 1;

    public VoronoiNode(Vector2 position) : base(position, "Voronoi (Cells)")
    {
        Outputs.Add(new Port("Distance", PortType.Heightmap));
        Outputs.Add(new Port("Cell ID",  PortType.Mask));
    }

    protected override float ExtraHeight => 45f;
    protected override Color HeaderColor => new Color(0.15f, 0.45f, 0.35f, 1f); // Dark Green

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 70 * scale;
        float y = rect.y;

        Scale = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Scale", Scale); y += s;
        Seed  = UnityEditor.EditorGUI.IntField(new Rect(rect.x, y, rect.width, h), "Seed",   Seed);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] result = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // World coordinates
                float worldX = (worldOffset.x + x * spacing) / Scale;
                float worldY = (worldOffset.y + y * spacing) / Scale;

                int cellX = Mathf.FloorToInt(worldX);
                int cellY = Mathf.FloorToInt(worldY);

                float minDist = 2f;
                float bestIdx = 0f;

                // Check 3x3 neighborhood of cells
                for (int cy = -1; cy <= 1; cy++)
                {
                    for (int cx = -1; cx <= 1; cx++)
                    {
                        int nx = cellX + cx;
                        int ny = cellY + cy;

                        // Deterministic dot for this cell
                        Vector2 dot = GetDot(nx, ny);
                        float d = Vector2.Distance(new Vector2(worldX, worldY), dot);

                        if (d < minDist)
                        {
                            minDist = d;
                            bestIdx = (nx * 13 + ny * 7) % 256; // Pseudo-random stable ID
                        }
                    }
                }

                if (outputPortIndex == 0) result[x, y] = Mathf.Clamp01(minDist);
                else result[x, y] = bestIdx / 256f;
            }
        }

        return result;
    }

    private Vector2 GetDot(int cx, int cy)
    {
        // Deterministic hash based on cell coords and Seed
        float h = (cx * 127.1f + cy * 311.7f + Seed * 1337.0f);
        float rx = (Mathf.Sin(h) * 43758.5453123f);
        float ry = (Mathf.Sin(h + 0.1f) * 43758.5453123f);
        return new Vector2(cx + (rx - Mathf.Floor(rx)), cy + (ry - Mathf.Floor(ry)));
    }
}

}
