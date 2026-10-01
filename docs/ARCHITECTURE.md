# Architecture

```
 editor (Svelte)  ── call(cmd,args) ──▶  Tauri `api`  ─┐                    ┌▶ rogue-host ─thread▶ rogue-script ▶ rogue-core
  Play / Graph / Sprites /    └─ HTTP POST /api/cmd ─▶ rogue-server ─┴▶ rogue-studio ┤           (Lua + mods)      (pure rules)
  Objects / Scripts                                                                  ├▶ rogue-assets (sprites, animation)
                                                                                     └▶ rogue-graph  (graph → Lua)
```
`rogue-studio` owns the project on disk and builds it into a mod for `rogue-host` at every new game.

## Why this shape

- **Lua state is `!Send`**, so `rogue-host` owns the engine on one thread and exposes a channel. Any frontend
  (Tauri, CLI, tests, a future web build) talks the same JSON protocol.
- **Rules in Lua, mechanisms in Rust.** Rust provides fast, safe primitives (movement, FOV, A*, damage application,
  inventory). Policy (damage formula, AI, level generation, item effects, command handling) is Lua in the `base` mod.
  Overriding a rule is assigning a function.
- **Events decouple everything.** Core mutations emit `Event`s (`moved`, `attacked`, `died`, `item_picked`...). The script
  layer dispatches them to `rogue.on(...)` handlers; a renderer can use the same stream for animation/sound.
- **Entities are a bag of parts + free-form `data`/`tags`.** Built-ins: stats, inventory, item info. Everything else
  (hunger, mana, factions) is mod data — no Rust change.
- **IDs are generational** (`index | gen << 32`, serialized as `u64`): stale handles never hit a recycled entity,
  and Lua/JS get a plain integer.
- **Turns are energy-based**: each actor gains `speed` energy per tick and acts at 100. Fast/slow actors, haste and
  action costs fall out naturally (`player_action`/`ai` return an energy cost).

## Mod loading

`mod.toml` → dependency-ordered load (cycles/missing deps are errors) → data files (`$patch` deep-merges) → `main.lua`.
`base` is embedded and always first.

## Determinism

World RNG is serialized with the world; same seed + same inputs ⇒ same game (tested for level generation).
Mods should use `rogue.random`/`rogue.chance`, not `math.random`.
