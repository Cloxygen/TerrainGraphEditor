using System.Collections.Generic;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class TerrainOutputNode : GraphNode
{
    public TerrainDataMap TargetData;





    public TerrainOutputNode(Vector2 position) : base(position, "Terrain Output")
    {
        Inputs.Add(new Port("Height", PortType.Heightmap));
        Inputs.Add(new Port("Alpha Mask", PortType.Mask));
    }


    protected override float ExtraHeight => 100f;

    protected override Color HeaderColor => new Color(0.18f, 0.42f, 0.18f, 1f); // forest green





    protected override void OnDrawFields(Rect rect, float scale)
    {
        float h = 18 * scale;
        float s = 20 * scale;
        UnityEditor.EditorGUIUtility.labelWidth = 80 * scale;
        float y = rect.y;




        if (GUI.Button(new Rect(rect.x, y, rect.width, h * 1.5f), "Save to PNG"))
        {
            ExportToPNG(TerrainGraphWindow.GetActiveGraph());
        }

        y += h * 2.5f;
        UnityEditor.EditorGUI.BeginChangeCheck();
        TargetData = (TerrainDataMap)UnityEditor.EditorGUI.ObjectField(new Rect(rect.x, y, rect.width, h), "Scene Data", TargetData, typeof(TerrainDataMap), false);
        if (UnityEditor.EditorGUI.EndChangeCheck())
        {
            if (TerrainGraphWindow.Instance != null) TerrainGraphWindow.Instance.RefreshGraph();
        }
    }





    public override float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        // Terrain Output has no outputs, but fetching port 0 previews its Height input
        return graph.GetInputValue(this, 0, width, height, worldOffset, spacing);
    }



    private void ExportToPNG(TerrainGraphAsset graph)
    {
        if (graph == null) return;
        int res = graph.GlobalResolution;
        // ...
        string path = UnityEditor.EditorUtility.SaveFilePanel("Save Heightmap as PNG", "", "Heightmap", "png");
        if (string.IsNullOrEmpty(path)) return;

        float[,] map = Evaluate(0, res, res, Vector2.zero, 1.0f, graph);
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        Color[] colors = new Color[res * res];

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float val = Mathf.Clamp01(map[x, y]);
                colors[(res - 1 - y) * res + x] = new Color(val, val, val, 1f);
            }
        }
        // ...


        tex.SetPixels(colors);
        tex.Apply();

        // 3. Save to Disk
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);

        Debug.Log($"Heightmap exported to: {path}");
        UnityEditor.EditorUtility.DisplayDialog("Export Complete", "Heightmap saved successfully!", "OK");
    }

}


}
