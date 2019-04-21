# Factory Tower Defence

other words

## Terrain

### Design
_The intention is to create procedurally generated deterministic terrain, with an emphasis on gameplay and limited/controlled traversal. Terrain should be broken up by cliffs with sloped providing limited/choked access between areas._

Terrain generation is based on [Worley (Cellular) noise](https://thebookofshaders.com/12/). Worley noise generation is based on [FastNoise.cs](https://assetstore.unity.com/packages/tools/particles-effects/fastnoise-70706).

### Cellular noise terrain generation

Cellular noise (below) is based on an even grid which can be scattered to create more natural shapes. Each cell has with a unique index (int2) in the grid.
<p align="center">
<img src="https://imgur.com/pszR8ED.png">
</p>
<p align="center">
Cellular noise with no scatter, some scatter and high scatter
(generated using [FastNoise Preview](https://github.com/Auburns/FastNoise/releases))
</p>

---

Cellular noise, like Perlin or Simplex, is deterministic but can be randomised using a seed. It is possible to generate the following information deterministically for any point in world space:

* Cell the point is inside info: Index, value noise

* Closest adjacent cell info: Index, value noise

* Distance from the edge of the cell, in the direction of the closest adjacent cell (distance-to-edge)

The index is an int2 and value noise is a float between 0 and 1. Both are unique to the each. 

To determine cell height the cell index x and y values are used as the input for a 2D Simplex noise function. The Simplex output chooses the cell height. This causes more natural, gradual height transitions between cells due to the wave shape generated by simplex noise.
<p align="center">
<img src="https://i.imgur.com/0QuGEV6.png">
</p>
<p align="center">
Terrain generation with no scatter
</p>

Cell value noise is used to decide if a slope exists between two neighbouring cells. Using the value of two cells, a third deterministic value can be created to decide if a slope connects them.
This allows each half of the slope owned by a different cell and both cells generated independently of each other, with no half slopes.
```csharp
float cellPairValue = (cellValue * adjacentValue);
bool slope = CheckIfSlopedBasedOnValue(cellPairValue);
```
<p align="center">
<img src="https://imgur.com/VJBkFBq.png">
</p>
<p align="center">
Slopes generated using Cellular distance-to-edge noise.
</p>

---

distance-to-edge can be used to blend height between cells, where the value 0 is the edge of the current cell. The code below sloped the terrain from the cell height, to half way between it's and the adjacent cell's height.
```csharp
float slopeLength = 0.5f;

float halfWayHeight = (point.cellHeight + point.adjacentHeight) / 2;
float interpolator = math.unlerp(0, slopeLength, point.distanceToEdge);

float terrainHeight = math.lerp(halfWayHeight, cellHeight, interpolator);
```
<p align="center">
<img src="https://imgur.com/McWVde3.png">
</p>
<p align="center">
Distance to edge noise visualised using FastNoise Preview.
</p>





