//! Tiny local HTTP server: serves the built editor and `POST /api/<cmd>` (JSON in, JSON out).
//! Lets the exact same UI run in a browser (dev, tests, future web build) as in Tauri.
//! Binds to loopback only.
use serde_json::{json, Value};
use std::io::Read;
use std::path::{Path, PathBuf};
use std::sync::Mutex;

use crate::Studio;

pub fn serve(studio: Studio, addr: &str, static_dir: Option<PathBuf>) -> Result<(), String> {
    if !(addr.starts_with("127.0.0.1") || addr.starts_with("localhost")) {
        return Err("refusing to bind a non-loopback address: the API can read and write files".into());
    }
    let server = tiny_http::Server::http(addr).map_err(|e| e.to_string())?;
    let studio = Mutex::new(studio);
    for mut req in server.incoming_requests() {
        let url = req.url().split('?').next().unwrap_or("/").to_string();
        let resp = if let (Some(cmd), &tiny_http::Method::Post) = (url.strip_prefix("/api/"), req.method()) {
            let mut body = String::new();
            let _ = req.as_reader().take(64 << 20).read_to_string(&mut body);
            let args: Value = serde_json::from_str(&body).unwrap_or(json!({}));
            let out = match studio.lock().unwrap().call(cmd, args) {
                Ok(v) => json!({ "ok": v }),
                Err(e) => json!({ "err": e }),
            };
            tiny_http::Response::from_string(out.to_string()).with_header(header("Content-Type", "application/json")).boxed()
        } else if let Some(dir) = &static_dir {
            file_response(dir, &url)
        } else {
            tiny_http::Response::from_string("not found").with_status_code(404).boxed()
        };
        let _ = req.respond(resp);
    }
    Ok(())
}

fn header(k: &str, v: &str) -> tiny_http::Header {
    tiny_http::Header::from_bytes(k.as_bytes(), v.as_bytes()).unwrap()
}

fn file_response(dir: &Path, url: &str) -> tiny_http::ResponseBox {
    let rel = url.trim_start_matches('/');
    // Reject anything that could climb out of the static dir.
    let rel = if rel.is_empty() { "index.html" } else { rel };
    if rel.split('/').any(|p| p == ".." || p.contains('\\')) {
        return tiny_http::Response::from_string("bad path").with_status_code(400).boxed();
    }
    let path = dir.join(rel);
    let path = if path.is_file() { path } else { dir.join("index.html") };
    let mime = match path.extension().and_then(|e| e.to_str()) {
        Some("html") => "text/html; charset=utf-8",
        Some("js") => "text/javascript",
        Some("css") => "text/css",
        Some("png") => "image/png",
        Some("svg") => "image/svg+xml",
        _ => "application/octet-stream",
    };
    match std::fs::read(&path) {
        Ok(bytes) => tiny_http::Response::from_data(bytes).with_header(header("Content-Type", mime)).boxed(),
        Err(_) => tiny_http::Response::from_string("not found").with_status_code(404).boxed(),
    }
}
