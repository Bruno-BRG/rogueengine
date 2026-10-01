# RogueEngine

An open-source engine **and editor** for 2D tile-based roguelikes. Rust core · Lua scripting · Tauri editor.

> **Status: v0.2 — MVP.** You can create a game, draw its sprites, design monsters and items, script rules visually
> or in Lua, and play it — all inside the editor. See [`docs/ROADMAP.md`](docs/ROADMAP.md) for what's next.

## What you can do today

| Tab | |
|-----|---|
| **Play** | Run your game instantly (F5): sprite renderer, fog of war, HUD, inventory/equipment, message log, death screen, live **Lua console** (`` ` ``). |
| **Visual Script** | Node graphs → Lua. Typed pins, drag-to-wire, searchable palette (`Space`), undo/redo, live generated-Lua panel, 55+ nodes. |
| **Sprites** | Pixel editor: pencil, eraser, line, rect, fill, picker, flip, frames, animation preview, undo/redo, PNG import/export. Auto-saved. |
| **Objects** | Forms for monsters, items, weapons, armor, potions and tiles. Edit built-ins as overrides. Spawn rules by floor and weight. |
| **Lua Scripts** | CodeMirror editor, `rogue.*` autocomplete, live syntax check, clickable API reference. |

The starter project ("Dungeon crawler") is playable immediately: 5 floors of monsters, XP/levels, potions, weapons, armor, stairs.

## Everything is moddable

The engine's own rules — damage, AI, player commands, level generation, item effects, HUD — are plain Lua in the
embedded `base` mod. Your project (and any mod) loads after it and can replace any of them:

```lua
rogue.rules.damage = function(attacker, target)          -- 10% criticals
  local d = rogue.default_damage(attacker, target)
  return rogue.chance(0.1) and d * 2 or d
end
rogue.on("died", function(ev) rogue.log(ev.name .. " is no more") end)
```
New monsters/items are *data* (no code): add an object with a `spawn` rule and it appears in generated dungeons.
See [`docs/MODDING.md`](docs/MODDING.md).

## Layout

| Path | What |
|------|------|
| `crates/rogue-core` | World, entities, tilemap, energy turns, FOV, A*, procgen, inventory/equipment, grid physics |
| `crates/rogue-script` | Lua 5.4 (`mlua`), `rogue.*` API, mod loader, data registry, `Plugin` trait |
| `crates/rogue-graph` | Visual graph → Lua compiler; node types are JSON (mods can add nodes) |
| `crates/rogue-assets` | Sprite editor core (tools, undo/redo, PNG), animation clips & state machine |
| `crates/rogue-host` | Runs the engine on its own thread behind a JSON protocol |
| `crates/rogue-studio` | Projects on disk, sprites, graphs, scripts, objects; `rogue-server` (HTTP) for browser/dev use |
| `src-tauri` | Tauri 2 desktop shell: one command, `api(cmd, args)` |
| `editor` | Svelte 5 + TypeScript UI |
| `e2e` | Playwright tests that drive the real UI |
| `mods` | Example mods |

## Run it

```bash
# Desktop app (needs Tauri system deps: https://tauri.app/start/prerequisites/)
npm install && npm run tauri dev

# …or in a browser, no system deps: backend + UI dev server
cargo run -p rogue-studio --bin rogue-server      # API on 127.0.0.1:1430
npm run dev                                       # UI on http://localhost:1420

# Tests
cargo test --workspace          # engine, scripting, graphs, assets, studio
npm run build                   # svelte-check + bundle
npm run e2e                     # Playwright (needs a built rogue-server + `npm run build`)
```

## Project format

A game is a folder: `game.reproj`, `data/` (entities, tiles), `scripts/*.lua`, `graphs/*.graph.json`,
`sprites/*.png|json`. Plain files — diff them, commit them, edit them by hand.

More: [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) · [`docs/MODDING.md`](docs/MODDING.md)
