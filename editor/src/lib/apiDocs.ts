export interface ApiDoc { group: string; name: string; sig: string; doc: string; snippet: string }

const d = (group: string, name: string, sig: string, doc: string, snippet = sig): ApiDoc => ({ group, name, sig, doc, snippet });

/** Reference for the `rogue.*` Lua API (kept in sync with docs/MODDING.md). */
export const API: ApiDoc[] = [
  d("Events", "on", "rogue.on(event, fn)", "Run fn(ev) when an event happens: spawned, moved, blocked, attacked, died, despawned, item_picked, item_dropped, equipped, unequipped, turn_started, message.", 'rogue.on("died", function(ev)\n  \nend)'),
  d("Events", "off", "rogue.off(event, fn)", "Remove a handler added with rogue.on."),

  d("Entities", "spawn", "rogue.spawn(def, x, y) → id", "Create an entity from an object definition. Returns its id.", 'rogue.spawn("goblin", x, y)'),
  d("Entities", "despawn", "rogue.despawn(id)", "Remove an entity from the world."),
  d("Entities", "exists", "rogue.exists(id) → bool", "True if the entity is still alive in the world."),
  d("Entities", "get", "rogue.get(id) → table", "Snapshot of an entity: name, kind, pos, stats, tags, data, item…"),
  d("Entities", "find_tagged", "rogue.find_tagged(tag) → ids", "All entities having a tag.", 'rogue.find_tagged("monster")'),
  d("Entities", "at", "rogue.at(x, y) → ids", "Entities standing on a tile."),
  d("Entities", "in_radius", "rogue.in_radius(x, y, r) → ids", "Entities within r tiles (square distance).", "rogue.in_radius(x, y, 3)"),
  d("Entities", "get_data", "rogue.get_data(id, key)", "Read your own per-entity value (hunger, mana, faction…).", 'rogue.get_data(id, "mana")'),
  d("Entities", "set_data", "rogue.set_data(id, key, value)", "Store any value on an entity. nil removes it.", 'rogue.set_data(id, "mana", 10)'),
  d("Entities", "has_tag", "rogue.has_tag(id, tag) → bool", "Check a tag.", 'rogue.has_tag(id, "monster")'),
  d("Entities", "add_tag", "rogue.add_tag(id, tag)", "Add a tag.", 'rogue.add_tag(id, "burning")'),
  d("Entities", "remove_tag", "rogue.remove_tag(id, tag)", "Remove a tag."),

  d("Movement", "pos", "rogue.pos(id) → {x,y}", "Current tile of an entity."),
  d("Movement", "move", "rogue.move(id, dx, dy) → ok, blocker", "Step by (dx,dy). Returns false and the blocking entity if blocked.", "local ok, blocker = rogue.move(id, dx, dy)"),
  d("Movement", "teleport", "rogue.teleport(id, x, y)", "Place an entity directly on a tile."),
  d("Movement", "knockback", "rogue.knockback(id, dx, dy, distance)", "Push an entity away until it hits something. Returns tiles moved."),
  d("Movement", "raycast", "rogue.raycast(x0, y0, x1, y1, range) → {kind,x,y,entity}", "Trace a projectile. kind is wall, entity or clear."),

  d("Combat", "stat", "rogue.stat(id, name) → number", "Effective stat including equipment: hp, max_hp, attack, defense, speed.", 'rogue.stat(id, "attack")'),
  d("Combat", "set_stat", "rogue.set_stat(id, name, value)", "Change a base stat."),
  d("Combat", "damage", "rogue.damage(source, target, amount)", "Apply damage (source may be nil). Fires attacked / died.", "rogue.damage(attacker, target, 5)"),
  d("Combat", "heal", "rogue.heal(id, amount)", "Restore HP up to max."),
  d("Combat", "attack", "rogue.attack(attacker, target) → dmg", "Melee attack using rogue.rules.damage."),
  d("Combat", "default_damage", "rogue.default_damage(a, t) → number", "The built-in damage formula (attack − defense ± 1)."),

  d("Inventory", "pickup", "rogue.pickup(who, item) → bool", "Pick up an item at the same tile."),
  d("Inventory", "drop", "rogue.drop(who, item) → bool", "Drop an item."),
  d("Inventory", "equip", "rogue.equip(who, item) → bool", "Equip into the item's slot (swaps the old one)."),
  d("Inventory", "unequip", "rogue.unequip(who, item) → bool", "Unequip an item."),
  d("Inventory", "inventory", "rogue.inventory(who) → ids", "Items carried by an entity."),
  d("Inventory", "use_item", "rogue.use_item(who, item) → bool", "Run the item's on_use effect and consume it."),
  d("Inventory", "item_effect", "rogue.item_effect(name, fn)", "Define an effect that items reference with item.on_use. Return false to keep the item.", 'rogue.item_effect("fireball", function(who, item)\n  \nend)'),
  d("Inventory", "consume", "rogue.consume(item)", "Remove a carried item."),

  d("Map", "tile", "rogue.tile(x, y) → table", "Tile definition at a position (id, name, walkable, tags…)."),
  d("Map", "set_tile", "rogue.set_tile(x, y, id)", "Change a tile.", 'rogue.set_tile(x, y, "floor")'),
  d("Map", "walkable", "rogue.walkable(x, y) → bool", "Can something stand here?"),
  d("Map", "map_size", "rogue.map_size() → w, h", "Map dimensions."),
  d("Map", "fov", "rogue.fov(x, y, radius) → {{x,y}…}", "Tiles visible from a position."),
  d("Map", "path", "rogue.path(x0, y0, x1, y1) → {{x,y}…}|nil", "A* path between two tiles."),
  d("Map", "generate_dungeon", "rogue.generate_dungeon(floor, wall) → room centers", "Fill the map with rooms and corridors.", 'rogue.generate_dungeon("floor", "wall")'),
  d("Map", "generate_cave", "rogue.generate_cave(floor, wall)", "Fill the map with a cave."),

  d("World", "world_get", "rogue.world_get(key)", "Read a world-wide value (depth, quest flags…).", 'rogue.world_get("depth")'),
  d("World", "world_set", "rogue.world_set(key, value)", "Store a world-wide value."),
  d("World", "clear_level", "rogue.clear_level(keep_ids)", "Remove everything except the listed entities (and what they carry).", "rogue.clear_level({ player })"),
  d("World", "definitions", "rogue.definitions() → table", "All object definitions by id."),
  d("World", "register_entity", "rogue.register_entity(id, def)", "Define a new object from Lua.", 'rogue.register_entity("bat", { name = "Bat", glyph = "b", blocks = true,\n  stats = { hp = 3, max_hp = 3 }, tags = { "actor", "monster" } })'),
  d("World", "register_tile", "rogue.register_tile(id, def)", "Define a new tile from Lua.", 'rogue.register_tile("lava", { name = "Lava", glyph = "~", color = "#ff5a1f" })'),

  d("Misc", "log", "rogue.log(text)", "Write to the message log.", 'rogue.log("Hello!")'),
  d("Misc", "random", "rogue.random(lo, hi) → int", "Deterministic random integer (use this, not math.random).", "rogue.random(1, 6)"),
  d("Misc", "chance", "rogue.chance(p) → bool", "True with probability p (0–1).", "rogue.chance(0.25)"),

  d("Rules", "rules.damage", "rogue.rules.damage = function(attacker, target) … end", "Replace the damage formula.", "rogue.rules.damage = function(attacker, target)\n  return rogue.default_damage(attacker, target)\nend"),
  d("Rules", "rules.ai", "rogue.rules.ai = function(id) … end", "Replace monster behaviour. Return the energy cost (default 100).", "rogue.rules.ai = function(id)\n  return 100\nend"),
  d("Rules", "rules.player_action", "rogue.rules.player_action = function(player, action, arg) … end", "Add or change player commands. Return the energy cost, or false for no time passing.", 'local base_action = rogue.rules.player_action\nrogue.rules.player_action = function(player, action, arg)\n  return base_action(player, action, arg)\nend'),
  d("Rules", "rules.build_level", "rogue.rules.build_level = function(depth, player) … end", "Replace level generation."),
  d("Rules", "rules.new_game", "rogue.rules.new_game = function() … end", "Replace the game start. Return the player id."),
  d("Rules", "rules.hud", "rogue.rules.hud = function() … end", "Return rows for the stats panel: { {label, value}, … }.", 'local base_hud = rogue.rules.hud\nrogue.rules.hud = function()\n  local rows = base_hud()\n  rows[#rows + 1] = { "Gold", tostring(0) }\n  return rows\nend'),
  d("Rules", "rules.xp_needed", "rogue.rules.xp_needed = function(level) … end", "XP required to leave a level."),
];

export const GROUPS = [...new Set(API.map((a) => a.group))];
