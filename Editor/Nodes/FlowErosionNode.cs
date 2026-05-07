using UnityEngine;
using System.Collections.Generic;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class FlowErosionNode : GraphNode
{
    public float Strength = 0.5f;
    public float RainAmount = 1.0f;

    public FlowErosionNode(Vector2 position) : base(position, "Flow Erosion")
    {
        Inputs.Add(new Port("Input", PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
        Outputs.Add(new Port("Flow Map", PortType.Mask));
    }

    protected override float ExtraHeight => 45f;
    protected override Color HeaderColor => new Color(0.2f, 0.35f, 0.5f, 1f); // Deep Blue

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 80 * scale;
        float y = rect.y;

        Strength   = UnityEditor.EditorGUI.Slider(new Rect(rect.x, y, rect.width, h), "Strength", Strength, 0f, 2f); y += s;
        RainAmount = UnityEditor.EditorGUI.Slider(new Rect(rect.x, y, rect.width, h), "Rain",     RainAmount, 0f, 5f);
    }

    private struct Pixel : System.IComparable<Pixel>
    {
        public int x, y;
        public float height;
        public int CompareTo(Pixel other) => other.height.CompareTo(height); // Descending
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] input = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] flow = new float[width, height];


        
        // 1. Initial rain
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                flow[x, y] = RainAmount;

        // 2. Sort pixels by height descending
        List<Pixel> sorted = new List<Pixel>(width * height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                sorted.Add(new Pixel { x = x, y = y, height = input[x, y] });
        sorted.Sort();

        // 3. Distribute flow to lowest neighbor
        float maxFlow = 0f;
        foreach (var p in sorted)
        {
            int lowX = -1, lowY = -1;
            float lowH = p.height;

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int nx = p.x + ox, ny = p.y + oy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (input[nx, ny] < lowH)
                        {
                            lowH = input[nx, ny];
                            lowX = nx; lowY = ny;
                        }
                    }
                }
            }

            if (lowX != -1)
            {
                flow[lowX, lowY] += flow[p.x, p.y];
                if (flow[lowX, lowY] > maxFlow) maxFlow = flow[lowX, lowY];
            }
        }

        // 4. Calculate outputs
        if (outputPortIndex == 1) // Flow Map
        {
            float[,] flowOut = new float[width, height];
            if (maxFlow > 0)
            {
                for(int y=0; y<height; y++)
                    for(int x=0; x<width; x++)
                        flowOut[x,y] = Mathf.Clamp01(flow[x,y] / (maxFlow * 0.5f)); // Scale for visibility
            }
            return flowOut;
        }
        else // Eroded Terrain
        {
            float[,] eroded = (float[,])input.Clone();
            float scale = Strength * 0.1f; // Much more balanced scaling
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    eroded[x, y] = Mathf.Max(0, eroded[x, y] - (flow[x, y] / maxFlow) * scale); 
            return eroded;
        }
    }
}

}
