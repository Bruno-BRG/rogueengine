# RogueEngine Roadmap

Living development plan for the **rogueengine** repository. Update this file as phases complete.

**Current status:** `0.5 — Portable build` (CLI build tool, validate, publish, ZIP export)

**Last updated:** 2026-06-19

---

## Version milestones

| Version | Focus | Expected outcome | Status |
|---------|-------|------------------|--------|
| **0.0** | Planning | Architecture doc, repo layout, README | Done |
| **0.1** | Minimal runtime | Window, map, player, movement, collision | Done |
| **0.2** | Minimal roguelike | Dungeon, turns, enemy, combat, message log | Done |
| **0.3** | Data & project | `game.reproj`, JSON entities/items, save/load | Done |
| **0.4** | C# scripting | Compiled scripts and custom behaviors | Done |
| **0.5** | Portable build | Windows x64 portable export | Done |
| **0.6** | Basic editor | Create, open, edit data, playtest, export | Not started |
| **0.7** | Visual scripting MVP | Simple graphs compiled to C# | Not started |
| **0.8** | Roguelike depth | FOV, pathfinding, items/inventory | Not started |
| **0.9** | World Toolkit | ProcGen lib, bitmask, overworld, generator picker | Not started |
| **0.10** | Installer | MSI/EXE via WiX | Not started |
| **1.0** | Initial release | Engine usable for at least two sample games | Not started |

---

## Implementation phases

### Phase 1 — Playable core (→ v0.1)

- [x] Create .NET solution and project references
- [x] Implement `World`, `TileMap`, `Entity`, basic components
- [x] Minimal SadConsole renderer (`RogueEngine.SadConsole`)
- [x] Input → command mapping
- [x] Movement and collision

**Modules:** `RogueEngine.Engine`, `RogueEngine.SadConsole`, `RogueEngine.Runtime`

---

### Phase 2 — Minimal roguelike (→ v0.2)

- [x] Simple procedural dungeon (rooms + corridors)
- [x] Turn system (`TurnManager`)
- [x] Basic enemy AI (chase)
- [x] Basic combat
- [x] Message log and minimal UI layer

**Engine areas:** ProcGen, TurnBased, Rules, AI

---

### Phase 3 — Project & data (→ v0.3)

- [x] Define `game.reproj` schema
- [x] Load actors/items/maps from JSON
- [x] Initial data schemas
- [x] Save / load game state
- [x] Starter template in `templates/BasicRoguelikeProject/`

---

### Phase 4 — Scripting (→ v0.4)

- [x] Behavior interfaces for entities
- [x] Compile C# scripts at build time (Roslyn)
- [x] Friendly compile errors
- [x] Bind scripts to entities via JSON

---

### Phase 5 — Build tool (→ v0.5)

- [x] CLI: `rogueengine build`
- [x] Validate `game.reproj`
- [x] Copy assets, compile scripts, run `dotnet publish`
- [x] Portable folder output
- [x] ZIP portable export
- [ ] Prepare path for single-file publish (later)

**Module:** `RogueEngine.BuildTool`

---

### Phase 6 — Editor (→ v0.6)

- [ ] Avalonia desktop shell (`RogueEngine.Editor`)
- [ ] Create / open project
- [ ] Edit core data files (actors, items, settings)
- [ ] Pick map generator + edit generator JSON
- [ ] Playtest (launch runtime)
- [ ] Trigger build tool from UI

---

### Phase 7 — Visual scripting (→ v0.7)

- [ ] Minimal graph editor
- [ ] Node types: event, condition, action, log, spawn, open door
- [ ] Graph JSON → generated C#
- [ ] Compile generated code in build pipeline

---

### Phase 8 — Roguelike depth (→ v0.8)

Systems from the architecture spec not yet implemented:

- [ ] Field of view (shadowcasting + explored/visible tile state)
- [ ] Pathfinding (A* on grid, Dijkstra map for AI)
- [ ] Items & inventory (JSON schema, pickup, use, equip)
- [ ] Interaction system (doors, stairs, usable tiles)
- [ ] AI uses pathfinding instead of greedy chase-only movement

**Module:** `RogueEngine.Toolkit` (initial assembly: FOV + Pathfinding)

---

### Phase 9 — World Toolkit (→ v0.9)

Dedicated C# library to **maximize ease of use** for game creators. Full spec: [`docs/planning/WORLD_TOOLKIT.md`](planning/WORLD_TOOLKIT.md).

- [ ] Create `RogueEngine.Toolkit` project (Engine-only deps)
- [ ] `IMapGenerator` plug-in interface + `GeneratorRegistry`
- [ ] Migrate `DungeonGenerator` → `rooms_corridors` algorithm
- [ ] **Cellular automata** cave generator
- [ ] **Drunkard’s walk** tunnel generator
- [ ] **BSP** room generator
- [ ] **Hybrid** cave + rooms generator
- [ ] **Noise** terrain layer (Perlin/Simplex) for outdoor maps
- [ ] **Tile bitmask** (4/8-bit) + tileset JSON + autotile resolver
- [ ] **Overworld** cell graph (regions, connections, biomes, local map per cell)
- [ ] `Data/generators/*.json` schema + loader
- [ ] Helper API: `ActorSpawner`, `MapCarver`, `MapQueries`, `RandomPlacement`, `SeedHelper`
- [ ] Sample game using cellular caves + bitmask walls
- [ ] Sample game using overworld travel between zones

**Algorithms available to user:** pick via JSON `algorithm` field or register custom `IMapGenerator` in scripts.

---

### Phase 10 — Editor world tools (part of v0.9 editor polish)

- [ ] Overworld graph editor (cells + connections visual)
- [ ] Tile bitmask preview / tile painter
- [ ] Generator parameter UI (sliders for fill %, smooth passes, etc.)
- [ ] Live seed preview (regenerate map in editor)

---

### Phase 11 — Installer (→ v0.10)

- [ ] WiX MSI/EXE packaging for editor + runtime distribution

---

## MVP acceptance criteria

The MVP is **done** when all items below pass:

| ID | Criterion | Phase |
|----|-----------|-------|
| CA-1 | Create a new project from the template | 3 |
| CA-2 | Open a SadConsole window with a rendered map | 1 |
| CA-3 | Move the player on a grid with collision | 1 |
| CA-4 | Generate a simple dungeon with rooms and corridors | 2 |
| CA-5 | At least one enemy with simple chase AI | 2 |
| CA-6 | Basic combat between player and enemy | 2 |
| CA-7 | Load entities from JSON | 3 |
| CA-8 | Compile at least one C# script from the project | 4 |
| CA-9 | Generate and compile at least one simple visual script | 7 |
| CA-10 | Save and load basic game state | 3 |
| CA-11 | Export a Windows x64 portable executable | 5 |
| CA-12 | Log build errors on failure | 5 |

### Post-MVP acceptance criteria

| ID | Criterion | Phase |
|----|-----------|-------|
| CA-13 | Player FOV hides unseen tiles; explored tiles remembered | 8 |
| CA-14 | Enemy reaches player via A* around obstacles | 8 |
| CA-15 | Pick dungeon algorithm from JSON (`cellular_caves`, `bsp_dungeon`, etc.) | 9 |
| CA-16 | Wall tiles autotile via bitmask tileset | 9 |
| CA-17 | Overworld with ≥3 connected regions; enter region loads local map | 9 |
| CA-18 | Editor: create project, pick generator, playtest without manual JSON editing | 6 |
| CA-19 | Custom `IMapGenerator` registrable from game script | 9 |

---

## Technical backlog

Priority-ordered items from the architecture specification. Check off when merged.

| ID | Task | Priority | Phase |
|----|------|----------|-------|
| BT-01 | Create repository and .NET solution | High | 1 |
| BT-02 | Implement `TileMap` and `Position` | High | 1 |
| BT-03 | Implement `Entity` and `Component` | High | 1 |
| BT-04 | Minimal SadConsole renderer | High | 1 |
| BT-05 | Input → commands | High | 1 |
| BT-06 | Basic procedural generation | High | 2 |
| BT-07 | Turn manager | High | 2 |
| BT-08 | Simple AI | Medium | 2 |
| BT-09 | Basic combat | Medium | 2 |
| BT-10 | JSON data schemas | Medium | 3 |
| BT-11 | Script compiler | Medium | 4 |
| BT-12 | Build tool | High | 5 |
| BT-13 | Minimal Avalonia editor | Medium | 6 |
| BT-14 | Visual scripting MVP | Low/Medium | 7 |
| BT-15 | WiX installer | Low | 11 |
| BT-16 | FOV (shadowcasting) | High | 8 |
| BT-17 | A* pathfinding on grid | High | 8 |
| BT-18 | Dijkstra map for AI | Medium | 8 |
| BT-19 | Items & inventory JSON + systems | Medium | 8 |
| BT-20 | `RogueEngine.Toolkit` assembly scaffold | High | 9 |
| BT-21 | `IMapGenerator` + `GeneratorRegistry` | High | 9 |
| BT-22 | Cellular automata cave generator | High | 9 |
| BT-23 | Drunkard’s walk generator | Medium | 9 |
| BT-24 | BSP dungeon generator | Medium | 9 |
| BT-25 | Tile bitmask + tileset JSON + autotile | High | 9 |
| BT-26 | Noise terrain generator | Medium | 9 |
| BT-27 | Overworld cell graph + loader | High | 9 |
| BT-28 | `Data/generators/*.json` schema | High | 9 |
| BT-29 | Toolkit helper API (spawn, carve, queries) | Medium | 9 |
| BT-30 | Editor generator picker + parameter UI | Medium | 6/10 |
| BT-31 | Editor overworld graph view | Low | 10 |
| BT-32 | Sample: caves + bitmask template | Medium | 9 |
| BT-33 | Sample: overworld multi-zone game | Medium | 9 |

---

## Repository checklist (pre-code)

- [x] Root `README.md`
- [x] `.gitignore`
- [x] `src/RogueEngine.*` folder scaffold
- [x] `templates/`, `samples/`, `tests/` scaffold
- [x] Architecture document and diagrams in `docs/`
- [x] This roadmap
- [x] .NET solution (`RogueEngine.sln`)
- [x] First passing engine unit test
- [ ] `RogueEngine.Toolkit` project scaffold
- [x] World Toolkit planning spec ([`docs/planning/WORLD_TOOLKIT.md`](planning/WORLD_TOOLKIT.md))
- [ ] Second sample game (procgen variety showcase)

---

## Out of scope (MVP)

- Generic 2D/3D physics
- Complex pixel animation off the grid
- Unity/Godot-scale visual editor
- Online multiplayer
- Asset marketplace / mods distribution
- Mobile support (first cycle)
- Untrusted script sandbox
- Wave Function Collapse / constraint-based PCG (v2 backlog candidate)
- Hex grids

---

## Risks (watch list)

| Risk | Mitigation |
|------|------------|
| Scope creep | Stay roguelike-focused; ship small vertical slices |
| SadConsole coupling | Keep `IRenderer` adapter; engine has no UI deps |
| Visual scripting complexity | Generate C# from graphs; no full interpreter in MVP |
| Untrusted scripts | MVP = trusted dev code only; sandbox later |
| Single-file + assets | Start with portable folder; single-file after |
| Editor becomes huge | Phase 6 = essentials; Phase 10 = world tools incrementally |
| ProcGen algorithm explosion | Ship 3–4 algorithms in v0.9; add more post-1.0 via registry |
| Bitmask tileset authoring pain | Editor tile painter in Phase 10; ship preset tilesets in templates |

---

## Related docs

- [Architecture specification (PDF)](planning/documento_desenvolvimento_momorogue.pdf) — full requirements (legacy **MomoRogue** naming)
- [World Toolkit spec](planning/WORLD_TOOLKIT.md) — procgen, bitmask, overworld, helper API
- [Diagrams](diagrams/) — Mermaid sources
- [Rendered figures](rendered/) — PNG/SVG used by the PDF and README
