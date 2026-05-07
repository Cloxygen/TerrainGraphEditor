using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class ThermalErosionNode : GraphNode
{
    public float Threshold = 0.05f;
    public float Strength = 0.2f;
    public int Iterations = 5;

    public ThermalErosionNode(Vector2 position) : base(position, "Thermal Erosion")
    {
        Inputs.Add(new Port("Input", PortType.Heightmap));
        Outputs.Add(new Port("Output", PortType.Heightmap));
    }

    protected override float ExtraHeight => 65f;
    protected override Color HeaderColor => new Color(0.4f, 0.3f, 0.2f, 1f); // Dirty Brown

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 80 * scale;
        float y = rect.y;

        Threshold  = UnityEditor.EditorGUI.Slider(new Rect(rect.x, y, rect.width, h), "Threshold", Threshold, 0f, 0.2f); y += s;
        Strength   = UnityEditor.EditorGUI.Slider(new Rect(rect.x, y, rect.width, h), "Strength",  Strength, 0f, 0.5f); y += s;
        Iterations = UnityEditor.EditorGUI.IntSlider(new Rect(rect.x, y, rect.width, h), "Iterations", Iterations, 1, 20);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] map = graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
        float[,] result = (float[,])map.Clone();


        for (int i = 0; i < Iterations; i++)
        {
            float[,] next = (float[,])result.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float currentH = result[x, y];
                    float totalMoveOut = 0f;
                    
                    // Check neighbors and sum up how much height to move
                    totalMoveOut += GetMoveAmount(x - 1, y, currentH, next, width, height, result);
                    totalMoveOut += GetMoveAmount(x + 1, y, currentH, next, width, height, result);
                    totalMoveOut += GetMoveAmount(x, y - 1, currentH, next, width, height, result);
                    totalMoveOut += GetMoveAmount(x, y + 1, currentH, next, width, height, result);

                    next[x, y] -= totalMoveOut;
                }
            }
            result = next;
        }

        return result;
    }

    private float GetMoveAmount(int nx, int ny, float currentH, float[,] targetMap, int w, int h, float[,] sourceMap)
    {
        if (nx < 0 || nx >= w || ny < 0 || ny >= h) return 0f;

        float neighborH = sourceMap[nx, ny];
        float diff = currentH - neighborH;

        if (diff > Threshold)
        {
            float move = (diff - Threshold) * Strength * 0.25f; // distribute across neighbors
            targetMap[nx, ny] += move;
            return move;
        }
        return 0f;
    }
}

}
