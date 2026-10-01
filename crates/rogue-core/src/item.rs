use serde::{Deserialize, Serialize};
use serde_json::{Map, Value};

use crate::entity::EntityId;
use crate::event::Event;
use crate::world::World;

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct ItemInfo {
    /// Equip slot ("weapon", "head"...); `None` means not equippable.
    pub slot: Option<String>,
    #[serde(default)]
    pub stackable: bool,
    #[serde(default = "one")]
    pub count: u32,
    /// Stat modifiers applied while equipped: {"attack": 2}.
    #[serde(default)]
    pub modifiers: Map<String, Value>,
    /// Lua hook name invoked on use (resolved by the script layer).
    pub on_use: Option<String>,
}
fn one() -> u32 {
    1
}

#[derive(Debug, PartialEq, Eq)]
pub enum ItemError {
    NotAnItem,
    NoInventory,
    Full,
    NotCarried,
    NotEquippable,
    NotHere,
}

impl World {
    pub fn pickup(&mut self, who: EntityId, item: EntityId) -> Result<(), ItemError> {
        let (ipos, is_item) = {
            let e = self.get(item).ok_or(ItemError::NotAnItem)?;
            (e.pos, e.item.is_some() && e.carried_by.is_none())
        };
        if !is_item {
            return Err(ItemError::NotAnItem);
        }
        let w = self.get(who).ok_or(ItemError::NoInventory)?;
        if w.pos != ipos {
            return Err(ItemError::NotHere);
        }
        let inv = w.inventory.as_ref().ok_or(ItemError::NoInventory)?;
        if inv.items.len() >= inv.capacity {
            return Err(ItemError::Full);
        }
        self.get_mut(who).unwrap().inventory.as_mut().unwrap().items.push(item);
        self.get_mut(item).unwrap().carried_by = Some(who);
        self.emit(Event::ItemPicked { who, item });
        Ok(())
    }

    pub fn drop_item(&mut self, who: EntityId, item: EntityId) -> Result<(), ItemError> {
        self.unequip_item(who, item).ok();
        let pos = self.get(who).ok_or(ItemError::NoInventory)?.pos;
        let inv = self.get_mut(who).and_then(|e| e.inventory.as_mut()).ok_or(ItemError::NoInventory)?;
        let before = inv.items.len();
        inv.items.retain(|i| *i != item);
        if inv.items.len() == before {
            return Err(ItemError::NotCarried);
        }
        let e = self.get_mut(item).unwrap();
        e.carried_by = None;
        e.pos = pos;
        self.emit(Event::ItemDropped { who, item });
        Ok(())
    }

    /// Equip into the item's slot, swapping out whatever was there.
    pub fn equip(&mut self, who: EntityId, item: EntityId) -> Result<(), ItemError> {
        let slot = self.get(item).and_then(|e| e.item.as_ref()).and_then(|i| i.slot.clone()).ok_or(ItemError::NotEquippable)?;
        let inv = self.get(who).and_then(|e| e.inventory.as_ref()).ok_or(ItemError::NoInventory)?;
        if !inv.items.contains(&item) {
            return Err(ItemError::NotCarried);
        }
        if let Some(old) = inv.equipped.get(&slot).copied() {
            self.unequip_item(who, old).ok();
        }
        self.get_mut(who).unwrap().inventory.as_mut().unwrap().equipped.insert(slot, item);
        self.emit(Event::Equipped { who, item });
        Ok(())
    }

    pub fn unequip_item(&mut self, who: EntityId, item: EntityId) -> Result<(), ItemError> {
        let inv = self.get_mut(who).and_then(|e| e.inventory.as_mut()).ok_or(ItemError::NoInventory)?;
        let slot = inv.equipped.iter().find(|(_, v)| **v == item).map(|(k, _)| k.clone()).ok_or(ItemError::NotCarried)?;
        inv.equipped.remove(&slot);
        self.emit(Event::Unequipped { who, item });
        Ok(())
    }

    /// Effective value of a stat: base + equipped modifiers.
    pub fn stat(&self, who: EntityId, name: &str) -> i32 {
        let Some(e) = self.get(who) else { return 0 };
        let base = e.stats.as_ref().map_or(0, |s| match name {
            "hp" => s.hp,
            "max_hp" => s.max_hp,
            "attack" => s.attack,
            "defense" => s.defense,
            "speed" => s.speed,
            _ => 0,
        });
        let bonus: i32 = e
            .inventory
            .iter()
            .flat_map(|inv| inv.equipped.values())
            .filter_map(|id| self.get(*id)?.item.as_ref()?.modifiers.get(name)?.as_i64())
            .map(|v| v as i32)
            .sum();
        base + bonus
    }
}
