use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, Hash, Serialize, Deserialize)]
pub struct Pos {
    pub x: i32,
    pub y: i32,
}

impl Pos {
    pub const fn new(x: i32, y: i32) -> Self {
        Self { x, y }
    }
    pub fn offset(self, dx: i32, dy: i32) -> Self {
        Self::new(self.x + dx, self.y + dy)
    }
    /// Chebyshev distance (8-way movement).
    pub fn dist(self, o: Pos) -> i32 {
        (self.x - o.x).abs().max((self.y - o.y).abs())
    }
}

pub const DIRS4: [(i32, i32); 4] = [(0, -1), (1, 0), (0, 1), (-1, 0)];
pub const DIRS8: [(i32, i32); 8] =
    [(0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1)];

/// Tile definition; modders add or replace these by `id` in JSON or Lua.
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct TileDef {
    pub id: String,
    #[serde(default)]
    pub name: String,
    #[serde(default = "yes")]
    pub walkable: bool,
    #[serde(default = "yes")]
    pub transparent: bool,
    #[serde(default)]
    pub glyph: Option<char>,
    #[serde(default)]
    pub sprite: Option<String>,
    #[serde(default)]
    pub color: Option<String>,
    #[serde(default)]
    pub tags: Vec<String>,
}
fn yes() -> bool {
    true
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct TileMap {
    pub width: i32,
    pub height: i32,
    tiles: Vec<u16>,
    defs: Vec<TileDef>,
    #[serde(skip)]
    index: HashMap<String, u16>,
}

impl TileMap {
    /// Index 0 is always the "void" tile: solid and opaque.
    pub fn new(width: i32, height: i32) -> Self {
        let mut m = Self {
            width,
            height,
            tiles: vec![0; (width * height).max(0) as usize],
            defs: Vec::new(),
            index: HashMap::new(),
        };
        m.register(TileDef {
            id: "void".into(),
            name: "Void".into(),
            walkable: false,
            transparent: false,
            glyph: Some(' '),
            sprite: None,
            color: None,
            tags: vec![],
        });
        m
    }
    pub fn rebuild_index(&mut self) {
        self.index = self.defs.iter().enumerate().map(|(i, d)| (d.id.clone(), i as u16)).collect();
    }
    /// Register or replace a tile definition (replacing keeps its numeric slot).
    pub fn register(&mut self, def: TileDef) -> u16 {
        if let Some(&i) = self.index.get(&def.id) {
            self.defs[i as usize] = def;
            return i;
        }
        let i = self.defs.len() as u16;
        self.index.insert(def.id.clone(), i);
        self.defs.push(def);
        i
    }
    pub fn tile_id(&self, id: &str) -> Option<u16> {
        self.index.get(id).copied()
    }
    pub fn in_bounds(&self, p: Pos) -> bool {
        p.x >= 0 && p.y >= 0 && p.x < self.width && p.y < self.height
    }
    pub fn get(&self, p: Pos) -> &TileDef {
        let i = if self.in_bounds(p) { self.tiles[(p.y * self.width + p.x) as usize] } else { 0 };
        &self.defs[i as usize]
    }
    pub fn set(&mut self, p: Pos, id: &str) -> bool {
        match (self.in_bounds(p), self.tile_id(id)) {
            (true, Some(t)) => {
                self.tiles[(p.y * self.width + p.x) as usize] = t;
                true
            }
            _ => false,
        }
    }
    pub fn fill(&mut self, id: &str) {
        if let Some(t) = self.tile_id(id) {
            self.tiles.iter_mut().for_each(|c| *c = t);
        }
    }
    pub fn walkable(&self, p: Pos) -> bool {
        self.in_bounds(p) && self.get(p).walkable
    }
    pub fn transparent(&self, p: Pos) -> bool {
        self.in_bounds(p) && self.get(p).transparent
    }
    pub fn defs(&self) -> &[TileDef] {
        &self.defs
    }
}
