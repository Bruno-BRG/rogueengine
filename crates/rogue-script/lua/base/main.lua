-- RogueEngine base rules. Every function here is a replaceable default:
-- a mod can reassign `rogue.rules.*` or `rogue.item_effects.*` to change the game.

local rules = rogue.rules

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
      local dmg = rogue.attack(player, blocker)
      rogue.log("You hit for " .. dmg .. ".")
      return 100
    end
    return false
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

rogue.on("died", function(ev)
  local e = ev.killer and rogue.get(ev.id)
  -- (entity is already despawned when `died` fires; use rogue.log text from the event for UI)
end)

--- Build a fresh level and return the player id. Override for custom level generation,
--- multiple floors, boss rooms, themed dungeons...
function rules.new_game(depth)
  local rooms = rogue.generate_dungeon("floor", "wall")
  local player = rogue.spawn("player", rooms[1].x, rooms[1].y)
  for i = 2, #rooms do
    local c = rooms[i]
    if rogue.chance(0.8) then rogue.spawn("goblin", c.x, c.y) end
    if rogue.chance(0.4) then rogue.spawn("potion_heal", c.x + 1, c.y) end
    if i == 3 then rogue.spawn("iron_sword", c.x - 1, c.y) end
  end
  return player
end
