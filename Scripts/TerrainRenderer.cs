using System.Collections.Generic;
using UnityEngine;

namespace TerrainRendering
{
    [ExecuteInEditMode]
    public class TerrainRenderer : MonoBehaviour
    {
        // ==========================================
        // PHASE 3: RENDERING (Building the 3D Mesh)
        // ==========================================
        // This takes the baked 2D data map from Phase 2 and generates 3D geometry in the scene.

        public TerrainDataMap    DataSource;
        public Material           TerrainMaterial;
        
        [Header("Chunk Settings")]
        public float ChunkSize       = 100f;
        public float HeightScale     = 50f;
        
        [Header("Grid Settings")]
        public int GridWidth  = 3;
        public int GridHeight = 3;

        private ChunkGridManager _chunkManager;

        private void OnEnable()
        {
            if (_chunkManager == null) _chunkManager = new ChunkGridManager(this);
        }

        private void OnDisable()
        {
            if (_chunkManager != null) _chunkManager.StopGeneration();
        }

        private void OnValidate()
        {
            if (_chunkManager == null) _chunkManager = new ChunkGridManager(this);
        }

        [ContextMenu("Regenerate Terrain")]
        public void RequestRegenerate()
        {
            if (DataSource == null) return;
            if (_chunkManager == null) _chunkManager = new ChunkGridManager(this);
            
            _chunkManager.StartGeneration();
        }
    }

    // Step 3A: Chunking (Queuing up terrain sections)
    internal class ChunkGridManager
    {
        private TerrainRenderer _renderer;
        private Dictionary<Vector2Int, TerrainChunk> _chunks = new Dictionary<Vector2Int, TerrainChunk>();
        private Queue<Vector2Int> _generationQueue = new Queue<Vector2Int>();
        private bool _isGenerating = false;

        public ChunkGridManager(TerrainRenderer renderer)
        {
            _renderer = renderer;
        }

        public void StartGeneration()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                StopGeneration();
                ClearChunks();
                _generationQueue.Clear();
                
                // Queue all the chunks
                for (int cy = 0; cy < _renderer.GridHeight; cy++)
                {
                    for (int cx = 0; cx < _renderer.GridWidth; cx++)
                    {
                        _generationQueue.Enqueue(new Vector2Int(cx, cy));
                    }
                }
                _isGenerating = true;
                UnityEditor.EditorApplication.update += EditorUpdateGeneration;
                return;
            }
#endif
            PerformRegenerate();
        }

        public void StopGeneration()
        {
#if UNITY_EDITOR
            if (_isGenerating) 
            {
                UnityEditor.EditorApplication.update -= EditorUpdateGeneration;
                _isGenerating = false;
                UnityEditor.EditorUtility.ClearProgressBar();
            }
#endif
        }

#if UNITY_EDITOR
        private void EditorUpdateGeneration()
        {
            if (_renderer == null || _renderer.DataSource == null)
            {
                StopGeneration();
                return;
            }

            if (_generationQueue.Count > 0)
            {
                int total = _renderer.GridWidth * _renderer.GridHeight;
                int current = total - _generationQueue.Count;
                float progress = (float)current / total;
                
                bool cancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar("Generating Terrain", $"Processing chunk {current + 1}/{total}... (Wait or Cancel)", progress);
                if (cancel)
                {
                    StopGeneration();
                    return;
                }

                Vector2Int coord = _generationQueue.Dequeue();
                GenerateChunk(coord.x, coord.y);
                
                if (_generationQueue.Count == 0)
                {
                    StopGeneration();
                }
            }
        }
#endif

        private void GenerateChunk(int cx, int cy)
        {
            // Step 3B: Building the Geometry
            Vector2Int coord = new Vector2Int(cx, cy);
            Vector2Int gridCount = new Vector2Int(_renderer.GridWidth, _renderer.GridHeight);

            GameObject go = new GameObject($"Chunk_{cx}_{cy}");
            go.transform.SetParent(_renderer.transform);
            go.transform.localPosition = new Vector3(cx * _renderer.ChunkSize, 0, cy * _renderer.ChunkSize);

            TerrainChunk chunk = go.AddComponent<TerrainChunk>();
            
            chunk.UpdateMesh(_renderer.DataSource, coord, gridCount, _renderer.ChunkSize, _renderer.HeightScale, _renderer.TerrainMaterial);
            _chunks[coord] = chunk;
        }

        private void PerformRegenerate()
        {
            if (_renderer == null || _renderer.DataSource == null) return;
            ClearChunks();
            
            for (int cy = 0; cy < _renderer.GridHeight; cy++)
            {
                for (int cx = 0; cx < _renderer.GridWidth; cx++)
                {
                    GenerateChunk(cx, cy);
                }
            }
        }

        private void ClearChunks()
        {
            foreach (var chunk in _chunks.Values)
            {
                if (chunk != null) 
                {
                    if (Application.isPlaying) Object.Destroy(chunk.gameObject);
                    else Object.DestroyImmediate(chunk.gameObject);
                }
            }
            _chunks.Clear();

            // Cleanup orphans
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < _renderer.transform.childCount; i++) children.Add(_renderer.transform.GetChild(i).gameObject);
            foreach (var child in children) 
            {
                 if (Application.isPlaying) Object.Destroy(child);
                 else Object.DestroyImmediate(child);
            }
        }
    }
}
