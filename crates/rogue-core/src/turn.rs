use serde::{Deserialize, Serialize};
use std::collections::BTreeMap;

use crate::entity::EntityId;
use crate::world::World;

pub const ACTION_COST: i32 = 100;

/// Energy-based scheduler: faster actors act more often, slow ones less.
#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct TurnQueue {
    energy: BTreeMap<EntityId, i32>,
    pub tick: u64,
}

impl TurnQueue {
    pub fn add(&mut self, id: EntityId) {
        self.energy.insert(id, 0);
    }
    pub fn remove(&mut self, id: EntityId) {
        self.energy.remove(&id);
    }
    pub fn len(&self) -> usize {
        self.energy.len()
    }
    pub fn is_empty(&self) -> bool {
        self.energy.is_empty()
    }
}

impl World {
    /// Advance time until some actor can act and return it. `None` if nobody can.
    pub fn next_actor(&mut self) -> Option<EntityId> {
        if self.turn.is_empty() {
            return None;
        }
        loop {
            let ready = self.turn.energy.iter().filter(|(_, e)| **e >= ACTION_COST).max_by_key(|(id, e)| (**e, std::cmp::Reverse(**id))).map(|(id, _)| *id);
            if let Some(id) = ready {
                self.emit(crate::event::Event::TurnStarted { id });
                return Some(id);
            }
            self.turn.tick += 1;
            let ids: Vec<_> = self.turn.energy.keys().copied().collect();
            for id in ids {
                let speed = self.stat(id, "speed").max(1);
                *self.turn.energy.get_mut(&id).unwrap() += speed;
            }
        }
    }

    /// Charge `cost` energy for the action just taken.
    pub fn spend(&mut self, id: EntityId, cost: i32) {
        if let Some(e) = self.turn.energy.get_mut(&id) {
            *e -= cost;
        }
    }
}
