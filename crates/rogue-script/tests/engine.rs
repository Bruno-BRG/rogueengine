use rogue_script::{Engine, Turn};
use serde_json::json;
use std::path::Path;

fn arena() -> Engine {
    let e = Engine::new(20, 20, 1).unwrap();
    e.exec(r#"for y=0,19 do for x=0,19 do rogue.set_tile(x,y,"floor") end end"#).unwrap();
    e
}

#[test]
fn base_rules_player_fights_goblin_and_picks_up_items() {
    let e = arena();
    e.exec(r#"
        P = rogue.spawn("player", 5, 5)
        G = rogue.spawn("goblin", 6, 5)
        POTION = rogue.spawn("potion_heal", 5, 5)
    "#).unwrap();
    assert_eq!(e.advance().unwrap(), Turn::AwaitingPlayer);
    assert_eq!(e.act("pickup", json!({})).unwrap(), Turn::AwaitingPlayer);
    assert_eq!(e.eval::<i64>("#rogue.inventory(P)").unwrap(), 1);
    // bump-attack until the goblin dies
    for _ in 0..10 {
        if !e.eval::<bool>("rogue.exists(G)").unwrap() { break; }
        e.act("move", json!({"dx": 1, "dy": 0})).unwrap();
    }
    assert!(!e.eval::<bool>("rogue.exists(G)").unwrap(), "goblin should be dead");
    e.exec("rogue.damage(nil, P, 10)").unwrap();
    let before: i64 = e.eval("rogue.get(P).stats.hp").unwrap();
    assert!(e.eval::<bool>("rogue.use_item(P, POTION)").unwrap());
    let max: i64 = e.eval("rogue.get(P).stats.max_hp").unwrap();
    assert_eq!(e.eval::<i64>("rogue.get(P).stats.hp").unwrap(), (before + 12).min(max));
    assert!(!e.eval::<bool>("rogue.exists(POTION)").unwrap(), "potion consumed");
}

#[test]
fn monsters_chase_via_ai() {
    let e = arena();
    e.exec(r#"P = rogue.spawn("player", 2, 2); G = rogue.spawn("goblin", 8, 2)"#).unwrap();
    e.advance().unwrap();
    let d0: i64 = e.eval("rogue.pos(G).x - rogue.pos(P).x").unwrap();
    e.act("wait", json!({})).unwrap();
    e.act("wait", json!({})).unwrap();
    let d1: i64 = e.eval("rogue.pos(G).x - rogue.pos(P).x").unwrap();
    assert!(d1 < d0, "{d1} !< {d0}");
}

#[test]
fn mod_overrides_rules_and_patches_data() {
    let mut e = arena();
    e.load_mods_dir(Path::new(concat!(env!("CARGO_MANIFEST_DIR"), "/../../mods"))).unwrap();
    assert!(e.loaded_mods.iter().any(|m| m.id == "example-poison"));
    // $patch merged hp without losing the rest of the goblin definition
    e.exec(r#"G = rogue.spawn("goblin", 3, 3)"#).unwrap();
    assert_eq!(e.eval::<i64>("rogue.get(G).stats.hp").unwrap(), 12);
    assert_eq!(e.eval::<i64>("rogue.get(G).stats.attack").unwrap(), 3);
    // poison via hooks
    e.exec(r#"P = rogue.spawn("player", 5, 5); S = rogue.spawn("spider", 6, 5)"#).unwrap();
    e.advance().unwrap();
    for _ in 0..3 { e.act("wait", json!({})).unwrap(); }
    assert!(e.eval::<i64>("rogue.get(P).stats.hp").unwrap() < 30);
    assert!(e.eval::<Option<i64>>("rogue.get_data(P, 'poison')").unwrap().is_some());
}

#[test]
fn rules_can_be_overridden_from_lua() {
    let e = arena();
    e.exec(r#"
        rogue.rules.damage = function() return 999 end
        P = rogue.spawn("player", 1, 1); G = rogue.spawn("goblin", 2, 1)
    "#).unwrap();
    e.advance().unwrap();
    e.act("move", json!({"dx": 1, "dy": 0})).unwrap();
    assert!(!e.eval::<bool>("rogue.exists(G)").unwrap());
}

#[test]
fn sandbox_blocks_ambient_authority() {
    let e = arena();
    assert!(e.exec(r#"os.execute("true")"#).is_err());
    assert!(e.exec(r#"io.open("/etc/passwd")"#).is_err());
    assert!(e.eval::<bool>("os.time() > 0").unwrap());
}

#[test]
fn script_errors_in_hooks_do_not_crash_the_game() {
    let e = arena();
    e.exec(r#"rogue.on("spawned", function() error("boom") end); rogue.spawn("goblin", 1, 1)"#).unwrap();
    e.dispatch_events().unwrap();
    assert!(e.world.borrow().log.iter().any(|l| l.contains("boom")));
}

#[test]
fn player_death_ends_the_game_instead_of_looping() {
    let e = arena();
    e.exec(r#"P = rogue.spawn("player", 1, 1); G = rogue.spawn("goblin", 5, 5)"#).unwrap();
    e.advance().unwrap();
    e.exec("rogue.damage(nil, P, 9999)").unwrap();
    assert_eq!(e.advance().unwrap(), rogue_script::Turn::Idle);
    assert_eq!(e.act("wait", json!({})).unwrap(), rogue_script::Turn::Idle);
}

#[test]
fn new_game_descend_and_xp() {
    let e = Engine::new(50, 30, 11).unwrap();
    e.exec("P = rogue.rules.new_game()").unwrap();
    e.advance().unwrap();
    assert!(e.eval::<i64>("#rogue.find_tagged('monster')").unwrap() > 0);
    // standing off the stairs: descending is refused and takes no time
    e.act("descend", json!({})).unwrap();
    assert_eq!(e.eval::<i64>("rogue.world_get('depth')").unwrap(), 1);
    // teleport onto the stairs and descend
    e.exec(r#"
        local w, h = rogue.map_size()
        for y=0,h-1 do for x=0,w-1 do
          if rogue.tile(x,y).id == "stairs_down" then rogue.teleport(P, x, y) end
        end end
    "#).unwrap();
    e.act("descend", json!({})).unwrap();
    assert_eq!(e.eval::<i64>("rogue.world_get('depth')").unwrap(), 2);
    assert!(e.eval::<bool>("rogue.exists(P)").unwrap());
    // XP and level-up by killing something
    e.exec(r#"
        local p = rogue.pos(P)
        K = rogue.spawn("orc", p.x, p.y)  -- same tile is fine for the test
        rogue.damage(P, K, 999)
    "#).unwrap();
    e.dispatch_events().unwrap();
    assert!(e.eval::<i64>("rogue.get_data(P,'level')").unwrap() >= 2, "orc xp (25) should level up");
    let hud = e.eval_json("rogue.rules.hud()").unwrap();
    assert_eq!(hud[0][0], "Depth");
}

#[test]
fn levels_are_deterministic_per_seed() {
    let sig = |seed| {
        let e = Engine::new(50, 30, seed).unwrap();
        e.exec("P = rogue.rules.new_game()").unwrap();
        e.eval::<String>(r#"local t = {} for _, id in ipairs(rogue.find_tagged('monster')) do t[#t+1] = rogue.get(id).kind .. rogue.pos(id).x .. "," .. rogue.pos(id).y end table.sort(t) return table.concat(t, ";")"#).unwrap()
    };
    assert_eq!(sig(3), sig(3));
    assert_ne!(sig(3), sig(4));
}
