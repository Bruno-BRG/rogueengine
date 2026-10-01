use crate::grid::{Pos, TileMap};
use crate::rng::Rng;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct Room {
    pub x: i32,
    pub y: i32,
    pub w: i32,
    pub h: i32,
}

impl Room {
    pub fn center(&self) -> Pos {
        Pos::new(self.x + self.w / 2, self.y + self.h / 2)
    }
    fn overlaps(&self, o: &Room) -> bool {
        self.x <= o.x + o.w && o.x <= self.x + self.w && self.y <= o.y + o.h && o.y <= self.y + self.h
    }
}

/// Rooms-and-corridors dungeon. Tiles `floor` / `wall` must already be registered.
/// Returns the rooms (first room is the natural spawn point).
pub fn rooms_and_corridors(map: &mut TileMap, rng: &mut Rng, floor: &str, wall: &str, attempts: usize) -> Vec<Room> {
    map.fill(wall);
    let mut rooms: Vec<Room> = Vec::new();
    for _ in 0..attempts {
        let (w, h) = (rng.range(4, 10), rng.range(4, 8));
        if map.width - w - 2 < 1 || map.height - h - 2 < 1 {
            continue;
        }
        let r = Room { x: rng.range(1, map.width - w - 2), y: rng.range(1, map.height - h - 2), w, h };
        if rooms.iter().any(|o| r.overlaps(o)) {
            continue;
        }
        for y in r.y..r.y + r.h {
            for x in r.x..r.x + r.w {
                map.set(Pos::new(x, y), floor);
            }
        }
        if let Some(prev) = rooms.last() {
            let (a, b) = (prev.center(), r.center());
            let horizontal_first = rng.chance(0.5);
            let (mid_x, mid_y) = if horizontal_first { (b.x, a.y) } else { (a.x, b.y) };
            carve(map, a, Pos::new(mid_x, mid_y), floor);
            carve(map, Pos::new(mid_x, mid_y), b, floor);
        }
        rooms.push(r);
    }
    rooms
}

fn carve(map: &mut TileMap, a: Pos, b: Pos, floor: &str) {
    for x in a.x.min(b.x)..=a.x.max(b.x) {
        map.set(Pos::new(x, a.y), floor);
    }
    for y in a.y.min(b.y)..=a.y.max(b.y) {
        map.set(Pos::new(b.x, y), floor);
    }
}

/// Cellular-automata caves. `fill` is the initial wall probability (~0.45).
pub fn cave(map: &mut TileMap, rng: &mut Rng, floor: &str, wall: &str, fill: f64, steps: usize) {
    for y in 0..map.height {
        for x in 0..map.width {
            let edge = x == 0 || y == 0 || x == map.width - 1 || y == map.height - 1;
            map.set(Pos::new(x, y), if edge || rng.chance(fill) { wall } else { floor });
        }
    }
    for _ in 0..steps {
        let snapshot: Vec<bool> = (0..map.width * map.height).map(|i| map.walkable(Pos::new(i % map.width, i / map.width))).collect();
        for y in 1..map.height - 1 {
            for x in 1..map.width - 1 {
                let walls = (-1..=1).flat_map(|dy| (-1..=1).map(move |dx| (dx, dy))).filter(|(dx, dy)| !snapshot[((y + dy) * map.width + x + dx) as usize]).count();
                map.set(Pos::new(x, y), if walls >= 5 { wall } else { floor });
            }
        }
    }
}
