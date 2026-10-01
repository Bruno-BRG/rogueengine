use rogue_script::{ModManifest, ModSource};
use serde::{Deserialize, Serialize};
use serde_json::{json, Map, Value};
use std::fs;
use std::path::{Path, PathBuf};

pub const PROJECT_FILE: &str = "game.reproj";

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Meta {
    pub name: String,
    #[serde(default = "v1")]
    pub engine_version: String,
}
fn v1() -> String {
    "0.1".into()
}

/// A game project: a folder on disk.
/// ```text
/// game.reproj          metadata
/// data/*.json          entities & tiles (merged over the base mod)
/// scripts/*.lua        Lua, run in alphabetical order after data
/// graphs/*.graph.json  visual scripts, compiled to Lua at game start
/// sprites/*.png|json   sprite sheets + metadata
/// ```
pub struct Project {
    pub dir: PathBuf,
    pub meta: Meta,
}

/// Asset names become file names, so they must be boring: no separators, no dots.
pub fn valid_name(n: &str) -> Result<&str, String> {
    let ok = !n.is_empty() && n.len() <= 64 && n.chars().all(|c| c.is_ascii_alphanumeric() || c == '_' || c == '-');
    if ok { Ok(n) } else { Err(format!("invalid name '{n}': use letters, digits, '_' and '-'")) }
}

pub fn slug(name: &str) -> String {
    let s: String = name.trim().to_lowercase().chars().map(|c| if c.is_ascii_alphanumeric() { c } else { '-' }).collect();
    let s = s.trim_matches('-').to_string();
    if s.is_empty() { "game".into() } else { s }
}

impl Project {
    pub fn open(dir: &Path) -> Result<Self, String> {
        let text = fs::read_to_string(dir.join(PROJECT_FILE)).map_err(|_| format!("{} is not a RogueEngine project (no {PROJECT_FILE})", dir.display()))?;
        let meta: Meta = serde_json::from_str(&text).map_err(|e| format!("{PROJECT_FILE}: {e}"))?;
        Ok(Self { dir: dir.to_path_buf(), meta })
    }

    pub fn create(parent: &Path, name: &str, template: &str) -> Result<Self, String> {
        let dir = parent.join(slug(name));
        if dir.exists() && fs::read_dir(&dir).map(|mut d| d.next().is_some()).unwrap_or(false) {
            return Err(format!("{} already exists and is not empty", dir.display()));
        }
        for sub in ["data", "scripts", "graphs", "sprites"] {
            fs::create_dir_all(dir.join(sub)).map_err(|e| e.to_string())?;
        }
        let meta = Meta { name: name.trim().to_string(), engine_version: v1() };
        fs::write(dir.join(PROJECT_FILE), serde_json::to_string_pretty(&meta).unwrap()).map_err(|e| e.to_string())?;
        let p = Self { dir, meta };
        if template != "empty" {
            crate::template::dungeon(&p)?;
        }
        Ok(p)
    }

    fn path(&self, sub: &str, name: &str, ext: &str) -> Result<PathBuf, String> {
        Ok(self.dir.join(sub).join(format!("{}{ext}", valid_name(name)?)))
    }

    pub fn list(&self, sub: &str, suffix: &str) -> Vec<String> {
        let mut v: Vec<String> = fs::read_dir(self.dir.join(sub))
            .into_iter()
            .flatten()
            .flatten()
            .filter_map(|e| e.file_name().to_str().and_then(|n| n.strip_suffix(suffix)).map(str::to_string))
            .filter(|n| valid_name(n).is_ok())
            .collect();
        v.sort();
        v
    }

    pub fn read_text(&self, sub: &str, name: &str, ext: &str) -> Result<String, String> {
        fs::read_to_string(self.path(sub, name, ext)?).map_err(|e| format!("{name}: {e}"))
    }
    pub fn write_text(&self, sub: &str, name: &str, ext: &str, text: &str) -> Result<(), String> {
        fs::write(self.path(sub, name, ext)?, text).map_err(|e| e.to_string())
    }
    pub fn write_bytes(&self, sub: &str, name: &str, ext: &str, bytes: &[u8]) -> Result<(), String> {
        fs::write(self.path(sub, name, ext)?, bytes).map_err(|e| e.to_string())
    }
    pub fn read_bytes(&self, sub: &str, name: &str, ext: &str) -> Result<Vec<u8>, String> {
        fs::read(self.path(sub, name, ext)?).map_err(|e| format!("{name}: {e}"))
    }
    pub fn delete(&self, sub: &str, name: &str, exts: &[&str]) -> Result<(), String> {
        for ext in exts {
            let p = self.path(sub, name, ext)?;
            if p.exists() {
                fs::remove_file(p).map_err(|e| e.to_string())?;
            }
        }
        Ok(())
    }

    /// `data/entities.json` / `data/tiles.json` as objects keyed by id.
    pub fn objects(&self) -> Value {
        let read = |file: &str, key: &str| -> Value {
            fs::read_to_string(self.dir.join("data").join(file)).ok().and_then(|t| serde_json::from_str::<Value>(&t).ok()).and_then(|v| v.get(key).cloned()).unwrap_or_else(|| json!({}))
        };
        json!({ "entities": read("entities.json", "entities"), "tiles": read("tiles.json", "tiles") })
    }

    pub fn save_objects(&self, entities: &Map<String, Value>, tiles: &Map<String, Value>) -> Result<(), String> {
        let w = |file: &str, key: &str, m: &Map<String, Value>| {
            fs::write(self.dir.join("data").join(file), serde_json::to_string_pretty(&json!({ key: m })).unwrap()).map_err(|e| e.to_string())
        };
        w("entities.json", "entities", entities)?;
        w("tiles.json", "tiles", tiles)
    }

    /// The game as a mod: data files, then every script, then every compiled graph.
    pub fn to_mod(&self) -> Result<ModSource, String> {
        let mut entry = String::from("-- generated entry: scripts, then visual graphs\n");
        let load = |name: &str, code: &str| format!("assert(load({}, {}))()\n", rogue_graph::lua_string(code), rogue_graph::lua_string(&format!("@{name}")));
        for s in self.list("scripts", ".lua") {
            entry.push_str(&load(&format!("scripts/{s}.lua"), &self.read_text("scripts", &s, ".lua")?));
        }
        let lib = rogue_graph::Library::builtin();
        for g in self.list("graphs", ".graph.json") {
            let graph: rogue_graph::Graph = serde_json::from_str(&self.read_text("graphs", &g, ".graph.json")?).map_err(|e| format!("graph '{g}': {e}"))?;
            let code = rogue_graph::compile(&graph, &lib).map_err(|e| format!("graph '{g}': {e}"))?;
            entry.push_str(&load(&format!("graphs/{g}"), &code));
        }
        let data: Vec<String> = ["data/tiles.json", "data/entities.json"].iter().filter(|f| self.dir.join(f).is_file()).map(|f| f.to_string()).collect();
        Ok(ModSource {
            manifest: ModManifest { id: "project".into(), name: self.meta.name.clone(), version: self.meta.engine_version.clone(), depends: vec!["base".into()], entry: "entry.lua".into(), data },
            dir: Some(self.dir.clone()),
            embedded: vec![("entry.lua".into(), entry)],
        })
    }
}
