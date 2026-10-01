//! The `rogue.*` Lua API. Rules: no Lua is ever called while the world is borrowed,
//! and every function takes/returns plain Lua values (ids are integers).
use mlua::{Lua, LuaSerdeExt, Table, Value as LuaValue};
use rogue_core::grid::Pos;
use rogue_core::physics::Hit;
use rogue_core::world::MoveResult;
use rogue_core::{EntityId, World};
use std::cell::RefCell;
use std::rc::Rc;

use crate::registry::{self, Registry};

type W = Rc<RefCell<World>>;
type R = Rc<RefCell<Registry>>;

fn rt(msg: impl Into<String>) -> mlua::Error {
    mlua::Error::runtime(msg.into())
}
fn id(v: u64) -> EntityId {
    EntityId::from_u64(v)
}
fn nopt() -> mlua::serde::SerializeOptions {
    mlua::serde::SerializeOptions::new().serialize_none_to_null(false)
}

pub fn install(lua: &Lua, world: W, registry: R) -> mlua::Result<()> {
    let rogue = lua.create_table()?;
    rogue.set("rules", lua.create_table()?)?;
    rogue.set("hooks", lua.create_table()?)?;
    rogue.set("item_effects", lua.create_table()?)?;

    macro_rules! f {
        ($name:literal, $world:ident, |$l:tt, $a:tt : $t:ty| $body:expr) => {{
            let $world = world.clone();
            rogue.set($name, lua.create_function(move |$l: &Lua, $a: $t| $body)?)?;
        }};
    }

    // ---- spawning / lifecycle
    {
        let (w, r) = (world.clone(), registry.clone());
        rogue.set("spawn", lua.create_function(move |_, (def, x, y): (String, i32, i32)| {
            let mut e = r.borrow().instantiate(&def).map_err(|e| rt(e.to_string()))?;
            e.pos = Pos::new(x, y);
            Ok(w.borrow_mut().spawn(e).to_u64())
        })?)?;
    }
    f!("despawn", w, |_, i: u64| Ok(w.borrow_mut().despawn(id(i))));
    f!("exists", w, |_, i: u64| Ok(w.borrow().get(id(i)).is_some()));
    f!("get", w, |lua, i: u64| match w.borrow().get(id(i)) {
        Some(e) => lua.to_value_with(e, nopt()),
        None => Ok(LuaValue::Nil),
    });
    f!("find_tagged", w, |_, tag: String| Ok(w.borrow().find_tagged(&tag).map(|i| i.to_u64()).collect::<Vec<_>>()));

    // ---- free-form data & tags (the extension surface for mods)
    f!("get_data", w, |lua, (i, key): (u64, String)| match w.borrow().get(id(i)).and_then(|e| e.data.get(&key)) {
        Some(v) => lua.to_value_with(v, nopt()),
        None => Ok(LuaValue::Nil),
    });
    f!("set_data", w, |lua, (i, key, v): (u64, String, LuaValue)| {
        let json: serde_json::Value = lua.from_value(v)?;
        let mut w = w.borrow_mut();
        let e = w.get_mut(id(i)).ok_or_else(|| rt("set_data: dead entity"))?;
        if json.is_null() { e.data.remove(&key); } else { e.data.insert(key, json); }
        Ok(())
    });
    f!("has_tag", w, |_, (i, t): (u64, String)| Ok(w.borrow().get(id(i)).is_some_and(|e| e.tags.contains(&t))));
    f!("add_tag", w, |_, (i, t): (u64, String)| {
        if let Some(e) = w.borrow_mut().get_mut(id(i)) { e.tags.insert(t); }
        Ok(())
    });
    f!("remove_tag", w, |_, (i, t): (u64, String)| {
        if let Some(e) = w.borrow_mut().get_mut(id(i)) { e.tags.remove(&t); }
        Ok(())
    });

    // ---- position, movement
    f!("pos", w, |lua, i: u64| match w.borrow().get(id(i)) {
        Some(e) => {
            let t = lua.create_table()?;
            t.set("x", e.pos.x)?;
            t.set("y", e.pos.y)?;
            Ok(LuaValue::Table(t))
        }
        None => Ok(LuaValue::Nil),
    });
    f!("move", w, |_, (i, dx, dy): (u64, i32, i32)| {
        // returns true if moved, else false plus the blocking entity id (if any)
        Ok(match w.borrow_mut().move_by(id(i), dx, dy) {
            MoveResult::Moved => (true, None),
            MoveResult::Blocked(b) => (false, b.map(|b| b.to_u64())),
        })
    });
    f!("teleport", w, |_, (i, x, y): (u64, i32, i32)| {
        if let Some(e) = w.borrow_mut().get_mut(id(i)) { e.pos = Pos::new(x, y); }
        Ok(())
    });
    f!("at", w, |_, (x, y): (i32, i32)| Ok(w.borrow().at(Pos::new(x, y)).into_iter().map(|i| i.to_u64()).collect::<Vec<_>>()));
    f!("in_radius", w, |_, (x, y, r): (i32, i32, i32)| Ok(w.borrow().in_radius(Pos::new(x, y), r).into_iter().map(|i| i.to_u64()).collect::<Vec<_>>()));
    f!("knockback", w, |_, (i, dx, dy, d): (u64, i32, i32, i32)| Ok(w.borrow_mut().knockback(id(i), dx, dy, d)));
    f!("raycast", w, |lua, (x0, y0, x1, y1, range): (i32, i32, i32, i32, usize)| {
        let hit = w.borrow().raycast(Pos::new(x0, y0), Pos::new(x1, y1), range, None);
        let t = lua.create_table()?;
        let (kind, p, ent) = match hit {
            Hit::Wall(p) => ("wall", p, None),
            Hit::Entity(e, p) => ("entity", p, Some(e.to_u64())),
            Hit::Clear(p) => ("clear", p, None),
        };
        t.set("kind", kind)?;
        t.set("x", p.x)?;
        t.set("y", p.y)?;
        t.set("entity", ent)?;
        Ok(t)
    });

    // ---- stats / combat primitives (formulas live in Lua: rogue.rules.damage)
    f!("stat", w, |_, (i, n): (u64, String)| Ok(w.borrow().stat(id(i), &n)));
    f!("set_stat", w, |_, (i, n, v): (u64, String, i32)| {
        if let Some(s) = w.borrow_mut().get_mut(id(i)).and_then(|e| e.stats.as_mut()) {
            match n.as_str() {
                "hp" => s.hp = v, "max_hp" => s.max_hp = v, "attack" => s.attack = v,
                "defense" => s.defense = v, "speed" => s.speed = v, _ => {}
            }
        }
        Ok(())
    });
    f!("damage", w, |_, (src, tgt, n): (Option<u64>, u64, i32)| { w.borrow_mut().damage(src.map(id), id(tgt), n); Ok(()) });
    f!("heal", w, |_, (tgt, n): (u64, i32)| { w.borrow_mut().heal(id(tgt), n); Ok(()) });
    f!("default_damage", w, |_, (a, t): (u64, u64)| Ok(w.borrow_mut().default_damage(id(a), id(t))));

    // ---- inventory
    f!("pickup", w, |_, (who, item): (u64, u64)| Ok(w.borrow_mut().pickup(id(who), id(item)).is_ok()));
    f!("drop", w, |_, (who, item): (u64, u64)| Ok(w.borrow_mut().drop_item(id(who), id(item)).is_ok()));
    f!("equip", w, |_, (who, item): (u64, u64)| Ok(w.borrow_mut().equip(id(who), id(item)).is_ok()));
    f!("unequip", w, |_, (who, item): (u64, u64)| Ok(w.borrow_mut().unequip_item(id(who), id(item)).is_ok()));
    f!("inventory", w, |_, who: u64| Ok(w.borrow().get(id(who)).and_then(|e| e.inventory.as_ref()).map(|i| i.items.iter().map(|x| x.to_u64()).collect::<Vec<_>>()).unwrap_or_default()));
    f!("consume", w, |_, item: u64| {
        // remove a carried item from the world (and its owner's inventory)
        let mut w = w.borrow_mut();
        let owner = w.get(id(item)).and_then(|e| e.carried_by);
        if let Some(o) = owner {
            if let Some(inv) = w.get_mut(o).and_then(|e| e.inventory.as_mut()) {
                inv.items.retain(|i| *i != id(item));
                inv.equipped.retain(|_, v| *v != id(item));
            }
        }
        Ok(w.despawn(id(item)))
    });

    // ---- map
    f!("tile", w, |lua, (x, y): (i32, i32)| lua.to_value_with(w.borrow().map.get(Pos::new(x, y)), nopt()));
    f!("set_tile", w, |_, (x, y, t): (i32, i32, String)| Ok(w.borrow_mut().map.set(Pos::new(x, y), &t)));
    f!("walkable", w, |_, (x, y): (i32, i32)| Ok(w.borrow().map.walkable(Pos::new(x, y))));
    f!("map_size", w, |_, _a: ()| { let w = w.borrow(); Ok((w.map.width, w.map.height)) });
    f!("fov", w, |lua, (x, y, r): (i32, i32, i32)| {
        let set = rogue_core::fov::compute_fov(&w.borrow().map, Pos::new(x, y), r);
        let v: Vec<Pos> = set.into_iter().collect();
        lua.to_value(&v)
    });
    f!("path", w, |lua, (x0, y0, x1, y1): (i32, i32, i32, i32)| {
        match rogue_core::path::find_path(&w.borrow(), Pos::new(x0, y0), Pos::new(x1, y1), 4000) {
            Some(p) => lua.to_value(&p),
            None => Ok(LuaValue::Nil),
        }
    });
    f!("generate_dungeon", w, |lua, (floor, wall): (String, String)| {
        let mut w = w.borrow_mut();
        let mut rng = w.rng.clone();
        let rooms = rogue_core::procgen::rooms_and_corridors(&mut w.map, &mut rng, &floor, &wall, 80);
        w.rng = rng;
        let centers: Vec<Pos> = rooms.iter().map(|r| r.center()).collect();
        lua.to_value(&centers)
    });
    f!("generate_cave", w, |_, (floor, wall): (String, String)| {
        let mut w = w.borrow_mut();
        let mut rng = w.rng.clone();
        rogue_core::procgen::cave(&mut w.map, &mut rng, &floor, &wall, 0.45, 4);
        w.rng = rng;
        Ok(())
    });

    // ---- misc
    f!("log", w, |_, text: String| { w.borrow_mut().message(text); Ok(()) });
    f!("random", w, |_, (lo, hi): (i32, i32)| Ok(w.borrow_mut().rng.range(lo, hi)));
    f!("chance", w, |_, p: f64| Ok(w.borrow_mut().rng.chance(p)));

    // ---- data definitions from Lua
    {
        let (w, r) = (world.clone(), registry.clone());
        rogue.set("register_tile", lua.create_function(move |lua, (tid, def): (String, LuaValue)| {
            let json: serde_json::Value = lua.from_value(def)?;
            registry::register_tile(&mut w.borrow_mut(), &tid, json).map_err(|e| rt(e.to_string()))
        })?)?;
        rogue.set("register_entity", lua.create_function(move |lua, (eid, def): (String, LuaValue)| {
            let json: serde_json::Value = lua.from_value(def)?;
            r.borrow_mut().define_entity(&eid, json);
            Ok(())
        })?)?;
    }

    lua.globals().set("rogue", rogue)?;
    // Pure-Lua parts of the API (hooks, rules, item use) are installed by the base mod prelude.
    lua.load(include_str!("../lua/prelude.lua")).set_name("@rogue/prelude.lua").exec()?;
    let _: Table = lua.globals().get("rogue")?;
    Ok(())
}
