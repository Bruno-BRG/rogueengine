# RogueEngine Editor — Vision & Master Plan

**Goal:** A professional, GameMaker-inspired desktop IDE for 2D tile-based roguelikes — not a clone of Unity/Godot, but with the **same mental model**: project hub → resource browser → scene tabs → inspectors → playtest → export.

**Reference engines:** GameMaker (layout & asset browser), Godot (scene tree, dockable panels, open-source patterns), Unity (project window, inspector, play mode).

**Constraint:** `RogueEngine.Engine` stays UI-free. All editor code lives in `RogueEngine.Editor`.

---

## Architecture overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PROJECT LAUNCHER (startup)                                              │
│  • Recent projects   • New / Open   • Templates   • Learn / Docs        │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │ open project
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  EDITOR SHELL                                                            │
│  ┌─────────┬──────────────────────────────────────┬──────────────────┐  │
│  │ RESOURCE│  [Scene] [Actor] [Script] ... tabs   │    INSPECTOR     │  │
│  │  TREE   │  ┌────────────────────────────────┐  │  (context props) │  │
│  │         │  │  WORKSPACE (editor per tab)    │  │                  │  │
│  │ Scenes  │  └────────────────────────────────┘  │                  │  │
│  │ Actors  │                                        │                  │  │
│  │ Scripts │                                        │                  │  │
│  │ Visual  │                                        │                  │  │
│  │ Sprites │                                        │                  │  │
│  │ Items   │                                        │                  │  │
│  └─────────┴──────────────────────────────────────┴──────────────────┘  │
│  OUTPUT CONSOLE (build / validate / playtest logs)                       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Phase E1 — Project hub & shell (v0.7.1) ✅ *in progress*

| Feature | GM | Godot | Unity | RogueEngine |
|---------|----|-------|-------|-------------|
| Startup launcher | ✅ | ✅ | Hub | ✅ ProjectLauncherWindow |
| Recent projects | ✅ | ✅ | ✅ | ✅ JSON in AppData |
| New from template | ✅ | ✅ | ✅ | ✅ TemplateService |
| Resource tree | Asset browser | FileSystem dock | Project window | ✅ TreeView |
| Document tabs | Workspace tabs | Scene tabs | Editor tabs | ✅ EditorDocumentTab |
| Inspector panel | Properties | Inspector | Inspector | ✅ Right dock |
| Dark GameMaker theme | ✅ | ✅ | ✅ | ✅ EditorTheme.axaml |
| Back to projects | — | — | Hub | ✅ Toolbar |

---

## Phase E2 — Scenes & maps (v0.8 editor)

| Feature | Description |
|---------|-------------|
| Scene JSON (`Data/scenes/*.scene.json`) | Id, name, linked generator, spawn points |
| Scene tab | Grid map painter (tiles, entities) |
| Multi-scene project | Tab per scene; default scene in `game.reproj` |
| Live preview | Regenerate procgen with seed slider |
| Scene transitions | Stairs / portals (engine Phase 8) |

**Godot inspiration:** `PackedScene` equivalent = scene JSON + referenced assets.

---

## Phase E3 — Sprites & tilesets (v0.9 editor)

| Feature | Description |
|---------|-------------|
| Sprite assets | `Assets/Sprites/` — PNG + metadata JSON |
| Glyph fallback | ASCII roguelike mode (current) |
| Tileset editor | Bitmask autotile preview (World Toolkit) |
| Sprite picker | Assign sprite to actor in inspector |
| Animation strips | Frame index + FPS (2D grid games) |

**GameMaker inspiration:** Sprite editor with origin point; we use grid cell origin.

---

## Phase E4 — Code & visual scripting (v0.8–0.9 editor)

| Feature | Status |
|---------|--------|
| C# script list in tree | ✅ v0.7.1 |
| In-editor code view | ✅ read-only MVP |
| Roslyn diagnostics inline | Planned |
| Visual script graph canvas | Planned (drag-drop nodes) |
| Breakpoints in playtest | Future |

**Godot inspiration:** Script icon per resource; open in docked editor.

---

## Phase E5 — Entities, items, components (v0.8–1.0)

| Feature | Description |
|---------|-------------|
| Actor editor | ✅ MVP forms |
| Item definitions | JSON + inspector |
| Component inspector | HP, AI, inventory, FOV |
| Prefab / archetype | Reusable actor templates |
| Entity placement on scene | Drag from browser to map |

---

## Phase E6 — Play mode & debugging (v1.0)

| Feature | GM | Godot | Unity | Plan |
|---------|----|-------|-------|------|
| Playtest (F5) | ✅ | ✅ | ✅ | ✅ |
| Stop play | ✅ | ✅ | ✅ | Planned |
| Pause | ✅ | ✅ | ✅ | Planned |
| In-game console | ✅ | ✅ | ✅ | Planned |
| Edit during play (optional) | — | — | Limited | Out of scope v1 |

---

## Phase E7 — Build & export UI (v1.0)

| Feature | Description |
|---------|-------------|
| Build panel | Platform, output path, ZIP |
| Build profiles | Debug / Release |
| One-click publish | `rogueengine build` wrapper |
| Installer trigger | WiX Phase 11 |

---

## Phase E8 — Advanced docks (post-1.0)

| Feature | Godot reference |
|---------|-----------------|
| Dockable panels | `EditorDock` split containers |
| Layout presets | 2D / scripting / debugging |
| Search everywhere | Ctrl+Shift+P command palette |
| Plugin API | Editor extensions in C# |

---

## Resource tree structure (target)

```
📁 {ProjectName}
├── ⚙ Project Settings
├── 🎬 Scenes
│   ├── main.scene
│   └── …
├── 👾 Actors
│   ├── player
│   └── goblin
├── 📦 Items
├── 🎨 Sprites
├── 🧱 Tilesets
├── 📜 Scripts (C#)
├── 🧠 Visual Scripts
├── 🗺 Generators
└── 📋 Data
```

---

## File & module map

| Path | Role |
|------|------|
| `Views/ProjectLauncherWindow.*` | Startup hub |
| `Views/EditorShellWindow.*` | Main IDE |
| `Themes/EditorTheme.axaml` | Shared GameMaker dark theme |
| `Services/RecentProjectsService.cs` | Persist recents |
| `Services/ResourceTreeService.cs` | Build asset tree |
| `Services/EditorNavigationService.cs` | Launcher ↔ shell |
| `Models/EditorDocumentTab.cs` | Open editor tabs |
| `Models/EditorResourceNode.cs` | Tree nodes |
| `Engine/Data/SceneDefinition.cs` | Scene schema |

---

## What we are NOT building (scope guard)

- 3D viewports, physics editors, animation state machines at Unity scale
- Online asset store, cloud sync, collaboration
- Full C# IDE replacement (use VS/Rider for heavy scripting)
- Untrusted mod sandbox in editor

---

## Success criteria (editor 1.0)

1. User opens launcher → picks recent project → lands in shell with tree populated.
2. User opens Scene, Actor, Script, Visual Script each in its own tab.
3. Inspector edits persist via Save; Validate + Playtest + Build work from toolbar.
4. UI is consistently dark, spaced, and navigable without reading JSON on disk.
5. New user can create a game from template without touching the command line.

---

## Related

- [ROADMAP.md](../ROADMAP.md) — engine phases 8–11
- [WORLD_TOOLKIT.md](WORLD_TOOLKIT.md) — procgen & tile tools for scene editor
- [AGENTS.md](../../AGENTS.md) — module dependency rules
