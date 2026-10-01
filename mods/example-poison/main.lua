-- Adds a poison status effect without touching engine code.
rogue.on("attacked", function(ev)
  if rogue.has_tag(ev.attacker, "venomous") and rogue.exists(ev.target) then
    rogue.set_data(ev.target, "poison", 3)
    rogue.log("You are poisoned!")
  end
end)

rogue.on("turn_started", function(ev)
  if not rogue.exists(ev.id) then return end
  local p = rogue.get_data(ev.id, "poison")
  if p and p > 0 then
    rogue.damage(nil, ev.id, 1)
    if rogue.exists(ev.id) then rogue.set_data(ev.id, "poison", p - 1) end
  end
end)
