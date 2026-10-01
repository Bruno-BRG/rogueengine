use serde::{Deserialize, Serialize};

use crate::entity::{Entity, EntityId};
use crate::event::Event;
use crate::grid::{Pos, TileMap};
use crate::rng::Rng;
use crate::turn::TurnQueue;

#[derive(Clone, Debug, Serialize, Deserialize)]
struct Slot {
    gen: u32,
    entity: Option<Entity>,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum MoveResult {
    Moved,
    Blocked(Option<EntityId>),
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct World {
    pub map: TileMap,
    pub rng: Rng,
    pub turn: TurnQueue,
    slots: Vec<Slot>,
    free: Vec<u32>,
    #[serde(skip)]
    events: Vec<Event>,
    pub log: Vec<String>,
}

impl World {
    pub fn new(width: i32, height: i32, seed: u64) -> Self {
        Self {
            map: TileMap::new(width, height),
            rng: Rng::new(seed),
            turn: TurnQueue::default(),
            slots: Vec::new(),
            free: Vec::new(),
            events: Vec::new(),
            log: Vec::new(),
        }
    }

    pub fn emit(&mut self, e: Event) {
        self.events.push(e);
    }
    /// Hand accumulated events to the UI/script layer.
    pub fn drain_events(&mut self) -> Vec<Event> {
        std::mem::take(&mut self.events)
    }
    pub fn message(&mut self, text: impl Into<String>) {
        let text = text.into();
        self.log.push(text.clone());
        self.emit(Event::Message { text });
    }

    pub fn spawn(&mut self, e: Entity) -> EntityId {
        let is_actor = e.stats.is_some() && e.tags.contains("actor");
        let id = if let Some(index) = self.free.pop() {
            let s = &mut self.slots[index as usize];
            s.entity = Some(e);
            EntityId { index, gen: s.gen }
        } else {
            self.slots.push(Slot { gen: 0, entity: Some(e) });
            EntityId { index: self.slots.len() as u32 - 1, gen: 0 }
        };
        if is_actor {
            self.turn.add(id);
        }
        self.emit(Event::Spawned { id });
        id
    }

    pub fn despawn(&mut self, id: EntityId) -> bool {
        let Some(slot) = self.slots.get_mut(id.index as usize) else { return false };
        if slot.gen != id.gen || slot.entity.is_none() {
            return false;
        }
        slot.entity = None;
        slot.gen += 1;
        self.free.push(id.index);
        self.turn.remove(id);
        self.emit(Event::Despawned { id });
        true
    }

    pub fn get(&self, id: EntityId) -> Option<&Entity> {
        let s = self.slots.get(id.index as usize)?;
        if s.gen == id.gen { s.entity.as_ref() } else { None }
    }
    pub fn get_mut(&mut self, id: EntityId) -> Option<&mut Entity> {
        let s = self.slots.get_mut(id.index as usize)?;
        if s.gen == id.gen { s.entity.as_mut() } else { None }
    }

    pub fn ids(&self) -> impl Iterator<Item = EntityId> + '_ {
        self.slots.iter().enumerate().filter_map(|(i, s)| {
            s.entity.as_ref().map(|_| EntityId { index: i as u32, gen: s.gen })
        })
    }

    /// Entities standing on the map at `p` (carried items are excluded).
    pub fn at(&self, p: Pos) -> Vec<EntityId> {
        self.ids().filter(|id| {
            let e = self.get(*id).unwrap();
            e.carried_by.is_none() && e.pos == p
        }).collect()
    }
    pub fn blocker_at(&self, p: Pos) -> Option<EntityId> {
        self.at(p).into_iter().find(|id| self.get(*id).is_some_and(|e| e.blocks))
    }
    pub fn find_tagged<'a>(&'a self, tag: &'a str) -> impl Iterator<Item = EntityId> + 'a {
        self.ids().filter(move |id| self.get(*id).is_some_and(|e| e.tags.contains(tag)))
    }

    pub fn move_by(&mut self, id: EntityId, dx: i32, dy: i32) -> MoveResult {
        let Some(from) = self.get(id).map(|e| e.pos) else { return MoveResult::Blocked(None) };
        let to = from.offset(dx, dy);
        if !self.map.walkable(to) {
            self.emit(Event::Blocked { id, by: None, at: to });
            return MoveResult::Blocked(None);
        }
        if let Some(b) = self.blocker_at(to).filter(|b| *b != id) {
            self.emit(Event::Blocked { id, by: Some(b), at: to });
            return MoveResult::Blocked(Some(b));
        }
        self.get_mut(id).unwrap().pos = to;
        self.emit(Event::Moved { id, from, to });
        MoveResult::Moved
    }

    /// Default combat formula; scripts may override by computing their own
    /// damage and calling [`World::damage`] directly.
    pub fn default_damage(&mut self, attacker: EntityId, target: EntityId) -> i32 {
        let a = self.stat(attacker, "attack");
        let d = self.stat(target, "defense");
        (a - d + self.rng.range(-1, 1)).max(1)
    }

    pub fn attack(&mut self, attacker: EntityId, target: EntityId) -> i32 {
        let dmg = self.default_damage(attacker, target);
        self.damage(Some(attacker), target, dmg);
        dmg
    }

    /// Apply damage; emits `Attacked`, and `Died` (+ despawn) at 0 HP.
    pub fn damage(&mut self, source: Option<EntityId>, target: EntityId, amount: i32) {
        let Some(stats) = self.get_mut(target).and_then(|e| e.stats.as_mut()) else { return };
        stats.hp -= amount;
        let dead = stats.hp <= 0;
        if let Some(s) = source {
            self.emit(Event::Attacked { attacker: s, target, damage: amount });
        }
        if dead {
            self.emit(Event::Died { id: target, killer: source });
            self.despawn(target);
        }
    }

    pub fn heal(&mut self, target: EntityId, amount: i32) {
        if let Some(s) = self.get_mut(target).and_then(|e| e.stats.as_mut()) {
            s.hp = (s.hp + amount).min(s.max_hp);
        }
    }
}

#[cfg(test)]
pub(crate) mod tests {
    use super::*;
    use crate::entity::Stats;
    use crate::grid::TileDef;

    pub(crate) fn test_world() -> World {
        let mut w = World::new(20, 20, 7);
        for (id, walk) in [("floor", true), ("wall", false)] {
            w.map.register(TileDef { id: id.into(), name: id.into(), walkable: walk, transparent: walk, glyph: None, sprite: None, tags: vec![] });
        }
        w.map.fill("floor");
        w
    }

    fn actor(pos: Pos, hp: i32, speed: i32) -> Entity {
        Entity { name: "a".into(), pos, blocks: true, stats: Some(Stats { hp, max_hp: hp, speed, ..Default::default() }), tags: ["actor".to_string()].into(), ..Default::default() }
    }

    #[test]
    fn stale_ids_do_not_alias() {
        let mut w = test_world();
        let a = w.spawn(actor(Pos::new(1, 1), 5, 100));
        w.despawn(a);
        let b = w.spawn(actor(Pos::new(2, 2), 5, 100));
        assert_eq!(a.index, b.index);
        assert!(w.get(a).is_none() && w.get(b).is_some());
    }

    #[test]
    fn collision_and_combat() {
        let mut w = test_world();
        let a = w.spawn(actor(Pos::new(1, 1), 50, 100));
        let b = w.spawn(actor(Pos::new(2, 1), 1, 100));
        assert_eq!(w.move_by(a, 1, 0), MoveResult::Blocked(Some(b)));
        w.attack(a, b);
        assert!(w.get(b).is_none());
        assert_eq!(w.move_by(a, 1, 0), MoveResult::Moved);
        assert!(w.drain_events().iter().any(|e| matches!(e, Event::Died { .. })));
    }

    #[test]
    fn faster_actors_act_more() {
        let mut w = test_world();
        let fast = w.spawn(actor(Pos::new(1, 1), 5, 200));
        let slow = w.spawn(actor(Pos::new(3, 3), 5, 100));
        let (mut f, mut s) = (0, 0);
        for _ in 0..30 {
            let id = w.next_actor().unwrap();
            w.spend(id, crate::turn::ACTION_COST);
            if id == fast { f += 1 } else if id == slow { s += 1 }
        }
        assert!(f > s, "fast {f} slow {s}");
    }

    #[test]
    fn inventory_and_equipment() {
        use crate::entity::Inventory;
        use crate::item::ItemInfo;
        let mut w = test_world();
        let mut hero = actor(Pos::new(5, 5), 10, 100);
        hero.inventory = Some(Inventory { capacity: 1, ..Default::default() });
        let h = w.spawn(hero);
        let mk = |slot| Entity { pos: Pos::new(5, 5), item: Some(ItemInfo { slot: Some(slot), modifiers: serde_json::from_str(r#"{"attack":3}"#).unwrap(), ..Default::default() }), ..Default::default() };
        let sword = w.spawn(mk("weapon".into()));
        let extra = w.spawn(mk("head".into()));
        w.pickup(h, sword).unwrap();
        assert_eq!(w.pickup(h, extra), Err(crate::item::ItemError::Full));
        w.equip(h, sword).unwrap();
        assert_eq!(w.stat(h, "attack"), 4);
        w.drop_item(h, sword).unwrap();
        assert_eq!(w.stat(h, "attack"), 1);
    }

    #[test]
    fn fov_pathfinding_physics() {
        let mut w = test_world();
        for y in 0..20 {
            w.map.set(Pos::new(10, y), "wall");
        }
        w.map.set(Pos::new(10, 10), "floor");
        let vis = crate::fov::compute_fov(&w.map, Pos::new(5, 5), 8);
        assert!(vis.contains(&Pos::new(9, 5)) && !vis.contains(&Pos::new(12, 5)));
        let path = crate::path::find_path(&w, Pos::new(5, 5), Pos::new(15, 5), 5000).unwrap();
        assert!(path.contains(&Pos::new(10, 10)) && *path.last().unwrap() == Pos::new(15, 5));
        assert!(matches!(w.raycast(Pos::new(5, 5), Pos::new(15, 5), 20, None), crate::physics::Hit::Wall(_)));
    }

    #[test]
    fn procgen_is_connected_and_deterministic() {
        let gen = |seed| {
            let mut w = test_world();
            let mut rng = Rng::new(seed);
            let rooms = crate::procgen::rooms_and_corridors(&mut w.map, &mut rng, "floor", "wall", 60);
            (w, rooms)
        };
        let (w, rooms) = gen(42);
        assert!(rooms.len() >= 2);
        let p = crate::path::find_path(&w, rooms[0].center(), rooms.last().unwrap().center(), 20000);
        assert!(p.is_some());
        assert_eq!(gen(42).1, rooms);
    }
}

#[cfg(test)]
mod save_tests {
    #[test]
    fn world_roundtrips_through_json() {
        let mut w = super::tests::test_world();
        w.spawn(crate::Entity { tags: ["actor".to_string()].into(), stats: Some(Default::default()), ..Default::default() });
        let json = serde_json::to_string(&w).unwrap();
        let mut back: super::World = serde_json::from_str(&json).unwrap();
        back.map.rebuild_index();
        assert_eq!(back.ids().count(), 1);
        assert!(back.map.tile_id("floor").is_some());
    }
}
