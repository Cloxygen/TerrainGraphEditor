using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TerrainGraph.Authoring
{
[System.Serializable]
public class GraphNode
{
    public string     GUID;
    public Rect       Rect;
    public string     Title;
    public bool       IsSelected;
    public List<Port> Inputs;
    public List<Port> Outputs;


    public Texture2D  PreviewTexture;

    protected const float HeaderHeight  = 24f;
    protected const float PortRowHeight = 22f;
    protected const float BodyPadding   = 8f;
    protected const float NodeWidth     = 220f;
    protected const float PreviewSize   = 204f; // Matches NodeWidth - BodyPadding * 2
    private   const float DotRadius     = 5f;
    private   const float HitRadius     = 10f;

    protected virtual Color HeaderColor => new Color(0.24f, 0.28f, 0.34f, 1f);

    protected virtual float ExtraHeight => 0f;

    public GraphNode(Vector2 position, string title,
                     List<Port> inputs = null, List<Port> outputs = null)
    {
        GUID    = System.Guid.NewGuid().ToString();
        Title   = title;
        Inputs  = inputs  ?? new List<Port>();
        Outputs = outputs ?? new List<Port>();

        Rect = new Rect(position.x, position.y, NodeWidth, 100f);
        UpdateHeight();
    }

    private void UpdateHeight()
    {
        float portsHeight   = Mathf.Max(Inputs.Count, Outputs.Count) * PortRowHeight;
        float contentHeight = portsHeight + BodyPadding * 2f + ExtraHeight + PreviewSize;
        Rect.height = HeaderHeight + contentHeight;
    }

    public Vector2 GetInputPortPos(int index)
    {
        return new Vector2(
            Rect.x,
            Rect.y + HeaderHeight + BodyPadding + (index + 0.5f) * PortRowHeight);
    }

    public Vector2 GetOutputPortPos(int index)
    {
        return new Vector2(
            Rect.xMax,
            Rect.y + HeaderHeight + BodyPadding + (index + 0.5f) * PortRowHeight);
    }

    public int HitTestInputPort(Vector2 graphPos)
    {
        for (int i = 0; i < Inputs.Count; i++)
            if (Vector2.Distance(GetInputPortPos(i), graphPos) <= HitRadius)
                return i;
        return -1;
    }

    public int HitTestOutputPort(Vector2 graphPos)
    {
        for (int i = 0; i < Outputs.Count; i++)
            if (Vector2.Distance(GetOutputPortPos(i), graphPos) <= HitRadius)
                return i;
        return -1;
    }

    public void Draw(Rect screenRect)
    {
        UpdateHeight();

        // 1. Layout Calculations
        float scale         = screenRect.height / Rect.height; 
        float scaledHeader  = HeaderHeight  * scale;
        float scaledRow     = PortRowHeight * scale;
        float scaledPad     = BodyPadding   * scale;
        float scaledDot     = DotRadius     * scale;
        float scaledPreview = PreviewSize   * scale;

        float borderSize    = IsSelected ? 2f : 1f;
        Color borderColor   = IsSelected
            ? new Color(0.30f, 0.50f, 0.80f, 1f)
            : new Color(0.05f, 0.05f, 0.06f, 1f);

        Rect inner  = new Rect(screenRect.x + borderSize, screenRect.y + borderSize,
                               screenRect.width  - borderSize * 2f,
                               screenRect.height - borderSize * 2f);
        Rect header = new Rect(inner.x, inner.y, inner.width, scaledHeader);
        float bodyTop  = screenRect.y + scaledHeader;
        float portsHeight = Mathf.Max(Inputs.Count, Outputs.Count) * scaledRow;

        // 2. Rendering
        DrawBackground(screenRect, inner, borderColor);
        DrawHeader(header);
        
        Rect fieldsRect = new Rect(inner.x + scaledPad, bodyTop + scaledPad + portsHeight, inner.width - scaledPad * 2, inner.height - portsHeight - scaledPreview - scaledPad * 3);
        OnDrawFields(fieldsRect, scale);

        DrawPortLabels(inner, bodyTop, scaledPad, scaledRow, scaledDot, scale);
        DrawPreviewSection(inner, scaledPreview, scaledPad);
        DrawPortDots(screenRect, bodyTop, scaledPad, scaledRow, scaledDot);
    }

    private void DrawBackground(Rect screenRect, Rect inner, Color borderColor)
    {
        EditorGUI.DrawRect(screenRect, borderColor);
        EditorGUI.DrawRect(inner, new Color(0.16f, 0.17f, 0.20f, 1f));
    }

    private void DrawHeader(Rect headerRect)
    {
        EditorGUI.DrawRect(headerRect, HeaderColor);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(8, 8, 0, 0)
        };
        titleStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        GUI.Label(headerRect, Title, titleStyle);
    }

    private void DrawPortLabels(Rect inner, float bodyTop, float scaledPad, float scaledRow, float scaledDot, float scale)
    {
        GUIStyle portStyle = new GUIStyle(EditorStyles.miniLabel);
        portStyle.normal.textColor = new Color(0.80f, 0.83f, 0.88f, 1f);
        float labelPad = scaledDot + 6f * scale;

        for (int i = 0; i < Inputs.Count; i++)
        {
            float cy = bodyTop + scaledPad + (i + 0.5f) * scaledRow;
            portStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(inner.x + labelPad, cy - scaledRow * 0.5f, inner.width * 0.5f, scaledRow), Inputs[i].Name, portStyle);
        }

        for (int i = 0; i < Outputs.Count; i++)
        {
            float cy = bodyTop + scaledPad + (i + 0.5f) * scaledRow;
            portStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(inner.xMax - inner.width * 0.5f - labelPad, cy - scaledRow * 0.5f, inner.width * 0.5f, scaledRow), Outputs[i].Name, portStyle);
        }
    }

    private void DrawPreviewSection(Rect inner, float scaledPreview, float scaledPad)
    {
        Rect previewRect = new Rect(inner.x + scaledPad, 
                                    inner.yMax - scaledPreview - scaledPad, 
                                    scaledPreview, scaledPreview);
        
        EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.12f, 1f));
        OnDrawPreview(previewRect);
    }

    private void DrawPortDots(Rect screenRect, float bodyTop, float scaledPad, float scaledRow, float scaledDot)
    {
        Handles.BeginGUI();
        for (int i = 0; i < Inputs.Count; i++)
        {
            float cy = bodyTop + scaledPad + (i + 0.5f) * scaledRow;
            Handles.color = GetPortColor(Inputs[i].Type);
            Handles.DrawSolidDisc(new Vector3(screenRect.x, cy, 0f), Vector3.forward, scaledDot);
        }
        for (int i = 0; i < Outputs.Count; i++)
        {
            float cy = bodyTop + scaledPad + (i + 0.5f) * scaledRow;
            Handles.color = GetPortColor(Outputs[i].Type);
            Handles.DrawSolidDisc(new Vector3(screenRect.xMax, cy, 0f), Vector3.forward, scaledDot);
        }
        Handles.color = Color.white;
        Handles.EndGUI();
    }

    protected virtual void OnDrawFields(Rect rect, float scale) { }
    
    protected virtual void OnDrawPreview(Rect previewRect) 
    {
        GUI.Box(previewRect, "Preview Area", EditorStyles.helpBox);
        if (PreviewTexture != null)
        {
            GUI.DrawTexture(previewRect, PreviewTexture, ScaleMode.StretchToFill);
        }
        else
        {
            GUI.Label(previewRect, "Uncomputed", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
        }
    }

    /// Evaluates this node's logic to produce a heightmap or mathematical array.
    /// Traverses backward through the graph using the provided TerrainGraphAsset.
    /// width/height: the resolution of the array (e.g. 64x64).
    /// worldOffset: the bottom-left corner of the chunk in world units.
    /// spacing: the distance between samples in world units.
    public virtual float[,] Evaluate(int outputPortIndex, int width, int height, Vector2 worldOffset, float spacing, TerrainGraphAsset graph)
    {
        return new float[width, height];
    }


    public virtual void ComputePreview(TerrainGraphAsset graph, Vector2 worldOffset, float spacing)
    {
        int res = 128;
        // Evaluate logic to produce the preview thumbnail
        float[,] map = Evaluate(0, res, res, worldOffset, spacing, graph);

        if (PreviewTexture == null || PreviewTexture.width != res)
        {
            PreviewTexture = new Texture2D(res, res, TextureFormat.RGBA32, false);
            PreviewTexture.filterMode = FilterMode.Bilinear;
            PreviewTexture.wrapMode = TextureWrapMode.Clamp;
        }

        // Find min/max to "fit" the heightmap values into the 0-1 range for visualization
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float v = map[x, y];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        float range = max - min;
        if (range < 0.0001f) range = 1f;

        Color[] colors = new Color[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                // Remap value to 0-1 based on the range of the preview data
                float val = (map[x, y] - min) / range;
                colors[(res - 1 - y) * res + x] = new Color(val, val, val, 1f); 
            }
        }
        PreviewTexture.SetPixels(colors);
        PreviewTexture.Apply();
    }

    public static Color GetPortColor(PortType type)
    {
        switch (type)
        {
            case PortType.Heightmap: return new Color(0.35f, 0.60f, 0.90f, 1f);
            case PortType.Mask:      return new Color(0.40f, 0.80f, 0.45f, 1f);
            default:                 return Color.white;
        }
    }
}

}
