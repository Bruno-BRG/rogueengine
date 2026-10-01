use std::collections::HashSet;

use crate::grid::{Pos, TileMap};

/// Recursive shadowcasting. Returns every visible tile within `radius` (including the origin).
#[allow(clippy::needless_range_loop)] // `oct` indexes four parallel rows of MULT
pub fn compute_fov(map: &TileMap, origin: Pos, radius: i32) -> HashSet<Pos> {
    let mut seen = HashSet::new();
    seen.insert(origin);
    const MULT: [[i32; 8]; 4] = [
        [1, 0, 0, -1, -1, 0, 0, 1],
        [0, 1, -1, 0, 0, -1, 1, 0],
        [0, 1, 1, 0, 0, -1, -1, 0],
        [1, 0, 0, 1, -1, 0, 0, -1],
    ];
    for oct in 0..8 {
        let m = [MULT[0][oct], MULT[1][oct], MULT[2][oct], MULT[3][oct]];
        cast(map, origin, radius, 1, 1.0, 0.0, m, &mut seen);
    }
    seen
}

#[allow(clippy::too_many_arguments)]
fn cast(map: &TileMap, o: Pos, radius: i32, row: i32, mut start: f32, end: f32, m: [i32; 4], seen: &mut HashSet<Pos>) {
    if start < end {
        return;
    }
    let mut new_start = 0.0;
    for i in row..=radius {
        let mut blocked = false;
        let dy = -i;
        for dx in -i..=0 {
            let (l_slope, r_slope) = ((dx as f32 - 0.5) / (dy as f32 + 0.5), (dx as f32 + 0.5) / (dy as f32 - 0.5));
            if start < r_slope {
                continue;
            }
            if end > l_slope {
                break;
            }
            let p = Pos::new(o.x + dx * m[0] + dy * m[1], o.y + dx * m[2] + dy * m[3]);
            if dx * dx + dy * dy <= radius * radius {
                seen.insert(p);
            }
            let opaque = !map.transparent(p);
            if blocked {
                if opaque {
                    new_start = r_slope;
                } else {
                    blocked = false;
                    start = new_start;
                }
            } else if opaque && i < radius {
                blocked = true;
                cast(map, o, radius, i + 1, start, l_slope, m, seen);
                new_start = r_slope;
            }
        }
        if blocked {
            break;
        }
    }
}
