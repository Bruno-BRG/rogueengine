# Modding & scripting guide

A mod is a folder: `mods/<id>/mod.toml`, optional `data.json` files, and a Lua entry file.

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
Tag `actor` makes an entity take turns; tag `player` marks the player-controlled one. Items have an `item` block
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
| Misc | `log` `random(lo,hi)` `chance(p)` `register_tile` `register_entity` `mod` (current mod info) |
| Events | `on(name, fn)` `off` — `spawned moved blocked attacked died despawned item_picked item_dropped equipped unequipped turn_started message` |

## Replaceable rules (`rogue.rules.*`)

`damage(attacker, target)` · `ai(id)` → energy cost · `player_action(player, action, arg)` → cost or `false` ·
`new_game(depth)`. Item effects: `rogue.item_effect("name", function(who, item) ... end)`, referenced by `item.on_use`.
Return `false` from an effect to keep the item.

## Visual scripting

Graphs compile to Lua (`rogue-graph`). Add your own nodes with a JSON `NodeDef` (`kind`: event/flow/data, pins,
and a Lua `template` using `${pin}` placeholders) — see `crates/rogue-graph/src/builtin_nodes.json`.

## Rust plugins

Implement `rogue_script::Plugin` to add native functions to the `rogue` table, then `engine.add_plugin(&MyPlugin)`.

## Trust model

Lua runs without `io`/`debug`/process access, but mods are **trusted code**. Only install mods you trust.
