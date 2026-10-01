//! Grid physics: the only "physics" a roguelike needs. Deterministic, tile-based.
use crate::entity::EntityId;
use crate::grid::Pos;
use crate::world::World;

/// Bresenham line, excluding `a`, including `b`.
pub fn line(a: Pos, b: Pos) -> Vec<Pos> {
    let (mut x, mut y) = (a.x, a.y);
    let (dx, dy) = ((b.x - a.x).abs(), -(b.y - a.y).abs());
    let (sx, sy) = (if a.x < b.x { 1 } else { -1 }, if a.y < b.y { 1 } else { -1 });
    let mut err = dx + dy;
    let mut out = Vec::new();
    while (x, y) != (b.x, b.y) {
        let e2 = 2 * err;
        if e2 >= dy {
            err += dy;
            x += sx;
        }
        if e2 <= dx {
            err += dx;
            y += sy;
        }
        out.push(Pos::new(x, y));
    }
    out
}

#[derive(Debug, PartialEq, Eq)]
pub enum Hit {
    Wall(Pos),
    Entity(EntityId, Pos),
    /// Reached the end of range without hitting anything.
    Clear(Pos),
}

impl World {
    /// Trace a projectile from `from` toward `to` up to `range` tiles.
    pub fn raycast(&self, from: Pos, to: Pos, range: usize, ignore: Option<EntityId>) -> Hit {
        let mut last = from;
        for p in line(from, to).into_iter().take(range) {
            if !self.map.walkable(p) {
                return Hit::Wall(p);
            }
            if let Some(b) = self.blocker_at(p).filter(|b| Some(*b) != ignore) {
                return Hit::Entity(b, p);
            }
            last = p;
        }
        Hit::Clear(last)
    }

    /// Push an entity up to `dist` tiles along (dx, dy); stops at walls/blockers.
    /// Returns tiles actually travelled.
    pub fn knockback(&mut self, id: EntityId, dx: i32, dy: i32, dist: i32) -> i32 {
        let (sx, sy) = (dx.signum(), dy.signum());
        let mut moved = 0;
        for _ in 0..dist {
            if self.move_by(id, sx, sy) != crate::world::MoveResult::Moved {
                break;
            }
            moved += 1;
        }
        moved
    }

    /// Entities within Chebyshev `radius` of `center`.
    pub fn in_radius(&self, center: Pos, radius: i32) -> Vec<EntityId> {
        self.ids().filter(|id| {
            let e = self.get(*id).unwrap();
            e.carried_by.is_none() && e.pos.dist(center) <= radius
        }).collect()
    }
}
