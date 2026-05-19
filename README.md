# Terrain Generation Project

[![Unity 6.3](https://img.shields.io/badge/Unity-6.3-blue.svg?style=flat&logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


## Overview
This project is a procedural terrain generation tool for Unity 6.3, featuring a custom node-based graph editor. It allows for flexible creation of terrain maps using various mathematical operations and noise functions, which are then evaluated and applied to a chunked terrain system in the scene.

### 🛠️ Requirements
*   **Unity 6.3**
*   **Universal Render Pipeline (URP)** recommended for included shaders.
*   **Unity Mathematics** package.


## Key Features
- **Node-Based Editor**: A custom visual editor for designing terrain pipelines.
- **Procedural Generation**: Support for various noise types (e.g., Perlin) and math operations.
- **Chunked Terrain System**: Efficiently renders large terrains by breaking them into manageable chunks.
- **Geological Simulation**: Thermal and Hydraulic erosion models add a layer of natural realism to standard procedural noise.
- **Data Baking**: Evaluation of graphs into reusable `TerrainDataMap` assets.

## Architecture Overview
The project is organized around a clean separation between the graph editor tools and the inspector-driven terrain system, both operating entirely within the Unity Editor.

| Class | Location | Responsibility |
|---|---|---|
| `TerrainGraphWindow` | `Editor/TerrainGraphWindow.cs` | Main editor window — UI, node rendering, and user input |
| `TerrainGraphAsset` | `Editor/TerrainGraphAsset.cs` | `ScriptableObject` that persists nodes and connections |
| `GraphNode` | `Editor/GraphNode.cs` | Base class for all nodes; subclasses implement specific logic (e.g., `PerlinNoiseNode`) |
| `GraphEvaluator` | `Editor/GraphEvaluator.cs` | Traverses the graph and calculates the final terrain data |
| `TerrainManager` | `Scripts/TerrainManager.cs` | `MonoBehaviour` with a custom Inspector that consumes a `TerrainDataMap` and exposes an **Update Mesh** button |
| `TerrainChunk` | `Scripts/TerrainChunk.cs` | Handles mesh generation and rendering for a single terrain segment |

## Installation

1.  **Download the Tool**: Clone this repository or download the ZIP.
2.  **Import to Unity**: Drop the `TerrainGraphEditor` folder into your project's `Assets` directory.
3.  **Check Dependencies**: Ensure the `Unity Mathematics` package is installed via the Package Manager.

## Getting Started

1. **Open the Terrain Graph Editor** — go to `Tools` > `Terrain Graph` in the Unity menu bar.
2. **Create or load a graph** — click **Create New Graph** in the toolbar, or select an existing `TerrainGraphAsset` in the graph selection field.
3. **Add nodes** — right-click in the graph area to see available nodes (Noise, Math, Output, etc.).
4. **Connect nodes** — drag from an output port to an input port to define the data flow. Ensure your graph ends with an **Output** node representing the final heightmap.
5. **Bake the data** — click **Bake Data Map** in the toolbar to generate the `TerrainDataMap` asset.
6. **Set up the scene** — add a **TerrainManager** component to a GameObject and assign the baked `TerrainDataMap` to its **DataSource** field.
7. **Adjust settings** — configure **Chunk Size**, **Height Scale**, and **Grid Settings** on the `TerrainManager` as needed.
8. **Generate** — click the **Update Mesh** button in the `TerrainManager` inspector to build the terrain in the editor.

## Design & Implementation Notes

### User-Centric Design Goals
- **Visual Workflow**: A node-based graph approach was chosen to make procedural generation intuitive and non-linear.
- **Responsive UX**: To prevent UI lockups during intensive bakes, the evaluation system uses an iterative step-by-step process with a built-in progress bar.
- **Modular Data**: Using `ScriptableObjects` ensures that terrain "recipes" are portable and completely decoupled from the rendering scene.

### Key Technical Systems
- **Decoupled Engine**: The generation logic outputs a standalone `TerrainDataMap`, allowing the procedural data to be used by any custom mesh or vertex system.
- **Custom GUI Transformations**: Matrix-based coordinate space handling manages pixel-perfect node interactions across different zoom and pan levels.
- **Geological Simulation**: Integrated Thermal and Hydraulic erosion models produce natural-looking terrain from standard procedural noise.

### 🖼️ Gallery
| Node Graph Editor | Resulting Terrain |
|---|---|
| ![Graph Screenshot](https://i.ibb.co/9mh3jGR8/Screenshot-2026-05-19-115352.png) | ![Terrain Screenshot](https://i.ibb.co/d4LP25Jq/Screenshot-2026-05-03-131613.png) |


### Development Process & AI Oversight
Development followed a structured, iterative loop with an AI coding agent. The project was broken into discrete components, and for each one, the agent was prompted to produce a detailed implementation plan before writing any code. These plans were reviewed and approved before proceeding, ensuring the overall architecture remained intentional.

Once code was written, each piece was read through and tested manually. Results varied — some components worked immediately, while others required extensive back-and-forth: anywhere from a single clarifying instruction to dozens of precise, explicit prompts to get the behavior exactly right. This process demanded a solid understanding of what the code *should* do in order to diagnose why it *didn't*, and to communicate corrections effectively.

The final result reflects decisions that were deliberated at every stage, with AI acting as an implementation tool under close human direction.

---

## Project Structure
```
Assets/TerrainGraphEditor/
├── Editor/        # Editor scripts, graph logic, and node definitions
├── Scripts/       # TerrainManager and TerrainChunk — editor-time mesh generation
├── Shaders/       # Custom terrain shaders
├── Materials/     # Materials used by the terrain system
├── NODE_REFERENCE.md   # Detailed guide for all available nodes
└── LICENSE.md          # MIT License
```
