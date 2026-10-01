-- RogueEngine base rules. Every function here is a replaceable default:
-- a mod can reassign `rogue.rules.*` or `rogue.item_effects.*` to change the game.

local rules = rogue.rules

local function has_tile_tag(x, y, tag)
  for _, t in ipairs(rogue.tile(x, y).tags or {}) do
    if t == tag then return true end
  end
  return false
end

local function sorted_ids(t)
  local ids = {}
  for id in pairs(t) do ids[#ids + 1] = id end
  table.sort(ids) -- Lua's pairs() order is not stable across runs; sort for deterministic levels
  return ids
end

--- Weighted pick among definitions whose data.spawn allows this depth.
--- kind: "monster" (default) or "item". Add a monster = add data, no code.
local function pick_spawn(depth, kind)
  local defs, choices, total = rogue.definitions(), {}, 0
  for _, id in ipairs(sorted_ids(defs)) do
    local sp = defs[id].data and defs[id].data.spawn
    if sp and (sp.kind or "monster") == kind and depth >= (sp.min_depth or 1) and depth <= (sp.max_depth or 999) then
      local w = sp.weight or 1
      choices[#choices + 1] = { id = id, w = w }
      total = total + w
    end
  end
  if total == 0 then return nil end
  local r = rogue.random(1, total)
  for _, c in ipairs(choices) do
    r = r - c.w
    if r <= 0 then return c.id end
  end
end

--- Damage formula. Override for armor types, crits, elemental resists...
function rules.damage(attacker, target)
  return rogue.default_damage(attacker, target)
end

--- Monster AI: attack the player when adjacent, chase when in sight, else idle.
--- Return the energy cost of the action (default 100).
function rules.ai(id)
  local me = rogue.pos(id)
  local players = rogue.find_tagged("player")
  local player = players[1]
  if not player or not me then return 100 end
  local pp = rogue.pos(player)
  local sight = rogue.get_data(id, "sight") or 8
  local dist = math.max(math.abs(pp.x - me.x), math.abs(pp.y - me.y))
  if dist <= 1 then
    rogue.attack(id, player)
  elseif dist <= sight then
    local path = rogue.path(me.x, me.y, pp.x, pp.y)
    if path and path[1] then
      rogue.move(id, path[1].x - me.x, path[1].y - me.y)
    end
  end
  return 100
end

--- Player commands. `action` is one of "move", "wait", "pickup", "use", "equip", "drop".
--- Return cost, or false when the command was invalid (no turn consumed).
function rules.player_action(player, action, arg)
  if action == "wait" then
    return 100
  elseif action == "move" then
    local ok, blocker = rogue.move(player, arg.dx, arg.dy)
    if ok then return 100 end
    if blocker and rogue.has_tag(blocker, "monster") then
      rogue.attack(player, blocker)
      return 100
    end
    return false
  elseif action == "descend" then
    local p = rogue.pos(player)
    if not has_tile_tag(p.x, p.y, "stairs_down") then
      rogue.log("There are no stairs here.")
      return false
    end
    rules.descend(player)
    return 100
  elseif action == "pickup" then
    local p = rogue.pos(player)
    for _, e in ipairs(rogue.at(p.x, p.y)) do
      if e ~= player and rogue.pickup(player, e) then
        rogue.log("Picked up " .. rogue.get(e).name .. ".")
        return 100
      end
    end
    return false
  elseif action == "use" then
    return rogue.use_item(player, arg.item) and 100 or false
  elseif action == "equip" then
    return rogue.equip(player, arg.item) and 100 or false
  elseif action == "drop" then
    return rogue.drop(player, arg.item) and 100 or false
  end
  return false
end

rogue.item_effect("heal", function(who, item)
  local amount = rogue.get_data(item, "heal_amount") or 5
  rogue.heal(who, amount)
  rogue.log("You feel better (+" .. amount .. ").")
end)


--- Build floor `depth`; the player (if given) is kept and placed in the first room.
function rules.build_level(depth, player)
  rogue.clear_level(player and { player } or {})
  local rooms = rogue.generate_dungeon("floor", "wall")
  if player then rogue.teleport(player, rooms[1].x, rooms[1].y) end
  local last = rooms[#rooms]
  rogue.set_tile(last.x, last.y, "stairs_down")
  for i = 2, #rooms do
    local c = rooms[i]
    for _ = 1, rogue.random(0, 1 + depth // 3) do
      local id = pick_spawn(depth, "monster")
      if id then rogue.spawn(id, c.x + rogue.random(-1, 1), c.y + rogue.random(-1, 1)) end
    end
    if rogue.chance(0.45) then
      local id = pick_spawn(depth, "item")
      if id then rogue.spawn(id, c.x + 1, c.y + 1) end
    end
  end
end

--- Start a new game; returns the player id. Override for custom setups.
function rules.new_game()
  local player = rogue.spawn("player", 0, 0)
  rogue.world_set("depth", 1)
  rules.build_level(1, player)
  rogue.log("You enter the dungeon. Find the stairs (>) to descend.")
  return player
end

function rules.descend(player)
  local depth = (rogue.world_get("depth") or 1) + 1
  rogue.world_set("depth", depth)
  rules.build_level(depth, player)
  rogue.heal(player, math.floor(rogue.stat(player, "max_hp") / 4))
  rogue.log("You descend to depth " .. depth .. ".")
end

--- XP needed to leave `level`.
function rules.xp_needed(level) return level * 15 end

function rules.level_up(player)
  local level = (rogue.get_data(player, "level") or 1) + 1
  rogue.set_data(player, "level", level)
  rogue.set_stat(player, "max_hp", rogue.stat(player, "max_hp") + 5)
  rogue.heal(player, 5)
  if level % 2 == 0 then rogue.set_stat(player, "attack", rogue.stat(player, "attack") + 1) end
  rogue.log("Welcome to level " .. level .. "!")
end

--- Status panel rows: return { {label, value}, ... }. Mods can append rows.
function rules.hud()
  local p = rogue.find_tagged("player")[1]
  if not p then return {} end
  local level = rogue.get_data(p, "level") or 1
  return {
    { "Depth", tostring(rogue.world_get("depth") or 1) },
    { "Level", tostring(level) },
    { "XP", (rogue.get_data(p, "xp") or 0) .. "/" .. rules.xp_needed(level) },
    { "Attack", tostring(rogue.stat(p, "attack")) },
    { "Defense", tostring(rogue.stat(p, "defense")) },
  }
end

rogue.on("attacked", function(ev)
  local dmg = ev.damage
  if ev.attacker_name == "You" then
    rogue.log("You hit the " .. ev.target_name .. " for " .. dmg .. ".")
  else
    rogue.log("The " .. ev.attacker_name .. " hits " .. (ev.target_name == "You" and "you" or ev.target_name) .. " for " .. dmg .. ".")
  end
end)

rogue.on("died", function(ev)
  if ev.name == "You" then
    rogue.log("You die on depth " .. (rogue.world_get("depth") or 1) .. "...")
    return
  end
  rogue.log("The " .. ev.name .. " dies.")
  if ev.killer and rogue.exists(ev.killer) and rogue.has_tag(ev.killer, "player") then
    local def = rogue.definitions()[ev.kind]
    local gain = def and def.data and def.data.xp or 5
    local xp = (rogue.get_data(ev.killer, "xp") or 0) + gain
    local level = rogue.get_data(ev.killer, "level") or 1
    while xp >= rules.xp_needed(level) do
      xp = xp - rules.xp_needed(level)
      rules.level_up(ev.killer)
      level = rogue.get_data(ev.killer, "level")
    end
    rogue.set_data(ev.killer, "xp", xp)
  end
end)
