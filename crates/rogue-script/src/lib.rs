//! Lua scripting + mod loading for RogueEngine.
//!
//! Layering: Lua can only touch the world through the `rogue.*` API registered here,
//! and Rust plugins can extend that API through the [`Plugin`] trait.
mod api;
pub mod modload;
pub mod registry;

use mlua::{Lua, LuaSerdeExt, Table, Value as LuaValue};
use rogue_core::{Event, World};
use std::cell::RefCell;
use std::path::Path;
use std::rc::Rc;

pub use modload::{ModManifest, ModSource};
pub use registry::Registry;

#[derive(Debug, thiserror::Error)]
pub enum ScriptError {
    #[error("lua: {0}")]
    Lua(String),
    #[error("unknown {0}")]
    Unknown(String),
    #[error("data: {0}")]
    Data(String),
    #[error("io: {0}")]
    Io(String),
}

impl From<mlua::Error> for ScriptError {
    fn from(e: mlua::Error) -> Self {
        ScriptError::Lua(e.to_string())
    }
}

/// Rust-side extension point: add functions to the `rogue` table.
/// This is how native plugins (new systems, renderers, tools) hook in.
pub trait Plugin {
    fn id(&self) -> &str;
    fn register(&self, lua: &Lua, rogue: &Table, world: Rc<RefCell<World>>) -> mlua::Result<()>;
}

/// Result of advancing the simulation.
#[derive(Debug, PartialEq, Eq)]
pub enum Turn {
    /// A player-controlled actor is ready and waiting for `act`.
    AwaitingPlayer,
    /// No actors exist, or the player is dead (game over).
    Idle,
}

pub struct Engine {
    pub lua: Lua,
    pub world: Rc<RefCell<World>>,
    pub registry: Rc<RefCell<Registry>>,
    pub loaded_mods: Vec<ModManifest>,
}

const BASE_MANIFEST: &str = r#"
id = "base"
name = "RogueEngine Base"
version = "0.1.0"
data = ["data.json"]
"#;

impl Engine {
    pub fn new(width: i32, height: i32, seed: u64) -> Result<Self, ScriptError> {
        let lua = Lua::new();
        let world = Rc::new(RefCell::new(World::new(width, height, seed)));
        let registry = Rc::new(RefCell::new(Registry::default()));
        sandbox(&lua)?;
        api::install(&lua, world.clone(), registry.clone())?;
        let mut engine = Self { lua, world, registry, loaded_mods: vec![] };
        engine.load_mods(vec![base_mod()])?;
        Ok(engine)
    }

    pub fn add_plugin(&mut self, plugin: &dyn Plugin) -> Result<(), ScriptError> {
        let rogue: Table = self.lua.globals().get("rogue")?;
        plugin.register(&self.lua, &rogue, self.world.clone())?;
        Ok(())
    }

    /// Discover `*/mod.toml` under `root` and load them in dependency order.
    pub fn load_mods_dir(&mut self, root: &Path) -> Result<(), ScriptError> {
        self.load_mods(modload::discover(root)?)
    }

    pub fn load_mods(&mut self, mods: Vec<ModSource>) -> Result<(), ScriptError> {
        // Already-loaded mods count as satisfied dependencies.
        let loaded: Vec<String> = self.loaded_mods.iter().map(|m| m.id.clone()).collect();
        let mut pending = mods;
        for m in &mut pending {
            m.manifest.depends.retain(|d| !loaded.contains(d));
        }
        for m in modload::load_order(pending)? {
            self.load_one(&m)?;
            self.loaded_mods.push(m.manifest);
        }
        Ok(())
    }

    fn load_one(&mut self, m: &ModSource) -> Result<(), ScriptError> {
        let id = &m.manifest.id;
        for f in &m.manifest.data {
            let text = m.read(f)?;
            self.registry.borrow_mut().load_data(&mut self.world.borrow_mut(), &text).map_err(|e| ScriptError::Data(format!("mod '{id}' {f}: {e}")))?;
        }
        let rogue: Table = self.lua.globals().get("rogue")?;
        let info = self.lua.create_table()?;
        info.set("id", id.as_str())?;
        info.set("name", m.manifest.name.as_str())?;
        info.set("version", m.manifest.version.as_str())?;
        if let Some(dir) = &m.dir {
            info.set("dir", dir.to_string_lossy().as_ref())?;
            // `require "foo"` resolves inside the mod's own folder.
            let package: Table = self.lua.globals().get("package")?;
            package.set("path", format!("{0}/?.lua;{0}/?/init.lua", dir.display()))?;
        }
        rogue.set("mod", info)?;
        if m.dir.is_some() || m.embedded.iter().any(|(n, _)| *n == m.manifest.entry) {
            if let Ok(code) = m.read(&m.manifest.entry) {
                self.lua.load(&code).set_name(format!("@{}/{}", id, m.manifest.entry)).exec().map_err(|e| ScriptError::Lua(format!("mod '{id}': {e}")))?;
            }
        }
        Ok(())
    }

    /// Run arbitrary Lua (editor console, tests, graph output).
    pub fn exec(&self, code: &str) -> Result<(), ScriptError> {
        self.lua.load(code).exec()?;
        Ok(())
    }

    /// Evaluate Lua and convert the result to JSON (used for host-side queries like the HUD).
    pub fn eval_json(&self, code: &str) -> Result<serde_json::Value, ScriptError> {
        let v: LuaValue = self.lua.load(code).eval()?;
        Ok(self.lua.from_value(v)?)
    }

    pub fn eval<T: mlua::FromLua>(&self, code: &str) -> Result<T, ScriptError> {
        Ok(self.lua.load(code).eval()?)
    }

    /// Turn queued engine events into Lua hook calls (`rogue.on(name, fn)`).
    /// Handlers may emit more events; loops until the queue is quiet.
    pub fn dispatch_events(&self) -> Result<Vec<Event>, ScriptError> {
        let mut all = Vec::new();
        for _ in 0..64 {
            let events = self.world.borrow_mut().drain_events();
            if events.is_empty() {
                break;
            }
            let rogue: Table = self.lua.globals().get("rogue")?;
            let emit: mlua::Function = rogue.get("_dispatch")?;
            for e in &events {
                let v = self.lua.to_value_with(e, mlua::serde::SerializeOptions::new().serialize_none_to_null(false))?;
                emit.call::<()>((e.name(), v))?;
            }
            all.extend(events);
        }
        Ok(all)
    }

    fn player(&self) -> Option<rogue_core::EntityId> {
        self.world.borrow().find_tagged("player").next()
    }

    /// Run actors until the player is up. Non-player actors run `rogue.rules.ai(id)`,
    /// which may return an energy cost (default 100).
    pub fn advance(&self) -> Result<Turn, ScriptError> {
        let rules: Table = self.lua.globals().get::<Table>("rogue")?.get("rules")?;
        let ai: mlua::Function = rules.get("ai")?;
        for _ in 0..10_000 {
            // No player left (died): the game is over, don't let monsters run forever.
            if self.player().is_none() {
                self.dispatch_events()?;
                return Ok(Turn::Idle);
            }
            let Some(id) = self.world.borrow_mut().next_actor() else { return Ok(Turn::Idle) };
            if self.world.borrow().get(id).is_some_and(|e| e.tags.contains("player")) {
                self.dispatch_events()?;
                return Ok(Turn::AwaitingPlayer);
            }
            let cost: Option<i32> = ai.call(id.to_u64())?;
            self.world.borrow_mut().spend(id, cost.unwrap_or(100));
            self.dispatch_events()?;
        }
        Err(ScriptError::Lua("advance(): actor loop did not settle".into()))
    }

    /// Player input: `rogue.rules.player_action(id, action, arg)` returns the energy cost,
    /// or `false`/`nil` if the action was invalid (no time passes).
    pub fn act(&self, action: &str, arg: serde_json::Value) -> Result<Turn, ScriptError> {
        let Some(player) = self.player() else { return Ok(Turn::Idle) };
        let rules: Table = self.lua.globals().get::<Table>("rogue")?.get("rules")?;
        let f: mlua::Function = rules.get("player_action")?;
        let arg = self.lua.to_value(&arg)?;
        let cost: LuaValue = f.call((player.to_u64(), action, arg))?;
        let cost = match cost {
            LuaValue::Integer(n) => n as i32,
            LuaValue::Number(n) => n as i32,
            LuaValue::Boolean(true) => 100,
            _ => {
                self.dispatch_events()?;
                return Ok(Turn::AwaitingPlayer);
            }
        };
        self.world.borrow_mut().spend(player, cost);
        self.dispatch_events()?;
        self.advance()
    }
}

/// The base mod's JSON data (tiles + entities), for editors that show built-in objects.
pub fn base_data() -> &'static str {
    include_str!("../lua/base/data.json")
}

fn base_mod() -> ModSource {
    ModSource {
        manifest: toml::from_str(BASE_MANIFEST).unwrap(),
        dir: None,
        embedded: vec![
            ("main.lua".into(), include_str!("../lua/base/main.lua").into()),
            ("data.json".into(), include_str!("../lua/base/data.json").into()),
        ],
    }
}

/// Remove ambient authority from the Lua state. This blocks accidental damage and the
/// obvious escapes; it is not a security boundary against hostile mods — treat mods like plugins.
fn sandbox(lua: &Lua) -> mlua::Result<()> {
    let g = lua.globals();
    for name in ["dofile", "loadfile", "io", "debug"] {
        g.set(name, LuaValue::Nil)?;
    }
    let os: Table = g.get("os")?;
    for name in ["execute", "remove", "rename", "exit", "tmpname", "getenv", "setlocale"] {
        os.set(name, LuaValue::Nil)?;
    }
    Ok(())
}
