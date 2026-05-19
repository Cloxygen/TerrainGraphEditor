# TerrainGraphEditor [![Unity 6.3](https://img.shields.io/badge/Unity-6.3-blue.svg?style=flat&logo=unity)](https://unity.com/) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

TerrainGraphEditor is a procedural terrain generation tool for Unity 6.3, featuring a custom node-based graph editor.

It allows for flexible creation of terrain maps using mathematical operations, noise functions, and erosion systems. Graphs are evaluated and baked into reusable `TerrainDataMap` assets, which are then applied to a chunked terrain system inside the Unity Editor.

This project demonstrates custom Unity editor tooling, graph-based data flow, procedural generation, terrain data baking, chunked mesh generation, and editor-driven terrain workflows.

---

## Requirements

- **Unity 6.3**
- **Universal Render Pipeline (URP)** recommended for the included shaders
- **Unity Mathematics** package

---

## Key Features

- **Node-Based Editor**: A custom visual editor for designing procedural terrain pipelines.
- **Procedural Generation**: Support for noise generation, including Perlin noise, along with mathematical operations for shaping terrain data.
- **Chunked Terrain System**: Renders terrain by breaking it into manageable chunks.
- **Geological Simulation**: Thermal and hydraulic erosion models add natural terrain shaping on top of procedural noise.
- **Data Baking**: Graphs are evaluated into reusable `TerrainDataMap` assets.
- **Editor-Driven Workflow**: Terrain can be designed, baked, assigned, configured, and generated inside the Unity Editor.
- **ScriptableObject Persistence**: Graphs and baked terrain data are stored as reusable Unity assets.

---

## Why This Project Matters

TerrainGraphEditor is not just a terrain demo. It is a custom Unity tool with a full editor-side workflow.

The project demonstrates:

- Custom Unity editor window development
- Node-based visual tooling
- Graph asset persistence using `ScriptableObject`
- Procedural terrain generation
- Graph traversal and evaluation
- Terrain data baking
- Chunked mesh generation
- Custom inspectors
- Editor-time terrain creation
- Thermal and hydraulic erosion simulation
- Shader and material integration for generated terrain

The tool separates terrain design from terrain rendering. The graph editor produces reusable terrain data, while the terrain system consumes that data and builds the mesh representation.

---

## Architecture Overview

The project is organized around a clean separation between the graph editor tools and the inspector-driven terrain system, both operating entirely within the Unity Editor.

| Class | Location | Responsibility |
|---|---|---|
| `TerrainGraphWindow` | `Editor/TerrainGraphWindow.cs` | Main editor window for UI, node rendering, user input, graph navigation, and toolbar actions |
| `TerrainGraphAsset` | `Editor/TerrainGraphAsset.cs` | `ScriptableObject` that persists graph nodes and connections |
| `GraphNode` | `Editor/GraphNode.cs` | Base class for all nodes; subclasses implement specific node behavior, such as `PerlinNoiseNode` |
| `GraphEvaluator` | `Editor/GraphEvaluator.cs` | Traverses the graph and calculates the final terrain data |
| `TerrainDataMap` | Editor/data system | Reusable baked terrain data generated from graph evaluation |
| `TerrainManager` | `Scripts/TerrainManager.cs` | `MonoBehaviour` with a custom inspector that consumes a `TerrainDataMap` and exposes an **Update Mesh** button |
| `TerrainChunk` | `Scripts/TerrainChunk.cs` | Handles mesh generation and rendering for a single terrain segment |

---

## Design Approach

TerrainGraphEditor is built around a purpose-fit workflow:

1. Create or load a graph.
2. Add procedural terrain nodes.
3. Connect nodes into a generation pipeline.
4. Evaluate and bake the graph into a `TerrainDataMap`.
5. Assign the baked data to a `TerrainManager`.
6. Generate chunked terrain meshes in the editor.

The graph editor is responsible for terrain design and data generation. The terrain system is responsible for consuming baked data and generating meshes.

This keeps the project easier to reason about:

- Graph assets define terrain recipes.
- Graph evaluation produces terrain data.
- Baked data can be reused.
- Terrain managers consume baked data.
- Terrain chunks handle mesh construction.

---

## Installation

1. **Download the Tool**: Clone this repository or download the ZIP.
2. **Import to Unity**: Drop the `TerrainGraphEditor` folder into your project's `Assets` directory.
3. **Check Dependencies**: Ensure the `Unity Mathematics` package is installed through the Unity Package Manager.
4. **Use URP if Needed**: Universal Render Pipeline is recommended for the included shaders and materials.

Example folder structure:

```text
Assets/
└── TerrainGraphEditor/
    ├── Editor/
    ├── Scripts/
    ├── Shaders/
    ├── Materials/
    ├── NODE_REFERENCE.md
    └── LICENSE.md
