use serde::{Deserialize, Serialize};
use serde_json::{Map, Value};
use std::collections::BTreeSet;

use crate::grid::Pos;

/// Generational id: stale handles never alias a recycled slot.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash, PartialOrd, Ord, Serialize, Deserialize)]
#[serde(from = "u64", into = "u64")]
pub struct EntityId {
    pub index: u32,
    pub gen: u32,
}

impl From<u64> for EntityId {
    fn from(v: u64) -> Self {
        Self::from_u64(v)
    }
}
impl From<EntityId> for u64 {
    fn from(id: EntityId) -> u64 {
        id.to_u64()
    }
}

impl EntityId {
    /// Pack to a single integer (used as the Lua-facing handle).
    pub fn to_u64(self) -> u64 {
        ((self.gen as u64) << 32) | self.index as u64
    }
    pub fn from_u64(v: u64) -> Self {
        Self { index: v as u32, gen: (v >> 32) as u32 }
    }
}

#[derive(Clone, Debug, Serialize, Deserialize)]
#[serde(default)]
pub struct Stats {
    pub hp: i32,
    pub max_hp: i32,
    pub attack: i32,
    pub defense: i32,
    /// Energy gained per tick; 100 = one action per tick.
    pub speed: i32,
}

impl Default for Stats {
    fn default() -> Self {
        Self { hp: 10, max_hp: 10, attack: 1, defense: 0, speed: 100 }
    }
}

/// Entities are a bag of built-in parts plus a free-form `data` map that scripts own.
/// Anything a mod needs (hunger, mana, faction...) lives in `data`/`tags` with no engine change.
#[derive(Clone, Debug, Default, Serialize, Deserialize)]
#[serde(default)]
pub struct Entity {
    pub kind: String,
    pub name: String,
    pub pos: Pos,
    pub glyph: Option<char>,
    pub sprite: Option<String>,
    #[serde(default)]
    pub blocks: bool,
    pub stats: Option<Stats>,
    #[serde(default)]
    pub tags: BTreeSet<String>,
    #[serde(default)]
    pub data: Map<String, Value>,
    /// Item contents, when this entity can carry things.
    pub inventory: Option<Inventory>,
    /// Present when this entity is an item.
    pub item: Option<crate::item::ItemInfo>,
    /// Who carries this entity (it then has no map position).
    pub carried_by: Option<EntityId>,
}

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
#[serde(default)]
pub struct Inventory {
    pub capacity: usize,
    pub items: Vec<EntityId>,
    /// slot name -> equipped item
    pub equipped: std::collections::BTreeMap<String, EntityId>,
}
