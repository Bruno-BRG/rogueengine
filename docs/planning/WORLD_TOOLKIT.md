# RogueEngine World Toolkit — Planning Spec

Companion to [`docs/ROADMAP.md`](../ROADMAP.md). Defines the **developer-facing library** for map generation, tile bitmasking, overworld graphs, FOV, and pathfinding — designed to minimize boilerplate for game creators.

**Status:** Planned (post-MVP expansion, Phases 8–9)

---

## Goals

1. **One library, many generators** — plug-in algorithms selectable via JSON or C# script.
2. **Bitmask-first tiles** — autotiling and neighbor-aware rendering without hand-placing every wall corner.
3. **Overworld support** — cell-based regional maps (travel graph, POIs, biomes) inspired by Caves of Qud / Stoneshard-style structure, adapted to RogueEngine’s grid model.
4. **High-level helpers** — common roguelike operations exposed as simple APIs (`SpawnActor`, `RevealFOV`, `FindPath`, `CarveRoom`, etc.).
5. **Testable without UI** — all logic in `RogueEngine.Toolkit` (BCL + Engine only).

---

## New assembly: `RogueEngine.Toolkit`

| Depends on | Must not depend on |
|------------|-------------------|
| `RogueEngine.Engine` | SadConsole, Avalonia, Runtime |

```
src/RogueEngine.Toolkit/
  ProcGen/           # Map generators
  Tiles/             # Bitmask, autotile, tile sets
  Overworld/         # Regional cell graphs
  Pathfinding/       # A*, Dijkstra map
  Fov/               # Field of view
  Helpers/           # Spawn, map queries, seeds
```

Runtime, Editor, and BuildTool reference Toolkit when they need world-building features.

---

## Pluggable generation API

### Core interfaces

```csharp
public interface IMapGenerator
{
    string Id { get; }
    TileMap Generate(GeneratorContext context, Random random);
}

public sealed class GeneratorContext
{
    public int Width { get; init; }
    public int Height { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; }
}
```

### Built-in algorithms (Phase 9)

| ID | Algorithm | Use case |
|----|-----------|----------|
| `rooms_corridors` | Random rooms + L-corridors | Classic roguelike dungeon (**exists today** in Engine; migrate to Toolkit) |
| `cellular_caves` | Cellular automata (cave smoothing) | Organic caves, Caves of Qud–style underground |
| `drunkard_walk` | Random walk carving | Simple caves, tunnels |
| `bsp_dungeon` | Binary space partition | Structured multi-room layouts |
| `hybrid_cave_rooms` | CA caves + room placement | Mixed dungeons |
| `noise_terrain` | Perlin/Simplex heightmap | Outdoor/overland height layers |
| `overworld_cells` | Graph of regions + local maps | World map with enterable zones |

### JSON configuration

`Data/generators/dungeon.json`:

```json
{
  "id": "main_dungeon",
  "algorithm": "cellular_caves",
  "width": 80,
  "height": 22,
  "seed": null,
  "parameters": {
    "fillPercent": 0.45,
    "smoothPasses": 5,
    "wallThreshold": 4
  }
}
```

`game.reproj` may reference default generator:

```json
{
  "name": "MyGame",
  "version": "1",
  "dataPath": "Data",
  "defaultGenerator": "generators/dungeon.json"
}
```

### Registry

`GeneratorRegistry` loads algorithm id → `IMapGenerator` instance. Games and scripts register custom generators:

```csharp
GeneratorRegistry.Register("my_custom", new MyCustomGenerator());
```

---

## Tile bitmasking & autotiling

### Concepts

- Each `Tile` gains optional **bitmask metadata** (neighbor walkability / material type).
- **4-bit** (cardinal) or **8-bit** (including diagonals) mask computed from neighbors.
- **TileSet definition** (JSON): maps mask value → glyph index / sprite id.

`Data/tilesets/stone_walls.json`:

```json
{
  "id": "stone_walls",
  "matchMode": "cardinal4",
  "tiles": {
    "0":  { "glyph": "#" },
    "1":  { "glyph": "│" },
    "2":  { "glyph": "─" },
    "15": { "glyph": "┼" }
  }
}
```

### API

```csharp
public static class TileBitmask
{
    public static int ComputeMask(TileMap map, int x, int y, Func<Tile, bool> isSolid);
    public static char ResolveGlyph(TileSetDefinition set, int mask);
}

public static class AutotileApplicator
{
    public static void Apply(TileMap map, TileSetDefinition set);
}
```

Renderer adapter uses resolved glyph when `Tile` references a tileset id.

---

## Overworld (regional cell maps)

Inspired by **cell-based overworlds** (Qud world structure, Stoneshard map regions) — not a clone, but same *pattern*: world = graph of regions, each region = local grid or generator.

### Data model

```json
{
  "id": "world_01",
  "cells": [
    { "id": "hamlet", "x": 0, "y": 0, "biome": "town", "localMap": "generators/hamlet.json" },
    { "id": "caves",  "x": 1, "y": 0, "biome": "cave", "localMap": "generators/caves.json" }
  ],
  "connections": [
    { "from": "hamlet", "to": "caves", "type": "road" }
  ]
}
```

### API

```csharp
public sealed class OverworldGraph { ... }
public sealed class OverworldGenerator
{
    public OverworldGraph LoadFromJson(string path);
    public TileMap GenerateOverworldView(OverworldGraph graph, int cellSize);
    public TileMap EnterCell(string cellId, Random random);
}
```

Player travels on overworld grid; entering a cell loads/runs that cell’s `IMapGenerator`.

---

## FOV & pathfinding (Phase 8)

Moved from architecture doc into explicit delivery:

| System | Algorithms |
|--------|------------|
| **FOV** | Recursive shadowcasting, raycast (optional), revealed/explored tile state on `TileMap` |
| **Pathfinding** | A* on grid, Dijkstra map for AI threat zones |

### Helper surface

```csharp
public static class MapQueries
{
    public static IEnumerable<Position> VisibleTiles(World world, Position origin, int radius);
    public static IReadOnlyList<Position> FindPath(TileMap map, Position from, Position to);
    public static int[,] BuildDijkstraMap(TileMap map, Position goal);
}
```

---

## Developer helper API (`RogueEngine.Toolkit.Helpers`)

High-level functions to reduce game script boilerplate:

| Helper | Purpose |
|--------|---------|
| `ActorSpawner.Spawn(world, definition, position)` | Spawn from `ActorDefinition` |
| `MapCarver.FillRect(map, room, tile)` | Carve rectangle |
| `MapCarver.ConnectPoints(map, a, b)` | L-corridor |
| `RandomPlacement.FindFloorTile(map, rng)` | Pick random walkable tile |
| `CombatHelper.BumpAttackOrMove(world, entity, dir)` | Standard bump combat |
| `SeedHelper.CreateRandom(seed?)` | Consistent seeded RNG |

Exposed to game scripts via `IScriptContext` extensions in a later scripting phase.

---

## Editor integration (Phase 6 + 10)

| UI feature | Phase |
|------------|-------|
| Dropdown: pick `algorithm` for dungeon generator | 6 |
| Parameter panel for generator JSON fields | 6 |
| Tile bitmask preview in tile painter | 10 |
| Overworld cell graph editor (nodes + links) | 10 |
| Playtest: start on overworld or dungeon | 6 |

---

## Migration from current code

| Today | After Toolkit |
|-------|---------------|
| `Engine/ProcGen/DungeonGenerator` | `Toolkit/ProcGen/RoomsCorridorsGenerator` implements `IMapGenerator` |
| `WorldBuilder` hardcodes generator | Reads `Data/generators/*.json` |
| `Tile` glyph only | Optional `tilesetId` + bitmask resolve at render |
| `ChaseAI` direct movement | Optional `FindPath` via Toolkit |

Existing `DungeonGenerator` remains as default until migration; no breaking change to template until Phase 9.

---

## Test strategy

- Each `IMapGenerator`: fixed seed → snapshot hash or invariant checks (connectivity, walkable ratio, bounds).
- Bitmask: known 3×3 patterns → expected mask values.
- Overworld: load JSON → correct cell count and connections.
- FOV/pathfinding: golden grid fixtures (standard roguelike test maps).
- All tests in `tests/RogueEngine.Toolkit.Tests/` — no window required.

---

## Out of scope (Toolkit v1)

- 3D or hex grids
- Real-time (non-turn-based) pathfinding
- WFC / constraint solvers (candidate for v2 backlog)
- GPU noise or compute shaders
- Procedural narrative / quest graphs
