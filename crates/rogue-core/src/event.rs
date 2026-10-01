use serde::{Deserialize, Serialize};

use crate::entity::EntityId;
use crate::grid::Pos;

/// Everything observable that happens. The script layer turns these into Lua hooks;
/// the UI turns them into animations/sounds/log lines.
#[derive(Clone, Debug, PartialEq, Serialize, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum Event {
    Spawned { id: EntityId },
    Moved { id: EntityId, from: Pos, to: Pos },
    Blocked { id: EntityId, by: Option<EntityId>, at: Pos },
    Attacked { attacker: EntityId, target: EntityId, damage: i32 },
    Died { id: EntityId, killer: Option<EntityId> },
    Despawned { id: EntityId },
    ItemPicked { who: EntityId, item: EntityId },
    ItemDropped { who: EntityId, item: EntityId },
    Equipped { who: EntityId, item: EntityId },
    Unequipped { who: EntityId, item: EntityId },
    TurnStarted { id: EntityId },
    Message { text: String },
}

impl Event {
    /// Hook name used by scripts: `rogue.on("died", fn)`.
    pub fn name(&self) -> &'static str {
        match self {
            Event::Spawned { .. } => "spawned",
            Event::Moved { .. } => "moved",
            Event::Blocked { .. } => "blocked",
            Event::Attacked { .. } => "attacked",
            Event::Died { .. } => "died",
            Event::Despawned { .. } => "despawned",
            Event::ItemPicked { .. } => "item_picked",
            Event::ItemDropped { .. } => "item_dropped",
            Event::Equipped { .. } => "equipped",
            Event::Unequipped { .. } => "unequipped",
            Event::TurnStarted { .. } => "turn_started",
            Event::Message { .. } => "message",
        }
    }
}
