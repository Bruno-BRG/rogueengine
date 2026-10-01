use serde::Deserialize;
use std::collections::HashMap;
use std::path::{Path, PathBuf};

use crate::ScriptError;

/// `mod.toml`
#[derive(Clone, Debug, Deserialize)]
pub struct ModManifest {
    pub id: String,
    #[serde(default)]
    pub name: String,
    #[serde(default)]
    pub version: String,
    #[serde(default)]
    pub depends: Vec<String>,
    /// Lua entry file, default `main.lua`.
    #[serde(default = "main_lua")]
    pub entry: String,
    /// JSON data files to load before the entry script.
    #[serde(default)]
    pub data: Vec<String>,
}
fn main_lua() -> String {
    "main.lua".into()
}

#[derive(Clone, Debug)]
pub struct ModSource {
    pub manifest: ModManifest,
    pub dir: Option<PathBuf>,
    /// Embedded files for built-in mods: (name, contents).
    pub embedded: Vec<(String, String)>,
}

impl ModSource {
    pub fn read(&self, file: &str) -> Result<String, ScriptError> {
        if let Some((_, c)) = self.embedded.iter().find(|(n, _)| n == file) {
            return Ok(c.clone());
        }
        let dir = self.dir.as_ref().ok_or_else(|| ScriptError::Unknown(format!("file '{file}' in mod '{}'", self.manifest.id)))?;
        std::fs::read_to_string(dir.join(file)).map_err(|e| ScriptError::Io(format!("{}: {e}", dir.join(file).display())))
    }
}

pub fn discover(root: &Path) -> Result<Vec<ModSource>, ScriptError> {
    let mut out = Vec::new();
    let Ok(rd) = std::fs::read_dir(root) else { return Ok(out) };
    for entry in rd.flatten() {
        let dir = entry.path();
        let mf = dir.join("mod.toml");
        if !mf.is_file() {
            continue;
        }
        let text = std::fs::read_to_string(&mf).map_err(|e| ScriptError::Io(e.to_string()))?;
        let manifest: ModManifest = toml::from_str(&text).map_err(|e| ScriptError::Data(format!("{}: {e}", mf.display())))?;
        out.push(ModSource { manifest, dir: Some(dir), embedded: vec![] });
    }
    Ok(out)
}

/// Dependencies first; ties broken by id so load order is deterministic.
pub fn load_order(mods: Vec<ModSource>) -> Result<Vec<ModSource>, ScriptError> {
    let mut by_id: HashMap<String, ModSource> = HashMap::new();
    for m in mods {
        by_id.insert(m.manifest.id.clone(), m);
    }
    let mut ids: Vec<String> = by_id.keys().cloned().collect();
    ids.sort();
    let mut done: Vec<String> = Vec::new();
    let mut visiting: Vec<String> = Vec::new();
    fn visit(id: &str, by_id: &HashMap<String, ModSource>, done: &mut Vec<String>, visiting: &mut Vec<String>) -> Result<(), ScriptError> {
        if done.iter().any(|d| d == id) {
            return Ok(());
        }
        if visiting.iter().any(|d| d == id) {
            return Err(ScriptError::Data(format!("mod dependency cycle at '{id}'")));
        }
        let m = by_id.get(id).ok_or_else(|| ScriptError::Unknown(format!("mod '{id}'")))?;
        visiting.push(id.to_string());
        let mut deps = m.manifest.depends.clone();
        deps.sort();
        for d in deps {
            if !by_id.contains_key(&d) {
                return Err(ScriptError::Data(format!("mod '{id}' depends on missing mod '{d}'")));
            }
            visit(&d, by_id, done, visiting)?;
        }
        visiting.pop();
        done.push(id.to_string());
        Ok(())
    }
    for id in &ids {
        visit(id, &by_id, &mut done, &mut visiting)?;
    }
    Ok(done.into_iter().map(|id| by_id.remove(&id).unwrap()).collect())
}
