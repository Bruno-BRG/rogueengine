# AGENTS.md — RogueEngine

Instructions for AI coding agents working in this repository.

## Project summary

**RogueEngine** (`rogueengine`) is a specialized C# engine for 2D tile-based roguelikes. It is **not** a general-purpose game engine. Scope is intentionally narrow: grid maps, entities, turn-based rules, procgen, FOV, pathfinding, scripting, and game export.

**Status:** v0.5 — portable build. Phases 1–5 done; World Toolkit planned for v0.9.

## Canonical naming

| Context | Use |
|---------|-----|
| Repository / CLI | `rogueengine` |
| Product / assemblies | `RogueEngine` |
| Namespaces | `RogueEngine.*` |
| Game project file | `game.reproj` |
| Build command (future) | `rogueengine build` |

Legacy name **MomoRogue** appears only in older planning PDFs/diagrams under `docs/planning/`. New code and docs must use **RogueEngine**.

## Core architectural rule

> **SadConsole is the screen; RogueEngine.Engine is the game rules.**

| Module | May depend on | Must not depend on |
|--------|---------------|-------------------|
| `RogueEngine.Engine` | BCL only | SadConsole, Avalonia, UI |
| `RogueEngine.Toolkit` | Engine | SadConsole, Avalonia, UI |
| `RogueEngine.SadConsole` | Engine, SadConsole | Avalonia |
| `RogueEngine.Runtime` | Engine, SadConsole adapter | Editor UI |
| `RogueEngine.Editor` | Engine, BuildTool (later) | — |
| `RogueEngine.BuildTool` | Engine, Roslyn, dotnet CLI | SadConsole, Avalonia |

Engine logic must be testable **without** opening a graphical window. Use `IRenderer` (or equivalent) at boundaries.

## Repository layout

```
src/RogueEngine.Engine/       # Core: World, Entity, systems, save/load
src/RogueEngine.Toolkit/      # ProcGen, bitmask, overworld, FOV, pathfinding, helpers (planned v0.8–0.9)
src/RogueEngine.SadConsole/   # Render + input adapter
src/RogueEngine.Runtime/      # Runs a game project
src/RogueEngine.Editor/       # Avalonia desktop tool (later phases)
src/RogueEngine.BuildTool/    # CLI export pipeline (later phases)
tests/                        # xUnit (or project default) tests
templates/                    # Starter game projects
samples/                      # Example games
docs/                         # ROADMAP, architecture PDF, diagrams
```

## Read before coding

1. [`README.md`](README.md) — overview and links
2. [`docs/ROADMAP.md`](docs/ROADMAP.md) — **current phase, backlog, MVP criteria** (update when completing work)
3. [`docs/planning/documento_desenvolvimento_momorogue.pdf`](docs/planning/documento_desenvolvimento_momorogue.pdf) — full spec (legacy naming)
4. [`docs/diagrams/`](docs/diagrams/) — architecture diagrams

## What to build next

Follow [`docs/ROADMAP.md`](docs/ROADMAP.md). **Phase 1** is the active target:

- .NET solution + project references
- `World`, `TileMap`, `Entity`, basic components
- Minimal SadConsole renderer
- Input → commands
- Movement and collision

Do not jump ahead to editor, visual scripting, or installer unless explicitly requested.

## C# conventions (when adding code)

- **Target:** .NET (LTS version TBD at Phase 1; prefer current LTS)
- **Style:** idiomatic C#, `nullable` enabled, clear public APIs on Engine
- **ECS:** pragmatic entities + typed components + systems — not pure ECS dogma
- **Tests:** unit tests for Engine rules in `tests/RogueEngine.Engine.Tests/`; no window required
- **Namespaces:** match folder/project name (`RogueEngine.Engine.Core`, etc.)
- **Public API:** game scripts and JSON loaders only touch documented public surface; keep internals `internal`

### Dependency direction (allowed)

```
Engine ← Toolkit, SadConsole, Runtime, BuildTool, Editor
Engine ← Tests
Toolkit ← Runtime, Editor, BuildTool, Tests
```

Never: `Engine → SadConsole` or `Engine → Avalonia`.

## Out of scope (do not add without explicit request)

- Generic physics, 3D, multiplayer
- Unity/Godot-scale editor features
- Mobile, marketplace, mod sandbox
- Rewriting the LaTeX planning doc (unless asked)
- Large refactors unrelated to the current task

## Agent workflow

1. **Scope small** — one phase item or backlog ID at a time
2. **Match existing patterns** — read surrounding code before adding abstractions
3. **Verify** — `dotnet build`, `dotnet test` when solution exists
4. **Update docs** — check off items in `docs/ROADMAP.md` when a phase deliverable is done
5. **Commits** — only when the user asks; never force-push `main`
6. **graphify** — if `graphify-out/graph.json` exists, use `graphify query` for codebase questions; run `graphify update .` after C# changes

## Game project format (reference)

Games are folders, not monolithic binaries:

```
MyGame/
  game.reproj
  Assets/  Data/  Scripts/  VisualScripts/  Build/
```

Template lives in `templates/BasicRoguelikeProject/` (Phase 3).

## Definition of done (from spec)

A feature is done when it:

- Lives in the correct module
- Has a unit test or documented manual test reason
- Does not leak UI deps into Engine
- Logs errors where applicable
- Is reflected in `docs/ROADMAP.md` if it closes a milestone item

## Useful commands (after Phase 1)

```bash
dotnet build
dotnet test
dotnet run --project src/RogueEngine.Runtime
```

Build tool (Phase 5+): `rogueengine build` — not implemented yet.
