//! RogueEngine core: pure game rules. No UI, no scripting dependency.
pub mod entity;
pub mod event;
pub mod fov;
pub mod grid;
pub mod item;
pub mod path;
pub mod physics;
pub mod procgen;
pub mod rng;
pub mod turn;
pub mod world;

pub use entity::{Entity, EntityId, Stats};
pub use event::Event;
pub use grid::{Pos, TileDef, TileMap};
pub use world::World;
