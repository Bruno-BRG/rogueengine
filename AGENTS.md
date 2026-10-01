# AGENTS.md — RogueEngine

Instructions for AI coding agents working in this repository.

## What this is

RogueEngine: open-source engine + editor for 2D tile-based roguelikes. **Rust core, Lua scripting, Tauri editor.**
Scope is deliberately narrow (grid maps, entities, turns, procgen, FOV, pathfinding, scripting, mods) — not a general engine.
This is a ground-up rewrite; the old C# engine is in git history only.

## Architecture rules

- `rogue-core` must not depend on Lua, Tauri, or any UI. Game rules live here or in Lua — never in the frontend.
- Lua reaches the world **only** through `rogue.*` (`crates/rogue-script/src/api.rs`). Never call Lua while the
  `World` `RefCell` is borrowed (re-entrancy panic). Formulas that mods should change belong in Lua (`lua/base/main.lua`)
  as overridable `rogue.rules.*`, not hard-coded in Rust.
- `rogue-host` is the single protocol surface for any frontend. Add UI features as `Request`/`Response` variants there,
  keep `src-tauri` a thin pass-through, and keep `editor/src/api.ts` types in sync (the wire-format test guards this).
- `src-tauri` is **its own cargo workspace** (needs WebView system libs); the root workspace must keep building headless.
- Node types, tiles and entities are data (JSON / Lua tables) so mods can add them without Rust changes.

## Workflow

1. Read `docs/ROADMAP.md`; pick one item; keep scope small.
2. Match existing patterns. Public Lua API changes → update `docs/MODDING.md`.
3. Verify: `cargo test --workspace` and `npm run build`. Add a test for every rule/bug fix (tests live next to the crate).
4. Tick `docs/ROADMAP.md` when a deliverable lands.
5. Commit/PR only when the user asks; never force-push `main`.

## Security note

Lua is stripped of `io`, `debug`, `dofile`, `loadfile` and dangerous `os.*`, but this is **not** a hostile-code sandbox.
Document mods as "trusted code, like plugins". Graph compilation must keep escaping string literals (`lua_string`).
