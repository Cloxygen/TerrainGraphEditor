using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using TerrainGraph.Authoring;

namespace TerrainGraph.Baking
{
    /// Builds terrain data from a TerrainGraph.
    /// Does the work in small steps so Unity stays responsive.
    public class TerrainBaker
    {
        // ==========================================
        // PHASE 2: BAKING (Number Crunching)
        // ==========================================
        // This class takes the Blueprint from Phase 1 and executes it to produce actual terrain data.

        public enum BakeStatus { Idle, Sorting, Calculating, Complete, Failed }

        public BakeStatus Status { get; private set; } = BakeStatus.Idle;
        public string CurrentActivity { get; private set; } = "";
        public float Progress { get; private set; } = 0f;

        private TerrainGraphAsset _graph;
        
        // The sub-systems that make up the baking pipeline
        private BlueprintResolver _resolver;
        private ExecutionQueueBuilder _queueBuilder;
        private AsyncConveyorBelt _conveyorBelt;
        private ResultExporter _exporter;

        public TerrainBaker(TerrainGraphAsset graph)
        {
            _graph = graph;
            _resolver = new BlueprintResolver();
            _queueBuilder = new ExecutionQueueBuilder();
            _conveyorBelt = new AsyncConveyorBelt();
            _exporter = new ResultExporter();
        }

        /// Starts the asynchronous bake process.
        public void BeginBake()
        {
            if (_graph == null) return;

            Status = BakeStatus.Sorting;
            CurrentActivity = "Resolving connections...";

            _resolver.ResolveConnections(_graph);
            
            TerrainOutputNode outputNode = _graph.Nodes.Find(n => n is TerrainOutputNode) as TerrainOutputNode;
            if (outputNode == null)
            {
                Debug.LogError("Bake failed: No Terrain Output node found in graph.");
                Status = BakeStatus.Failed;
                return;
            }

            var executionQueue = _queueBuilder.GetTopologicalSort(outputNode);
            int resolution = _graph.GlobalResolution;
            float spacing = 1000f / (resolution - 1);
            
            Status = BakeStatus.Calculating;
            _graph.IsBaking = true;
            
            _conveyorBelt.Start(executionQueue, resolution, spacing, _graph);
        }

        /// Must be called from an Editor update loop to drive the evaluation.
        public bool Tick()
        {
            if (Status != BakeStatus.Calculating) return false;

            bool isFinished = _conveyorBelt.Tick(out string activity, out float progress, out bool isFaulted);
            CurrentActivity = activity;
            Progress = progress;

            if (isFaulted)
            {
                Cleanup();
                Status = BakeStatus.Failed;
                return true;
            }

            if (isFinished)
            {
                _exporter.ExportResult(_graph, _conveyorBelt.IntermediateResults);
                Cleanup();
                Status = BakeStatus.Complete;
                CurrentActivity = "Bake Complete!";
                Progress = 1f;
                return true;
            }

            return false;
        }

        private void Cleanup()
        {
            _graph.IsBaking = false;
            _graph.BakeCache = null;
        }
    }

    // ==========================================
    // PIPELINE COMPONENTS
    // ==========================================

    // Step 2A: The Queue (Connection Resolution & Sorting)
    internal class BlueprintResolver
    {
        public void ResolveConnections(TerrainGraphAsset graph)
        {
            foreach (var node in graph.Nodes)
            {
                foreach (var port in node.Inputs)
                {
                    port.ConnectedNode = graph.GetNodeByGUID(port.SourceNodeGUID);
                }
            }
        }
    }

    internal class ExecutionQueueBuilder
    {
        public List<GraphNode> GetTopologicalSort(TerrainOutputNode root)
        {
            List<GraphNode> sorted = new List<GraphNode>();
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> visiting = new HashSet<string>();

            void Visit(GraphNode node)
            {
                if (node == null || visited.Contains(node.GUID)) return;
                if (visiting.Contains(node.GUID))
                {
                    Debug.LogWarning("Circular dependency detected in graph! Skipping node.");
                    return;
                }

                visiting.Add(node.GUID);

                foreach (var port in node.Inputs)
                {
                    if (port.ConnectedNode != null)
                    {
                        Visit(port.ConnectedNode);
                    }
                }

                visiting.Remove(node.GUID);
                visited.Add(node.GUID);
                sorted.Add(node);
            }

            Visit(root);
            return sorted;
        }
    }

    // Step 2B: The Conveyor Belt (Asynchronous Evaluation)
    internal class AsyncConveyorBelt
    {
        private List<GraphNode> _queue;
        private int _currentIndex;
        private Task<float[,]> _activeTask;
        private GraphNode _activeNode;
        private int _resolution;
        private float _spacing;
        private TerrainGraphAsset _graph;
        
        public Dictionary<string, float[,]> IntermediateResults { get; private set; }

        public void Start(List<GraphNode> queue, int resolution, float spacing, TerrainGraphAsset graph)
        {
            _queue = queue;
            _resolution = resolution;
            _spacing = spacing;
            _graph = graph;
            _currentIndex = 0;
            IntermediateResults = new Dictionary<string, float[,]>();
            graph.BakeCache = IntermediateResults;
        }

        public bool Tick(out string activity, out float progress, out bool isFaulted)
        {
            isFaulted = false;
            
            if (_activeTask == null)
            {
                if (_currentIndex < _queue.Count)
                {
                    _activeNode = _queue[_currentIndex];
                    activity = $"Processing Node {_currentIndex + 1}/{_queue.Count}: {_activeNode.Title}";
                    progress = (float)_currentIndex / _queue.Count;

                    _activeTask = Task.Run(() => 
                        _activeNode.Evaluate(0, _resolution, _resolution, Vector2.zero, _spacing, _graph)
                    );
                }
                else
                {
                    activity = "Finishing up...";
                    progress = 1f;
                    return true; // Finished
                }
            }
            else if (_activeTask.IsCompleted)
            {
                if (_activeTask.IsFaulted)
                {
                    Debug.LogError($"Bake failed at node {_activeNode.Title}: {_activeTask.Exception}");
                    isFaulted = true;
                    activity = "Failed";
                    progress = 0f;
                    return false;
                }

                IntermediateResults[_activeNode.GUID] = _activeTask.Result;
                
                _activeTask = null;
                _activeNode = null;
                _currentIndex++;
                
                activity = $"Processing Node {_currentIndex}/{_queue.Count}";
                progress = (float)_currentIndex / _queue.Count;
            }
            else
            {
                activity = $"Processing Node {_currentIndex + 1}/{_queue.Count}: {_activeNode.Title}";
                progress = (float)_currentIndex / _queue.Count;
            }

            return false;
        }
    }

    // Step 2C: The Final Save (Output to DataMap)
    internal class ResultExporter
    {
        public void ExportResult(TerrainGraphAsset graph, Dictionary<string, float[,]> results)
        {
            TerrainOutputNode outputNode = graph.Nodes.Find(n => n is TerrainOutputNode) as TerrainOutputNode;
            if (outputNode != null && outputNode.TargetData != null && results.ContainsKey(outputNode.GUID))
            {
                outputNode.TargetData.UpdateGlobalData(results[outputNode.GUID]);
            }
        }
    }
}
