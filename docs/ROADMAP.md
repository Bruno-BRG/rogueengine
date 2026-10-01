# Roadmap

## v0.1 — Foundation (done)
- [x] `rogue-core`, `rogue-script` (Lua + mods), `rogue-graph`, `rogue-assets`, `rogue-host`
- [x] Tauri shell

## v0.2 — MVP: make a game end-to-end (done)
- [x] Project format on disk (`game.reproj`, data, scripts, graphs, sprites) with atomic writes
- [x] Starter "Dungeon crawler" template with pixel art, a custom monster, an example script and graph
- [x] Game rules in Lua `base` mod: floors/stairs, XP & levels, data-driven spawn tables, combat log, HUD rows
- [x] UI design system (dark theme), Welcome/project hub, tabbed workspace, toasts, modals
- [x] **Play**: sprite renderer, tweened movement, fog of war, HP bars, inventory/equipment UI, death screen, Lua console, error screen with links to the fix
- [x] **Visual Script**: pan/zoom canvas, typed pins, drag-to-wire, palette search, undo/redo, autosave, live Lua, 55+ nodes
- [x] **Sprites**: pixel editor, frames, animation preview, import/export PNG, undo/redo, strokes as one undo step
- [x] **Objects**: entity/tile forms, built-in overrides, spawn rules, custom data
- [x] **Lua Scripts**: CodeMirror, `rogue.*` autocomplete, syntax check, API reference
- [x] Play flushes pending autosaves first; end-to-end browser tests; CI workflow

## Next
- [ ] **Verify the Tauri build on a machine with WebView deps** (CI job `tauri` is set up but has not run yet; nothing Tauri-specific could be compiled in the authoring environment)
- [ ] Export a standalone game (player-only build) for Windows/macOS/Linux
- [ ] Hand-authored levels: tile-map editor, prefabs, multiple scenes (today levels come from procgen or `rules.build_level`)
- [ ] Game save/load from the UI (world serialization exists; needs registry + Lua state strategy)
- [ ] Animation editor with named clips and state machine UI (runtime exists in `rogue-assets`); per-entity animation triggers (attack/hurt)
- [ ] Built-in UI kit that scripts can drive: dialogs, menus, shops, tooltips, themeable HUD
- [ ] More base systems: status effects, ranged attacks/projectiles, doors/keys/traps, quests, NPC dialogue, multiple classes
- [ ] Graph editor: subgraphs, variables, loops with accumulators, comment boxes, multi-select, "eject to Lua"
- [ ] Mod manager UI (enable/disable, load order, per-mod errors), mod packaging/sharing
- [ ] Audio, particles/screen effects, floating combat text
- [ ] Lua language-server stubs + generated API docs
- [ ] Project settings (map size, generator choice, key bindings) and localization
