use std::cmp::Reverse;
use std::collections::{BinaryHeap, HashMap};

use crate::grid::{Pos, DIRS8};
use crate::world::World;

/// A* over the walkable grid (8-way). Tiles occupied by blockers are impassable
/// except the goal itself, so the path can end "at" a target to attack it.
pub fn find_path(world: &World, from: Pos, to: Pos, max_nodes: usize) -> Option<Vec<Pos>> {
    if from == to {
        return Some(vec![]);
    }
    let h = |p: Pos| p.dist(to) as u32;
    let mut open = BinaryHeap::new();
    let mut g: HashMap<Pos, u32> = HashMap::new();
    let mut came: HashMap<Pos, Pos> = HashMap::new();
    g.insert(from, 0);
    open.push(Reverse((h(from), 0u32, from.x, from.y)));
    let mut expanded = 0;
    while let Some(Reverse((_, cost, x, y))) = open.pop() {
        let cur = Pos::new(x, y);
        if cur == to {
            let mut path = vec![cur];
            let mut c = cur;
            while let Some(&p) = came.get(&c) {
                if p == from {
                    break;
                }
                path.push(p);
                c = p;
            }
            path.reverse();
            return Some(path);
        }
        if cost > g[&cur] {
            continue;
        }
        expanded += 1;
        if expanded > max_nodes {
            return None;
        }
        for (dx, dy) in DIRS8 {
            let n = cur.offset(dx, dy);
            if !world.map.walkable(n) || (n != to && world.blocker_at(n).is_some()) {
                continue;
            }
            let nc = cost + 1;
            if g.get(&n).is_none_or(|&old| nc < old) {
                g.insert(n, nc);
                came.insert(n, cur);
                open.push(Reverse((nc + h(n), nc, n.x, n.y)));
            }
        }
    }
    None
}
