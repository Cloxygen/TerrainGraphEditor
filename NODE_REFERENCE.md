# Terrain Graph Node Reference

This document provides a detailed overview of all available nodes in the Terrain Graph Editor.

## Noise & Generation
Nodes used to generate initial procedural data.

### Perlin Noise
Generates standard fractal Perlin noise.
- **Scale**: The size of the noise features.
- **Octaves**: Number of layers of noise combined.
- **Persistence**: How much each octave contributes to the final result.
- **Lacunarity**: How much detail is added in each octave.
- **Seed**: Random seed for generation.

### Voronoi
Generates cellular Voronoi patterns, useful for cracked earth or stone.

---

## Math & Operations
Nodes used to combine or modify heightmaps using mathematical operations.

### Add / Subtract / Multiply
Performs basic arithmetic between two input heightmaps.

### Power
Raises the input heightmap to a specified power. Useful for sharpening peaks or flattening valleys.

### Clamp
Restricts the heightmap values between a specified minimum and maximum.

### Normalize
Remaps the heightmap values so they span the full 0-1 range.

### Invert
Flips the heightmap values (1.0 becomes 0.0, and vice versa).

---

## Filters & Processing
Nodes that apply filters or masks to the data.

### Blend
Blends two heightmaps together using various blend modes (Linear, Overlay, Screen, etc.).

### Curve
Remaps height values based on a custom AnimationCurve. Highly powerful for shaping terrain features.

### Height Mask
Generates a grayscale mask based on a specific height range.

### Slope Mask
Generates a mask based on the steepness (slope) of the terrain.

### Slope Blur
Blurs the terrain specifically based on the slope, useful for simulating accumulated sediment.

### Terrace
Quantizes the heightmap into flat steps, creating a terraced or layered look.

---

## Advanced Effects
Complex simulation-based nodes.

### Thermal Erosion
Simulates the movement of material from steep slopes to flatter areas (talus piles).
- **Threshold**: The angle at which material starts to slide.
- **Strength**: How much material moves in each iteration.
- **Iterations**: Number of times to run the simulation.

### Flow Erosion
Simulates hydraulic erosion caused by water flowing over the surface. Creates realistic river beds and drainage patterns.

### Directional Warp
Warps one heightmap using the gradient of another, creating twisted or flowing geological features.

---

## Output
The final destination for your graph.

### Terrain Output
The required node that defines the final heightmap to be baked into the `TerrainDataMap`.
