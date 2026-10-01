#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use rogue_assets::{Sprite, SpriteEditor, SpriteOp, SpriteView};
use rogue_host::{Host, Request, Response};
use std::sync::Mutex;
use tauri::State;

struct AppState {
    host: Host,
    sprite: Mutex<Option<SpriteEditor>>,
}

/// Single entry point to the engine (game, console, graphs, mods). See `rogue-host`.
#[tauri::command(async)]
fn engine_request(state: State<AppState>, request: Request) -> Response {
    state.host.request(request)
}

#[tauri::command]
fn sprite_new(state: State<AppState>, name: String, width: u32, height: u32) -> Result<SpriteView, String> {
    if !(1..=256).contains(&width) || !(1..=256).contains(&height) {
        return Err("sprite size must be 1..=256".into());
    }
    let ed = SpriteEditor::new(Sprite::new(&name, width, height));
    let view = ed.view();
    *state.sprite.lock().unwrap() = Some(ed);
    Ok(view)
}

#[tauri::command]
fn sprite_apply(state: State<AppState>, op: SpriteOp) -> Result<SpriteView, String> {
    let mut guard = state.sprite.lock().unwrap();
    let ed = guard.as_mut().ok_or("no sprite open")?;
    ed.apply(op);
    Ok(ed.view())
}

#[tauri::command]
fn sprite_export_png(state: State<AppState>) -> Result<Vec<u8>, String> {
    let guard = state.sprite.lock().unwrap();
    guard.as_ref().ok_or("no sprite open")?.sprite.sheet_png().map_err(|e| e.to_string())
}

fn main() {
    let mods = std::env::var_os("ROGUE_MODS").map(Into::into).or_else(|| Some(std::path::PathBuf::from("mods")));
    tauri::Builder::default()
        .manage(AppState { host: Host::spawn(mods), sprite: Mutex::new(None) })
        .invoke_handler(tauri::generate_handler![engine_request, sprite_new, sprite_apply, sprite_export_png])
        .run(tauri::generate_context!())
        .expect("error while running RogueEngine");
}
