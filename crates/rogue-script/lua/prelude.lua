-- Pure-Lua half of the `rogue` API. Loaded before any mod.

rogue.hooks = {}

--- Subscribe to an engine event (e.g. "died", "moved", "attacked", "item_picked").
--- Returns a handle for rogue.off. Later subscribers run after earlier ones.
function rogue.on(event, fn)
  local list = rogue.hooks[event]
  if not list then list = {}; rogue.hooks[event] = list end
  list[#list + 1] = fn
  return fn
end

function rogue.off(event, fn)
  local list = rogue.hooks[event] or {}
  for i = #list, 1, -1 do
    if list[i] == fn then table.remove(list, i) end
  end
end

-- Called by the engine for each event.
function rogue._dispatch(name, ev)
  for _, fn in ipairs(rogue.hooks[name] or {}) do
    local ok, err = pcall(fn, ev)
    if not ok then rogue.log("[script error] " .. name .. ": " .. tostring(err)) end
  end
end

--- Define a custom item effect referenced by an item's `item.on_use`.
function rogue.item_effect(name, fn) rogue.item_effects[name] = fn end

--- Use a carried item. Returns true if the item had an effect.
function rogue.use_item(who, item)
  local data = rogue.get(item)
  local name = data and data.item and data.item.on_use
  local fn = name and rogue.item_effects[name]
  if not fn then return false end
  local consumed = fn(who, item)
  if consumed ~= false then rogue.consume(item) end
  return true
end

--- Melee attack through the overridable damage rule.
function rogue.attack(attacker, target)
  local dmg = rogue.rules.damage(attacker, target)
  rogue.damage(attacker, target, dmg)
  return dmg
end
