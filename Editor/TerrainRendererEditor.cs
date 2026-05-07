using UnityEditor;
using UnityEngine;
using TerrainRendering;

namespace TerrainRendering
{
    [CustomEditor(typeof(TerrainRenderer))]
    public class TerrainRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            TerrainRenderer manager = (TerrainRenderer)target;

            GUILayout.Space(10);
            
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Light green
            if (GUILayout.Button("Update Mesh", GUILayout.Height(30)))
            {
                manager.RequestRegenerate();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
