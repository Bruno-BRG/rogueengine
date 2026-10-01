# RogueEngine

An open-source engine and editor for 2D tile-based roguelikes. **Rust core + Lua scripting + Tauri editor.**

> **Status: v0.1 foundation.** The rules core, Lua scripting, mod system, visual-script compiler, sprite-editor
> core and an editor shell are in place and tested. See [`docs/ROADMAP.md`](docs/ROADMAP.md) for what is done and what is next.
> This is a ground-up rewrite; the earlier C#/SadConsole engine was removed (it is in git history).

## Principles

1. **Everything is moddable.** The engine's own rules (damage, AI, new-game, item effects, player commands) are
   plain Lua in the `base` mod. Mods replace or extend them; Rust plugins can extend the Lua API.
2. **Visual and text scripting are the same thing.** Node graphs compile to Lua.
3. **Core is UI-free.** `rogue-core` has no Lua, no Tauri, no window — it is unit-tested headless.
4. **Batteries included.** Inventory, equipment, items, FOV, A*, procgen, grid physics, turn scheduler, animation.

## Layout

| Path | What |
|------|------|
| `crates/rogue-core` | World, entities, tilemap, turns (energy), FOV, A*, procgen, inventory/equipment, grid physics |
| `crates/rogue-script` | Embedded Lua 5.4 (`mlua`), the `rogue.*` API, mod loader, data registry, `Plugin` trait |
| `crates/rogue-graph` | Visual graph → Lua compiler; node types are data (mods can add nodes) |
| `crates/rogue-assets` | Sprite editor core (tools, undo/redo, PNG), animation clips & state machine |
| `crates/rogue-host` | Runs the engine on its own thread behind a JSON request/response protocol |
| `src-tauri` | Tauri 2 app: thin command layer over `rogue-host` / `rogue-assets` |
| `editor` | Editor UI (Vite + TypeScript): game view, visual script, sprite editor, Lua console |
| `mods` | Example mods |
| `docs` | Architecture, modding guide, roadmap |

## Build & test

```bash
# Engine crates: Rust only, no system libs needed
cargo test --workspace

# Editor UI typecheck + bundle
npm install && npm run build

# Desktop app (needs Tauri system deps: https://tauri.app/start/prerequisites/)
npm run tauri dev
```

## A mod in 10 lines

```toml
# mods/my-mod/mod.toml
id = "my-mod"
depends = ["base"]
```
```lua
-- mods/my-mod/main.lua
rogue.rules.damage = function(attacker, target)          -- replace a core rule
  return rogue.default_damage(attacker, target) * 2
end
rogue.on("died", function(ev) rogue.log("something died") end)   -- react to events
```

More: [`docs/MODDING.md`](docs/MODDING.md) · [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
