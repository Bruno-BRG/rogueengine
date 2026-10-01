# Roadmap

## v0.1 — Foundation (done)
- [x] `rogue-core`: world, generational entities, tilemap, energy turns, FOV, A*, procgen (rooms, caves), inventory/equipment, grid physics (raycast, knockback), save/load serialization
- [x] `rogue-script`: Lua 5.4, `rogue.*` API, events→hooks, overridable rules, mod loader (deps, `$patch`), sandbox, Rust `Plugin` trait
- [x] `rogue-graph`: graph→Lua compiler with data-defined nodes, injection-safe literals, cycle detection
- [x] `rogue-assets`: sprite editing core (pencil/line/rect/fill/flip, frames, undo/redo, PNG), animation clips + state machine
- [x] `rogue-host`: engine thread + JSON protocol
- [x] Tauri shell + editor UI skeleton (game view, graph editor, sprite editor, Lua console)

## Next
- [ ] **Verify the Tauri build on a machine with WebView deps** (not compiled in the authoring environment) and add CI (Linux/Win/macOS)
- [ ] Project format `game.reproj` (folder: assets, data, mods, graphs, sprites) + save/open in the editor
- [ ] Wire sprites/animations into the game renderer (tiles/entities draw sprites; `animator` driven by events)
- [ ] Game save/load commands (world serialization exists; needs registry + Lua state strategy)
- [ ] Built-in UI kit: menus, dialogs, inventory/equipment screens, HUD, tooltips — themeable and Lua-scriptable
- [ ] More roguelike systems as base-mod Lua: status effects, ranged attacks/projectiles, doors/keys/traps, levels & XP, shops, quests, multi-floor
- [ ] Animation editor UI (timeline, clip editor) and object/prefab manager
- [ ] Graph editor: subgraphs, loops, variables, search palette, graph→Lua "eject", save/load graphs in project
- [ ] Mod manager UI (enable/disable, load order, per-mod errors), mod packaging
- [ ] Audio, export/packaging of standalone games
- [ ] Lua API reference generation + language-server stubs
