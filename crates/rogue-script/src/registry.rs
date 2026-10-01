use rogue_core::grid::TileDef;
use rogue_core::{Entity, World};
use serde_json::Value;
use std::collections::HashMap;

use crate::ScriptError;

/// Data definitions keyed by id. Later mods replace earlier ones; `"$patch": true`
/// deep-merges instead, so a mod can tweak one field of an existing definition.
#[derive(Default)]
pub struct Registry {
    pub entities: HashMap<String, Value>,
}

pub fn merge_patch(base: &mut Value, patch: &Value) {
    match (base, patch) {
        (Value::Object(b), Value::Object(p)) => {
            for (k, v) in p {
                if k == "$patch" {
                    continue;
                }
                if v.is_null() {
                    b.remove(k);
                } else {
                    merge_patch(b.entry(k.clone()).or_insert(Value::Null), v);
                }
            }
        }
        (b, p) => *b = p.clone(),
    }
}

impl Registry {
    pub fn define_entity(&mut self, id: &str, def: Value) {
        let patch = def.get("$patch").and_then(Value::as_bool).unwrap_or(false);
        match self.entities.get_mut(id) {
            Some(existing) if patch => merge_patch(existing, &def),
            _ => {
                self.entities.insert(id.to_string(), def);
            }
        }
    }

    pub fn instantiate(&self, id: &str) -> Result<Entity, ScriptError> {
        let def = self.entities.get(id).ok_or_else(|| ScriptError::Unknown(format!("entity '{id}'")))?;
        let mut e: Entity = serde_json::from_value(def.clone()).map_err(|err| ScriptError::Data(format!("entity '{id}': {err}")))?;
        e.kind = id.to_string();
        if e.name.is_empty() {
            e.name = id.to_string();
        }
        Ok(e)
    }

    /// Load a data file: `{ "tiles": {id: def}, "entities": {id: def} }`.
    pub fn load_data(&mut self, world: &mut World, json: &str) -> Result<(), ScriptError> {
        let root: Value = serde_json::from_str(json).map_err(|e| ScriptError::Data(e.to_string()))?;
        if let Some(tiles) = root.get("tiles").and_then(Value::as_object) {
            for (id, def) in tiles {
                register_tile(world, id, def.clone())?;
            }
        }
        if let Some(ents) = root.get("entities").and_then(Value::as_object) {
            for (id, def) in ents {
                self.define_entity(id, def.clone());
            }
        }
        Ok(())
    }
}

pub fn register_tile(world: &mut World, id: &str, mut def: Value) -> Result<(), ScriptError> {
    if let Some(existing) = world.map.tile_id(id) {
        if def.get("$patch").and_then(Value::as_bool).unwrap_or(false) {
            let mut base = serde_json::to_value(&world.map.defs()[existing as usize]).unwrap();
            merge_patch(&mut base, &def);
            def = base;
        }
    }
    def["id"] = Value::String(id.to_string());
    let t: TileDef = serde_json::from_value(def).map_err(|e| ScriptError::Data(format!("tile '{id}': {e}")))?;
    world.map.register(t);
    Ok(())
}
