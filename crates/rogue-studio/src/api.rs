//! Studio API: one `call(cmd, args)` entry point shared by the Tauri app and the dev/web server.
use base64::{engine::general_purpose::STANDARD as B64, Engine as _};
use rogue_assets::{Sprite, SpriteEditor};
use rogue_host::Host;
use serde_json::{json, Value};
use std::path::{Path, PathBuf};

use crate::project::{valid_name, Project};

struct SpriteSession {
    name: String,
    editor: SpriteEditor,
    fps: u32,
}

pub struct Studio {
    host: Host,
    project: Option<Project>,
    sprite: Option<SpriteSession>,
}

pub fn save_sprite(p: &Project, ed: &SpriteEditor, fps: u32) -> Result<(), String> {
    let s = &ed.sprite;
    p.write_bytes("sprites", &s.name, ".png", &s.sheet_png().map_err(|e| e.to_string())?)?;
    let meta = json!({ "width": s.width, "height": s.height, "frames": s.frames.len(), "fps": fps });
    p.write_text("sprites", &s.name, ".json", &meta.to_string())
}

fn load_sprite(p: &Project, name: &str) -> Result<(SpriteEditor, u32), String> {
    let meta: Value = serde_json::from_str(&p.read_text("sprites", name, ".json")?).map_err(|e| e.to_string())?;
    let w = meta["width"].as_u64().unwrap_or(16) as u32;
    let fps = meta["fps"].as_u64().unwrap_or(4) as u32;
    let sprite = Sprite::from_png(name, &p.read_bytes("sprites", name, ".png")?, Some(w)).map_err(|e| e.to_string())?;
    Ok((SpriteEditor::new(sprite), fps))
}

fn arg<'a>(args: &'a Value, key: &str) -> Result<&'a Value, String> {
    args.get(key).ok_or_else(|| format!("missing argument '{key}'"))
}
fn str_arg<'a>(args: &'a Value, key: &str) -> Result<&'a str, String> {
    arg(args, key)?.as_str().ok_or_else(|| format!("argument '{key}' must be a string"))
}

impl Studio {
    pub fn new(host: Host) -> Self {
        Self { host, project: None, sprite: None }
    }

    fn project(&self) -> Result<&Project, String> {
        self.project.as_ref().ok_or_else(|| "no project open".to_string())
    }

    fn project_json(&self) -> Result<Value, String> {
        let p = self.project()?;
        let sprites: Vec<Value> = p.list("sprites", ".json").iter().filter_map(|n| {
            let m: Value = serde_json::from_str(&p.read_text("sprites", n, ".json").ok()?).ok()?;
            Some(json!({ "name": n, "width": m["width"], "height": m["height"], "frames": m["frames"], "fps": m["fps"] }))
        }).collect();
        let base: Value = serde_json::from_str(rogue_script::base_data()).unwrap();
        Ok(json!({
            "name": p.meta.name, "dir": p.dir,
            "sprites": sprites,
            "graphs": p.list("graphs", ".graph.json"),
            "scripts": p.list("scripts", ".lua"),
            "objects": p.objects(),
            "base": base,
        }))
    }

    pub fn call(&mut self, cmd: &str, args: Value) -> Result<Value, String> {
        match cmd {
            "app_info" => {
                let home = std::env::var_os("HOME").or_else(|| std::env::var_os("USERPROFILE")).map(PathBuf::from).unwrap_or_else(|| PathBuf::from("."));
                Ok(json!({ "projects_dir": home.join("RogueEngineProjects"), "version": env!("CARGO_PKG_VERSION") }))
            }
            "project_create" => {
                let parent = PathBuf::from(str_arg(&args, "parent")?);
                std::fs::create_dir_all(&parent).map_err(|e| e.to_string())?;
                let name = str_arg(&args, "name")?;
                if name.trim().is_empty() {
                    return Err("give your game a name".into());
                }
                let template = args.get("template").and_then(Value::as_str).unwrap_or("dungeon");
                self.project = Some(Project::create(&parent, name, template)?);
                self.sprite = None;
                self.project_json()
            }
            "project_open" => {
                self.project = Some(Project::open(Path::new(str_arg(&args, "dir")?))?);
                self.sprite = None;
                self.project_json()
            }
            "project_close" => {
                self.project = None;
                self.sprite = None;
                Ok(json!(null))
            }
            "project_info" => self.project_json(),

            // ---- sprites (auto-saved on every edit)
            "sprite_new" => {
                let p = self.project()?;
                let name = valid_name(str_arg(&args, "name")?)?.to_string();
                let (w, h) = (arg(&args, "width")?.as_u64().unwrap_or(16) as u32, arg(&args, "height")?.as_u64().unwrap_or(16) as u32);
                if !(1..=128).contains(&w) || !(1..=128).contains(&h) {
                    return Err("sprite size must be 1..=128".into());
                }
                if p.list("sprites", ".json").contains(&name) {
                    return Err(format!("a sprite named '{name}' already exists"));
                }
                let ed = SpriteEditor::new(Sprite::new(&name, w, h));
                save_sprite(p, &ed, 4)?;
                let v = ed.view();
                self.sprite = Some(SpriteSession { name, editor: ed, fps: 4 });
                Ok(self.sprite_view(v))
            }
            "sprite_open" => {
                let name = str_arg(&args, "name")?.to_string();
                let (editor, fps) = load_sprite(self.project()?, &name)?;
                let v = editor.view();
                self.sprite = Some(SpriteSession { name, editor, fps });
                Ok(self.sprite_view(v))
            }
            "sprite_op" => {
                let op = serde_json::from_value(arg(&args, "op")?.clone()).map_err(|e| e.to_string())?;
                let s = self.sprite.as_mut().ok_or("no sprite open")?;
                s.editor.apply(op);
                let (v, fps) = (s.editor.view(), s.fps);
                save_sprite(self.project.as_ref().ok_or("no project")?, &self.sprite.as_ref().unwrap().editor, fps)?;
                Ok(self.sprite_view(v))
            }
            "sprite_set_fps" => {
                let s = self.sprite.as_mut().ok_or("no sprite open")?;
                s.fps = arg(&args, "fps")?.as_u64().unwrap_or(4).clamp(1, 60) as u32;
                save_sprite(self.project.as_ref().ok_or("no project")?, &self.sprite.as_ref().unwrap().editor, self.sprite.as_ref().unwrap().fps)?;
                let v = self.sprite.as_ref().unwrap().editor.view();
                Ok(self.sprite_view(v))
            }
            "sprite_import" => {
                let p = self.project()?;
                let name = valid_name(str_arg(&args, "name")?)?.to_string();
                let bytes = B64.decode(str_arg(&args, "png")?).map_err(|e| e.to_string())?;
                let fw = args.get("frame_width").and_then(Value::as_u64).map(|v| v as u32);
                let sprite = Sprite::from_png(&name, &bytes, fw).map_err(|e| e.to_string())?;
                let ed = SpriteEditor::new(sprite);
                save_sprite(p, &ed, 4)?;
                let v = ed.view();
                self.sprite = Some(SpriteSession { name, editor: ed, fps: 4 });
                Ok(self.sprite_view(v))
            }
            "sprite_delete" => {
                let name = str_arg(&args, "name")?;
                self.project()?.delete("sprites", name, &[".png", ".json"])?;
                if self.sprite.as_ref().is_some_and(|s| s.name == name) {
                    self.sprite = None;
                }
                Ok(json!(null))
            }
            "sprite_sheet" => {
                let p = self.project()?;
                let name = str_arg(&args, "name")?;
                let meta: Value = serde_json::from_str(&p.read_text("sprites", name, ".json")?).map_err(|e| e.to_string())?;
                Ok(json!({ "name": name, "width": meta["width"], "height": meta["height"], "frames": meta["frames"], "fps": meta["fps"], "png": B64.encode(p.read_bytes("sprites", name, ".png")?) }))
            }

            // ---- graphs & scripts
            "graph_load" => serde_json::from_str(&self.project()?.read_text("graphs", str_arg(&args, "name")?, ".graph.json")?).map_err(|e| e.to_string()),
            "graph_save" => {
                let g: rogue_graph::Graph = serde_json::from_value(arg(&args, "graph")?.clone()).map_err(|e| e.to_string())?;
                self.project()?.write_text("graphs", str_arg(&args, "name")?, ".graph.json", &serde_json::to_string_pretty(&g).unwrap())?;
                Ok(json!(null))
            }
            "graph_delete" => self.project()?.delete("graphs", str_arg(&args, "name")?, &[".graph.json"]).map(|_| json!(null)),
            "graph_compile" => {
                let g: rogue_graph::Graph = serde_json::from_value(arg(&args, "graph")?.clone()).map_err(|e| e.to_string())?;
                rogue_graph::compile(&g, &rogue_graph::Library::builtin()).map(|c| json!({ "code": c })).map_err(|e| e.to_string())
            }
            "script_load" => self.project()?.read_text("scripts", str_arg(&args, "name")?, ".lua").map(|t| json!({ "text": t })),
            "script_save" => self.project()?.write_text("scripts", str_arg(&args, "name")?, ".lua", str_arg(&args, "text")?).map(|_| json!(null)),
            "script_delete" => self.project()?.delete("scripts", str_arg(&args, "name")?, &[".lua"]).map(|_| json!(null)),

            // ---- objects (entities & tiles)
            "objects_save" => {
                let e = arg(&args, "entities")?.as_object().ok_or("entities must be an object")?;
                let t = arg(&args, "tiles")?.as_object().ok_or("tiles must be an object")?;
                for id in e.keys().chain(t.keys()) {
                    valid_name(id)?;
                }
                self.project()?.save_objects(e, t).map(|_| json!(null))
            }

            // ---- running the game
            "engine" => {
                let request = arg(&args, "request")?.clone();
                if request.get("type").and_then(Value::as_str) == Some("new_game") {
                    let project = match &self.project {
                        Some(p) => match p.to_mod() {
                            Ok(m) => Some(m),
                            Err(message) => return Ok(json!({ "type": "error", "message": message })),
                        },
                        None => None,
                    };
                    self.host.set_project(project);
                }
                let req = serde_json::from_value(request).map_err(|e| e.to_string())?;
                serde_json::to_value(self.host.request(req)).map_err(|e| e.to_string())
            }
            other => Err(format!("unknown command '{other}'")),
        }
    }

    fn sprite_view(&self, v: rogue_assets::SpriteView) -> Value {
        let fps = self.sprite.as_ref().map_or(4, |s| s.fps);
        let mut j = serde_json::to_value(v).unwrap();
        j["fps"] = json!(fps);
        j
    }
}
