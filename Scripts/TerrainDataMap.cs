using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Terrain Data Map", menuName = "Terrain Graph/Data Output")]
public class TerrainDataMap : ScriptableObject
{
    [System.Serializable]
    public class ChunkData
    {
        public Vector2Int Coordinate;
        public int Resolution;
        public float[] Heights; // Serialized 1D array (Unity doesn't serialize 2D)
    }

    public float[] GlobalHeights;
    public int GlobalWidth;
    public int GlobalHeight;
    public List<ChunkData> Chunks = new List<ChunkData>();

    
    public System.Action OnUpdated;

    public void UpdateGlobalData(float[,] heightMap)
    {
        if (heightMap == null) return;

        int w = heightMap.GetLength(0);
        int h = heightMap.GetLength(1);
        float[] flatHeights = new float[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                flatHeights[y * w + x] = heightMap[x, y];
            }
        }

        GlobalWidth = w;
        GlobalHeight = h;
        GlobalHeights = flatHeights;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        OnUpdated?.Invoke();
    }

    public float GetGlobalHeight(float u, float v)
    {
        if (GlobalHeights == null || GlobalHeights.Length == 0 || GlobalWidth <= 1 || GlobalHeight <= 1) return 0f;

        // Clamp normalized coordinates to stay within bounds
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        float x = u * (GlobalWidth - 1);
        float y = v * (GlobalHeight - 1);

        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(x0 + 1, GlobalWidth - 1);
        int y1 = Mathf.Min(y0 + 1, GlobalHeight - 1);

        float tx = x - x0;
        float ty = y - y0;

        float h00 = GlobalHeights[y0 * GlobalWidth + x0];
        float h10 = GlobalHeights[y0 * GlobalWidth + x1];
        float h01 = GlobalHeights[y1 * GlobalWidth + x0];
        float h11 = GlobalHeights[y1 * GlobalWidth + x1];

        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(h0, h1, ty);
    }

    public void UpdateChunk(Vector2Int coord, float[,] heightMap)
    {
        int res = heightMap.GetLength(0);
        float[] flatHeights = new float[res * res];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                flatHeights[y * res + x] = heightMap[x, y];
            }
        }

        ChunkData chunk = Chunks.Find(c => c.Coordinate == coord);
        if (chunk == null)
        {
            chunk = new ChunkData { Coordinate = coord };
            Chunks.Add(chunk);
        }

        chunk.Resolution = res;
        chunk.Heights = flatHeights;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        OnUpdated?.Invoke();
    }


    public float[,] GetChunk(Vector2Int coord)
    {
        ChunkData chunk = Chunks.Find(c => c.Coordinate == coord);
        if (chunk == null || chunk.Heights == null) return null;

        int res = chunk.Resolution;
        float[,] map = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                map[x, y] = chunk.Heights[y * res + x];
            }
        }
        return map;
    }

    public void Clear()
    {
        Chunks.Clear();
        OnUpdated?.Invoke();
    }
}
