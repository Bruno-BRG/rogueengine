#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use rogue_studio::Studio;
use serde_json::Value;
use std::sync::Mutex;
use tauri::State;

/// The whole editor backend is one command: `api(cmd, args)`.
/// The same API is served over HTTP by `rogue-server` for browser dev/testing.
#[tauri::command(async)]
fn api(state: State<Mutex<Studio>>, cmd: String, args: Option<Value>) -> Result<Value, String> {
    state.lock().map_err(|_| "studio lock poisoned".to_string())?.call(&cmd, args.unwrap_or(Value::Null))
}

fn main() {
    tauri::Builder::default()
        .manage(Mutex::new(Studio::new(rogue_host::Host::spawn(None))))
        .invoke_handler(tauri::generate_handler![api])
        .run(tauri::generate_context!())
        .expect("error while running RogueEngine");
}
