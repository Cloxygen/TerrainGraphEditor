using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class PerlinNoiseNode : GraphNode
{
    public float Scale = 10f;
    public int Octaves = 4;
    public float Persistence = 0.5f;
    public float Lacunarity = 2.0f;
    public int Seed = 1337;

    public PerlinNoiseNode(Vector2 position) : base(position, "Perlin Noise")
    {
        Outputs.Add(new Port("Height", PortType.Heightmap));
    }

    protected override float ExtraHeight => 100f;
    protected override Color HeaderColor => new Color(0.18f, 0.32f, 0.42f, 1f);

    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 80 * scale;
        float y = rect.y;

        Scale       = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Scale", Scale);       y += s;
        Octaves     = UnityEditor.EditorGUI.IntField  (new Rect(rect.x, y, rect.width, h), "Octaves", Octaves);     y += s;
        Persistence = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Persistence", Persistence); y += s;
        Lacunarity  = UnityEditor.EditorGUI.FloatField(new Rect(rect.x, y, rect.width, h), "Lacunarity", Lacunarity);  y += s;
        Seed        = UnityEditor.EditorGUI.IntField  (new Rect(rect.x, y, rect.width, h), "Seed", Seed);
    }

    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        float[,] map = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                float maxPossibleHeight = 0;
                
                for (int i = 0; i < Octaves; i++)
                {
                    // Use world-space coordinates for sampling
                    float worldX = worldOffset.x + x * spacing;
                    float worldY = worldOffset.y + y * spacing;

                    float sampleX = (worldX / Scale) * frequency + Seed;
                    float sampleY = (worldY / Scale) * frequency + Seed;
                    
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;
                    maxPossibleHeight += amplitude;
                    
                    amplitude *= Persistence;
                    frequency *= Lacunarity;
                }
                
                // Normalize to keep within 0-1 range
                map[x, y] = noiseHeight / maxPossibleHeight;
            }
        }
        return map;
    }
}


}
