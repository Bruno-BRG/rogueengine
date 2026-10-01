use crate::project::Project;
use rogue_assets::SpriteEditor;
use serde_json::json;

/// Starter "dungeon crawler": sprites for every base object plus one custom monster,
/// an example script and an example visual graph, so a new project is playable immediately.
pub fn dungeon(p: &Project) -> Result<(), String> {
    for sprite in crate::art::all() {
        crate::api::save_sprite(p, &SpriteEditor::new(sprite), 4)?;
    }
    // A custom monster defined purely as data. It spawns from depth 2 with no code.
    let entities = json!({
        "slime": {
            "name": "Slime", "glyph": "j", "color": "#62d48f", "sprite": "slime", "blocks": true,
            "stats": { "hp": 10, "max_hp": 10, "attack": 2, "defense": 1, "speed": 80 },
            "tags": ["actor", "monster"],
            "data": { "sight": 6, "xp": 8, "spawn": { "min_depth": 2, "max_depth": 8, "weight": 8 } }
        }
    });
    p.save_objects(entities.as_object().unwrap(), &serde_json::Map::new())?;
    p.write_text("scripts", "rules", ".lua", EXAMPLE_SCRIPT)?;
    let graph = json!({
        "nodes": [
            { "id": 1, "type": "event.died", "props": {}, "x": 40, "y": 60 },
            { "id": 2, "type": "flow.branch", "props": {}, "x": 340, "y": 60 },
            { "id": 3, "type": "logic.chance", "props": { "p": 0.35 }, "x": 90, "y": 220 },
            { "id": 4, "type": "action.spawn", "props": { "def": "potion_heal" }, "x": 640, "y": 40 },
            { "id": 5, "type": "action.log", "props": { "text": "Something glints in the remains..." }, "x": 920, "y": 40 }
        ],
        "edges": [
            { "from": { "node": 1, "pin": "then" }, "to": { "node": 2, "pin": "exec" } },
            { "from": { "node": 3, "pin": "out" }, "to": { "node": 2, "pin": "condition" } },
            { "from": { "node": 2, "pin": "true" }, "to": { "node": 4, "pin": "exec" } },
            { "from": { "node": 1, "pin": "x" }, "to": { "node": 4, "pin": "x" } },
            { "from": { "node": 1, "pin": "y" }, "to": { "node": 4, "pin": "y" } },
            { "from": { "node": 4, "pin": "then" }, "to": { "node": 5, "pin": "exec" } }
        ]
    });
    p.write_text("graphs", "loot_drops", ".graph.json", &serde_json::to_string_pretty(&graph).unwrap())
}

const EXAMPLE_SCRIPT: &str = r#"-- Your game's rules live here. Everything in the base game can be replaced.
-- API reference: Scripts tab > "API" panel.

-- Example 1: react to events. Poison from spiders, XP bonuses, achievements...
rogue.on("item_picked", function(ev)
  rogue.log("You pick up the " .. rogue.get(ev.item).name .. ".")
end)

-- Example 2 (uncomment): change the damage formula.
-- rogue.rules.damage = function(attacker, target)
--   local base = rogue.default_damage(attacker, target)
--   if rogue.chance(0.1) then return base * 2 end  -- 10% critical hit
--   return base
-- end

-- Example 3 (uncomment): add a custom item effect, used by an item with item.on_use = "fireball".
-- rogue.item_effect("fireball", function(who, item)
--   local p = rogue.pos(who)
--   for _, id in ipairs(rogue.in_radius(p.x, p.y, 3)) do
--     if id ~= who and rogue.has_tag(id, "monster") then rogue.damage(who, id, 8) end
--   end
--   rogue.log("Flames roar out!")
-- end)
"#;
