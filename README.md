# RogueEngine

A specialized 2D tile-based roguelike engine in C#. RogueEngine is not a general-purpose game engine like Unity or Godot — it focuses on a narrower domain: grid maps, entities, turn-based gameplay, procedural generation, field of view, pathfinding, gameplay scripts, and exporting finished games as executables or installers.

**Status:** v0.9 — items/inventory, World Toolkit (pluggable generators, bitmask autotile, overworld MVP), editor item & generator UI.

## What is this?

RogueEngine provides a modular platform for building roguelike games with C# as the primary language and [SadConsole](https://sadconsole.com/) as the rendering and input adapter. Game logic lives in `RogueEngine.Engine`, which stays decoupled from SadConsole so the core can be tested without a graphical window and could support other renderers in the future.

## Core principle

> *SadConsole is the screen; RogueEngine.Engine is the game rules.*

## Architecture at a glance

```
Developer → RogueEngine.Editor → game.reproj
                ↓
         RogueEngine.Runtime ← RogueEngine.Engine ← game assets & scripts
                ↓
         RogueEngine.SadConsole → SadConsole → MonoGame / window / input

RogueEngine.BuildTool → dotnet publish → portable ZIP / EXE / installer
```

See the full architecture diagram in [docs/rendered/01_contexto_arquitetura.svg](docs/rendered/01_contexto_arquitetura.svg). Note: planning documents in `docs/` still use the legacy draft name **MomoRogue**; RogueEngine is the canonical product name going forward.

## Repository layout

| Path | Purpose |
|------|---------|
| `src/RogueEngine.Engine/` | Core game rules — world, entities, systems, save/load. No UI dependencies. |
| `src/RogueEngine.Toolkit/` | FOV, pathfinding, procgen, bitmask autotile, overworld helpers. |
| `src/RogueEngine.SadConsole/` | Renderer and input adapter for SadConsole. |
| `src/RogueEngine.Runtime/` | Application that loads and runs a game project. |
| `src/RogueEngine.Editor/` | Desktop editor for creating and configuring projects (Avalonia). |
| `src/RogueEngine.BuildTool/` | CLI for validating, compiling, and exporting games. |
| `templates/` | Starter game project templates. |
| `samples/` | Example games built with the engine. |
| `tests/` | Unit and integration tests. |
| `docs/` | Architecture document, Mermaid diagrams, and rendered figures. |

## Tech stack

| Layer | Choice |
|-------|--------|
| Language | [C#](https://learn.microsoft.com/dotnet/csharp/) / [.NET](https://dotnet.microsoft.com/) |
| Rendering | [SadConsole](https://sadconsole.com/) |
| Editor UI | [Avalonia](https://docs.avaloniaui.net/) |
| Scripting | C# + [Roslyn](https://github.com/dotnet/roslyn) |
| Export | `dotnet publish`, [WiX](https://www.firegiant.com/wixtoolset/) (installer, later) |
| Initial target | Windows x64 |

## Game project structure (planned)

A game created with RogueEngine is a versionable folder:

```
MyGame/
  game.reproj
  Assets/
    Tilesets/
    Audio/
    Fonts/
  Data/
    actors.json
    items.json
    maps.json
  Scripts/
    PlayerController.cs
  VisualScripts/
    door_interaction.graph.json
  Build/
```

The starter template lives in `templates/BasicRoguelikeProject/`.

## Roadmap

**Current version:** 0.7 (visual scripting MVP)

Full version milestones, implementation phases, MVP criteria, and technical backlog: **[`docs/ROADMAP.md`](docs/ROADMAP.md)**

| Version | Focus |
|---------|-------|
| 0.1 | Minimal runtime |
| 0.2 | Minimal roguelike |
| 0.3 | Data & `game.reproj` |
| 0.4–0.5 | Scripting & portable build |
| 0.6 | Basic editor |
| 0.7 | Visual scripting MVP |
| 0.8–0.10 | FOV, World Toolkit, installer |
| 1.0 | Initial release |

## Documentation

| Resource | Location |
|----------|----------|
| Roadmap (living) | [`docs/ROADMAP.md`](docs/ROADMAP.md) |
| AI agent guide | [`AGENTS.md`](AGENTS.md) |
| Doc index | [`docs/README.md`](docs/README.md) |
| Architecture PDF | [`docs/planning/`](docs/planning/) |
| Diagrams | [`docs/diagrams/`](docs/diagrams/), [`docs/rendered/`](docs/rendered/) |

## Getting started

```bash
dotnet build
dotnet test
dotnet run --project src/RogueEngine.Editor
dotnet run --project src/RogueEngine.Runtime -- templates/BasicRoguelikeProject/game.reproj
dotnet run --project src/RogueEngine.BuildTool -- build templates/BasicRoguelikeProject/game.reproj
```

Exported game runs as: `Build/BasicRoguelike.exe game.reproj`

## License

TBD
