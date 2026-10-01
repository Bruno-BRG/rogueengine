//! Runs the engine on its own thread (Lua state is `!Send`) behind a JSON request/response
//! protocol. The Tauri app, tests, and any future frontend (CLI, web) all use this one surface.
use rogue_core::grid::{Pos, TileDef};
use rogue_core::World;
use rogue_graph::{Graph, Library};
use rogue_script::{Engine, Turn};
use serde::{Deserialize, Serialize};
use serde_json::Value;
use std::collections::HashSet;
use std::path::PathBuf;
use std::sync::mpsc;

#[derive(Debug, Deserialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum Request {
    /// Start (or restart) a game with the mods currently on disk.
    NewGame { seed: u64, #[serde(default = "default_w")] width: i32, #[serde(default = "default_h")] height: i32 },
    /// Player command, forwarded to `rogue.rules.player_action`.
    Act { action: String, #[serde(default)] arg: Value },
    /// Run Lua in the live game (editor console).
    Exec { code: String },
    /// Compile a visual graph to Lua without running it.
    CompileGraph { graph: Graph },
    /// Compile a graph and load it into the running game.
    ApplyGraph { graph: Graph },
    /// Node palette for the graph editor.
    NodeLibrary,
    ListMods,
}
fn default_w() -> i32 { 60 }
fn default_h() -> i32 { 36 }

#[derive(Debug, Serialize)]
#[serde(tag = "type", rename_all = "snake_case")]
pub enum Response {
    State(Box<Snapshot>),
    Lua { code: String },
    Nodes { nodes: Vec<rogue_graph::NodeDef> },
    Mods { mods: Vec<ModInfo> },
    Ok,
    Error { message: String },
}

#[derive(Debug, Serialize)]
pub struct ModInfo {
    pub id: String,
    pub name: String,
    pub version: String,
}

#[derive(Debug, Serialize)]
pub struct EntityView {
    pub id: u64,
    pub x: i32,
    pub y: i32,
    pub name: String,
    pub kind: String,
    pub glyph: Option<char>,
    pub sprite: Option<String>,
}

#[derive(Debug, Serialize)]
pub struct ItemView {
    pub id: u64,
    pub name: String,
    pub glyph: Option<char>,
    pub equipped: bool,
    pub usable: bool,
}

/// Everything a renderer needs for one frame. Tiles are indices into `tile_defs`.
#[derive(Debug, Serialize)]
pub struct Snapshot {
    pub width: i32,
    pub height: i32,
    pub tile_defs: Vec<TileDef>,
    pub tiles: Vec<u16>,
    /// 0 = unseen, 1 = remembered, 2 = visible now.
    pub fog: Vec<u8>,
    pub entities: Vec<EntityView>,
    pub player: Option<EntityView>,
    pub hp: i32,
    pub max_hp: i32,
    pub inventory: Vec<ItemView>,
    pub log: Vec<String>,
    pub game_over: bool,
}

#[derive(Clone)]
pub struct Host {
    tx: mpsc::Sender<(Request, mpsc::Sender<Response>)>,
}

impl Host {
    /// `mods_dir` is scanned for `*/mod.toml` at every new game.
    pub fn spawn(mods_dir: Option<PathBuf>) -> Host {
        let (tx, rx) = mpsc::channel::<(Request, mpsc::Sender<Response>)>();
        std::thread::spawn(move || {
            let mut session = Session::new(mods_dir);
            for (req, reply) in rx {
                let resp = session.handle(req).unwrap_or_else(|message| Response::Error { message });
                let _ = reply.send(resp);
            }
        });
        Host { tx }
    }

    pub fn request(&self, req: Request) -> Response {
        let (rtx, rrx) = mpsc::channel();
        if self.tx.send((req, rtx)).is_err() {
            return Response::Error { message: "engine thread stopped".into() };
        }
        rrx.recv().unwrap_or(Response::Error { message: "engine thread stopped".into() })
    }
}

struct Session {
    mods_dir: Option<PathBuf>,
    engine: Option<Engine>,
    explored: HashSet<Pos>,
    graphs: Library,
    over: bool,
}

impl Session {
    fn new(mods_dir: Option<PathBuf>) -> Self {
        Self { mods_dir, engine: None, explored: HashSet::new(), graphs: Library::builtin(), over: false }
    }

    fn engine(&self) -> Result<&Engine, String> {
        self.engine.as_ref().ok_or_else(|| "no game running; send new_game first".to_string())
    }

    fn handle(&mut self, req: Request) -> Result<Response, String> {
        match req {
            Request::NewGame { seed, width, height } => {
                let mut e = Engine::new(width, height, seed).map_err(|e| e.to_string())?;
                if let Some(dir) = &self.mods_dir {
                    e.load_mods_dir(dir).map_err(|e| e.to_string())?;
                }
                e.exec("rogue.rules.new_game(1)").map_err(|e| e.to_string())?;
                e.advance().map_err(|e| e.to_string())?;
                self.explored.clear();
                self.over = false;
                self.engine = Some(e);
                self.snapshot()
            }
            Request::Act { action, arg } => {
                let turn = self.engine()?.act(&action, arg).map_err(|e| e.to_string())?;
                self.over = turn == Turn::Idle || self.engine()?.world.borrow().find_tagged("player").next().is_none();
                self.snapshot()
            }
            Request::Exec { code } => {
                self.engine()?.exec(&code).map_err(|e| e.to_string())?;
                self.engine()?.dispatch_events().map_err(|e| e.to_string())?;
                self.snapshot()
            }
            Request::CompileGraph { graph } => {
                let code = rogue_graph::compile(&graph, &self.graphs).map_err(|e| e.to_string())?;
                Ok(Response::Lua { code })
            }
            Request::ApplyGraph { graph } => {
                let code = rogue_graph::compile(&graph, &self.graphs).map_err(|e| e.to_string())?;
                self.engine()?.exec(&code).map_err(|e| e.to_string())?;
                Ok(Response::Ok)
            }
            Request::NodeLibrary => Ok(Response::Nodes { nodes: self.graphs.all().into_iter().cloned().collect() }),
            Request::ListMods => {
                let mods = self.engine()?.loaded_mods.iter().map(|m| ModInfo { id: m.id.clone(), name: m.name.clone(), version: m.version.clone() }).collect();
                Ok(Response::Mods { mods })
            }
        }
    }

    fn snapshot(&mut self) -> Result<Response, String> {
        let engine = self.engine.as_ref().ok_or("no game")?;
        let world = engine.world.borrow();
        let player_id = world.find_tagged("player").next();
        let mut fog = vec![0u8; (world.map.width * world.map.height) as usize];
        let mut visible: HashSet<Pos> = HashSet::new();
        if let Some(p) = player_id.and_then(|id| world.get(id)) {
            visible = rogue_core::fov::compute_fov(&world.map, p.pos, 9);
            self.explored.extend(visible.iter().copied());
        }
        for p in &self.explored {
            if world.map.in_bounds(*p) {
                fog[(p.y * world.map.width + p.x) as usize] = 1;
            }
        }
        for p in &visible {
            if world.map.in_bounds(*p) {
                fog[(p.y * world.map.width + p.x) as usize] = 2;
            }
        }
        let view = |id: rogue_core::EntityId| world.get(id).map(|e| EntityView { id: id.to_u64(), x: e.pos.x, y: e.pos.y, name: e.name.clone(), kind: e.kind.clone(), glyph: e.glyph, sprite: e.sprite.clone() });
        let entities = world.ids().filter(|id| world.get(*id).is_some_and(|e| e.carried_by.is_none() && visible.contains(&e.pos))).filter_map(view).collect();
        let (hp, max_hp) = player_id.and_then(|id| world.get(id)?.stats.as_ref().map(|s| (s.hp, world.stat(id, "max_hp")))).unwrap_or((0, 0));
        let inventory = player_id.and_then(|id| world.get(id)?.inventory.as_ref()).map(|inv| {
            inv.items.iter().filter_map(|i| {
                let e = world.get(*i)?;
                Some(ItemView { id: i.to_u64(), name: e.name.clone(), glyph: e.glyph, equipped: inv.equipped.values().any(|v| v == i), usable: e.item.as_ref().is_some_and(|it| it.on_use.is_some()) })
            }).collect()
        }).unwrap_or_default();
        let log = world.log.iter().rev().take(50).rev().cloned().collect();
        Ok(Response::State(Box::new(Snapshot {
            width: world.map.width,
            height: world.map.height,
            tile_defs: world.map.defs().to_vec(),
            tiles: tiles_of(&world),
            fog,
            entities,
            player: player_id.and_then(view),
            hp,
            max_hp,
            inventory,
            log,
            game_over: self.over || player_id.is_none(),
        })))
    }
}

fn tiles_of(world: &World) -> Vec<u16> {
    let mut v = Vec::with_capacity((world.map.width * world.map.height) as usize);
    for y in 0..world.map.height {
        for x in 0..world.map.width {
            let name = &world.map.get(Pos::new(x, y)).id;
            v.push(world.map.tile_id(name).unwrap_or(0));
        }
    }
    v
}
