# RogueEngine Roadmap

Living development plan for the **rogueengine** repository. Update this file as phases complete.

**Current status:** `0.0 — Planning` (repository scaffold, architecture doc, no runtime code)

**Last updated:** 2026-06-11

---

## Version milestones

| Version | Focus | Expected outcome | Status |
|---------|-------|------------------|--------|
| **0.0** | Planning | Architecture doc, repo layout, README | Done |
| **0.1** | Minimal runtime | Window, map, player, movement, collision | Not started |
| **0.2** | Minimal roguelike | Dungeon, turns, enemy, combat, message log | Not started |
| **0.3** | Data & project | `game.reproj`, JSON entities/items, save/load | Not started |
| **0.4** | C# scripting | Compiled scripts and custom behaviors | Not started |
| **0.5** | Portable build | Windows x64 portable export | Not started |
| **0.6** | Basic editor | Create, open, edit data, playtest, export | Not started |
| **0.7** | Visual scripting MVP | Simple graphs compiled to C# | Not started |
| **0.8** | Installer | MSI/EXE via WiX | Not started |
| **1.0** | Initial release | Engine usable for at least two sample games | Not started |

---

## Implementation phases

### Phase 1 — Playable core (→ v0.1)

- [ ] Create .NET solution and project references
- [ ] Implement `World`, `TileMap`, `Entity`, basic components
- [ ] Minimal SadConsole renderer (`RogueEngine.SadConsole`)
- [ ] Input → command mapping
- [ ] Movement and collision

**Modules:** `RogueEngine.Engine`, `RogueEngine.SadConsole`, `RogueEngine.Runtime`

---

### Phase 2 — Minimal roguelike (→ v0.2)

- [ ] Simple procedural dungeon (rooms + corridors)
- [ ] Turn system (`TurnManager`)
- [ ] Basic enemy AI (chase)
- [ ] Basic combat
- [ ] Message log and minimal UI layer

**Engine areas:** ProcGen, TurnBased, Rules, AI

---

### Phase 3 — Project & data (→ v0.3)

- [ ] Define `game.reproj` schema
- [ ] Load actors/items/maps from JSON
- [ ] Initial data schemas
- [ ] Save / load game state
- [ ] Starter template in `templates/BasicRoguelikeProject/`

---

### Phase 4 — Scripting (→ v0.4)

- [ ] Behavior interfaces for entities
- [ ] Compile C# scripts at build time (Roslyn)
- [ ] Friendly compile errors
- [ ] Bind scripts to entities via JSON

---

### Phase 5 — Build tool (→ v0.5)

- [ ] CLI: `rogueengine build`
- [ ] Validate `game.reproj`
- [ ] Copy assets, compile scripts, run `dotnet publish`
- [ ] Portable folder output
- [ ] ZIP portable export
- [ ] Prepare path for single-file publish (later)

**Module:** `RogueEngine.BuildTool`

---

### Phase 6 — Editor (→ v0.6)

- [ ] Avalonia desktop shell (`RogueEngine.Editor`)
- [ ] Create / open project
- [ ] Edit core data files
- [ ] Playtest (launch runtime)
- [ ] Trigger build tool from UI

---

### Phase 7 — Visual scripting (→ v0.7)

- [ ] Minimal graph editor
- [ ] Node types: event, condition, action, log, spawn, open door
- [ ] Graph JSON → generated C#
- [ ] Compile generated code in build pipeline

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
| BT-15 | WiX installer | Low | 8 |

---

## Repository checklist (pre-code)

- [x] Root `README.md`
- [x] `.gitignore`
- [x] `src/RogueEngine.*` folder scaffold
- [x] `templates/`, `samples/`, `tests/` scaffold
- [x] Architecture document and diagrams in `docs/`
- [x] This roadmap
- [ ] .NET solution (`RogueEngine.sln`)
- [ ] First passing engine unit test

---

## Out of scope (MVP)

- Generic 2D/3D physics
- Complex pixel animation off the grid
- Unity/Godot-scale visual editor
- Online multiplayer
- Asset marketplace / mods distribution
- Mobile support (first cycle)
- Untrusted script sandbox

---

## Risks (watch list)

| Risk | Mitigation |
|------|------------|
| Scope creep | Stay roguelike-focused; ship small vertical slices |
| SadConsole coupling | Keep `IRenderer` adapter; engine has no UI deps |
| Visual scripting complexity | Generate C# from graphs; no full interpreter in MVP |
| Untrusted scripts | MVP = trusted dev code only; sandbox later |
| Single-file + assets | Start with portable folder; single-file after |
| Editor becomes huge | Editor only creates, opens, validates, playtests, exports |

---

## Related docs

- [Architecture specification (PDF)](planning/documento_desenvolvimento_momorogue.pdf) — full requirements (legacy **MomoRogue** naming)
- [Diagrams](diagrams/) — Mermaid sources
- [Rendered figures](rendered/) — PNG/SVG used by the PDF and README
