# Modding & scripting guide

> **Your game project is itself a mod** that loads after `base`: its `data/*.json`, then every `scripts/*.lua`
> (alphabetical), then every visual graph. Everything below applies to both. Use the editor's **Objects** tab for data
> and **Lua Scripts** for code; this page is the reference.

A standalone mod is a folder: `mods/<id>/mod.toml`, optional `data.json` files, and a Lua entry file.

```toml
id = "my-mod"
name = "My Mod"
version = "0.1.0"
depends = ["base"]        # loaded after these
data = ["data.json"]      # JSON loaded before the entry script
entry = "main.lua"        # default
```

## Data (`data.json`)

```json
{
  "tiles":    { "lava": { "glyph": "~", "walkable": true, "tags": ["hot"] } },
  "entities": {
    "goblin": { "$patch": true, "stats": { "hp": 12 } },
    "spider": { "glyph": "S", "blocks": true, "tags": ["actor", "monster"], "stats": { "hp": 6, "attack": 2 } }
  }
}
```
Same `id` replaces the earlier definition; `"$patch": true` deep-merges instead (set a field to `null` to remove it).
Entities and tiles accept `glyph`, `color` (CSS) and `sprite` (a project sprite name; animated if it has several frames).
Tag `actor` makes an entity take turns; tag `player` marks the player-controlled one.

**Spawn tables (no code):** give an object `data.spawn = { min_depth, max_depth, weight, kind }` (`kind` is `"monster"`
or `"item"`) and the base level generator will place it. Monsters can also define `data.xp` and `data.sight`. Items have an `item` block
(`slot`, `modifiers`, `on_use`). Anything under `data` is yours.

## Lua API (`rogue.*`)

| Area | Functions |
|------|-----------|
| Entities | `spawn(def,x,y)` `despawn` `exists` `get` `find_tagged` `at(x,y)` `in_radius(x,y,r)` |
| Data/tags | `get_data` `set_data` `has_tag` `add_tag` `remove_tag` |
| Movement | `pos` `move(id,dx,dy)` → `ok, blocker` · `teleport` `knockback` `raycast` |
| Stats/combat | `stat` `set_stat` `damage(src,tgt,n)` `heal` `attack(a,t)` `default_damage` |
| Inventory | `pickup` `drop` `equip` `unequip` `inventory` `consume` `use_item` |
| Map | `tile` `set_tile` `walkable` `map_size` `fov(x,y,r)` `path(x0,y0,x1,y1)` `generate_dungeon` `generate_cave` |
| World | `world_get` `world_set` (e.g. `depth`) · `clear_level(keep_ids)` · `definitions()` |
| Misc | `log` `random(lo,hi)` `chance(p)` `register_tile` `register_entity` `mod` (current mod info) |
| Events | `on(name, fn)` `off` — `spawned moved blocked attacked died despawned item_picked item_dropped equipped unequipped turn_started message`. `attacked` carries `attacker_name`/`target_name`; `died` carries `kind`, `name`, `pos` (the entity is already gone). |

## Replaceable rules (`rogue.rules.*`)

`damage(attacker, target)` · `ai(id)` → energy cost · `player_action(player, action, arg)` → cost or `false`
(actions: `move wait pickup use equip unequip drop descend`) · `new_game()` · `build_level(depth, player)` · `descend(player)` ·
`xp_needed(level)` · `level_up(player)` · `hud()` → `{ {label, value}, … }`. Item effects: `rogue.item_effect("name", function(who, item) ... end)`, referenced by `item.on_use`.
Return `false` from an effect to keep the item.

## Visual scripting

Graphs compile to Lua (`rogue-graph`). Add your own nodes with a JSON `NodeDef` (`kind`: event/flow/data, pins,
and a Lua `template` using `${pin}` placeholders) — see `crates/rogue-graph/src/builtin_nodes.json`.

## Rust plugins

Implement `rogue_script::Plugin` to add native functions to the `rogue` table, then `engine.add_plugin(&MyPlugin)`.

## Trust model

Lua runs without `io`/`debug`/process access, but mods are **trusted code**. Only install mods you trust.
