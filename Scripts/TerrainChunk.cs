using UnityEngine;

namespace TerrainRendering
{
[ExecuteAlways]
public class TerrainChunk : MonoBehaviour
{
    private MeshCollider _collider;
    private LODGroup     _lodGroup;
    
    // Arrays for LODs
    private MeshFilter[]   _filters   = new MeshFilter[4];
    private MeshRenderer[] _renderers = new MeshRenderer[4];
    private Mesh[]         _meshes    = new Mesh[4];

    private void EnsureComponents(Material material)
    {
        if (_collider == null) _collider = GetComponent<MeshCollider>();
        if (_collider == null) _collider = gameObject.AddComponent<MeshCollider>();
        _collider.hideFlags = HideFlags.HideInInspector;
        
        if (_lodGroup == null) _lodGroup = GetComponent<LODGroup>();
        if (_lodGroup == null) _lodGroup = gameObject.AddComponent<LODGroup>();

        for (int i = 0; i < 4; i++)
        {
            Transform child = transform.Find($"LOD_{i}");
            if (child == null)
            {
                GameObject go = new GameObject($"LOD_{i}");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            if (_filters[i] == null) _filters[i] = child.GetComponent<MeshFilter>();
            if (_filters[i] == null) _filters[i] = child.gameObject.AddComponent<MeshFilter>();

            if (_renderers[i] == null) _renderers[i] = child.GetComponent<MeshRenderer>();
            if (_renderers[i] == null) _renderers[i] = child.gameObject.AddComponent<MeshRenderer>();
            
            _renderers[i].sharedMaterial = material;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetSelectedRenderState(_renderers[i], UnityEditor.EditorSelectedRenderState.Hidden);
#endif

            if (_meshes[i] == null)
            {
                _meshes[i] = new Mesh();
                _meshes[i].name = $"Terrain Chunk LOD {i}";
            }
        }
    }

    public void UpdateMesh(TerrainDataMap dataSource, Vector2Int coord, Vector2Int gridCount, float worldSize, float heightScale, Material material)
    {
        if (dataSource == null) return;

        EnsureComponents(material);
        
        int[] resolutions = { 128, 64, 32, 16 };
        LOD[] lods = new LOD[4];
        
        // Define screen height transition thresholds
        // LOD 0 (128x128): Very close (e.g. > 50% screen height)
        // LOD 1 (64x64): Mid (e.g. > 25%)
        // LOD 2 (32x32): Far (e.g. > 10%)
        // LOD 3 (16x16): Very far (e.g. > 1%)
        float[] thresholds = { 0.5f, 0.25f, 0.1f, 0.01f };

        for (int l = 0; l < 4; l++)
        {
            int res = resolutions[l];
            Mesh mesh = _meshes[l];
            
            Vector3[] vertices = new Vector3[res * res];
            Vector2[] uv       = new Vector2[res * res];
            int[]     triangles = new int[(res - 1) * (res - 1) * 6];

            float step = worldSize / (res - 1);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = y * res + x;
                    
                    float localU = x / (float)(res - 1);
                    float localV = y / (float)(res - 1);

                    float globalU = (coord.x + localU) / gridCount.x;
                    float globalV = (coord.y + localV) / gridCount.y;

                    float h = dataSource.GetGlobalHeight(globalU, globalV);
                    float height = h * heightScale;

                    vertices[i] = new Vector3(x * step, height, y * step);
                    uv[i] = new Vector2(localU, localV);

                    if (x < res - 1 && y < res - 1)
                    {
                        int ti = (y * (res - 1) + x) * 6;
                        triangles[ti]     = i;
                        triangles[ti + 1] = i + res;
                        triangles[ti + 2] = i + 1;
                        triangles[ti + 3] = i + 1;
                        triangles[ti + 4] = i + res;
                        triangles[ti + 5] = i + res + 1;
                    }
                }
            }

            mesh.Clear();
            mesh.vertices  = vertices;
            mesh.uv        = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            _filters[l].sharedMesh = null;
            _filters[l].sharedMesh = mesh;
            
            Renderer[] renderersForLOD = new Renderer[] { _renderers[l] };
            lods[l] = new LOD(thresholds[l], renderersForLOD);

            // Assign the highest resolution mesh (LOD 0) to the collider.
            // This ensures physics (Player/NPCs) are 100% accurate across the whole map 
            // regardless of where the camera is looking or if the visual is downsampled.
            if (l == 0) 
            {
                _collider.sharedMesh = null;
                _collider.sharedMesh = mesh;
            }
        }
        
        _lodGroup.SetLODs(lods);
        _lodGroup.RecalculateBounds();
    }
}
}
