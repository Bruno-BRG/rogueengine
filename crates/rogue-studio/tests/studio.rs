use rogue_host::Host;
use rogue_studio::Studio;
use serde_json::{json, Value};

fn studio() -> (Studio, tempfile::TempDir) {
    (Studio::new(Host::spawn(None)), tempfile::tempdir().unwrap())
}

fn create(s: &mut Studio, dir: &tempfile::TempDir) -> Value {
    s.call("project_create", json!({"parent": dir.path(), "name": "My Test Game"})).unwrap()
}

#[test]
fn template_project_is_complete_and_playable() {
    let (mut s, dir) = studio();
    let info = create(&mut s, &dir);
    assert!(dir.path().join("my-test-game/game.reproj").is_file());
    assert_eq!(info["name"], "My Test Game");
    let sprites: Vec<_> = info["sprites"].as_array().unwrap().iter().map(|v| v["name"].as_str().unwrap().to_string()).collect();
    for needed in ["hero", "goblin", "floor", "wall", "stairs_down", "potion", "slime"] {
        assert!(sprites.contains(&needed.to_string()), "missing sprite {needed}");
    }
    assert_eq!(info["graphs"], json!(["loot_drops"]));
    assert!(info["objects"]["entities"]["slime"].is_object());

    // every sprite referenced by base + project data exists, so the template never shows broken art
    let mut refs = vec![];
    for src in [&info["base"]["entities"], &info["base"]["tiles"], &info["objects"]["entities"]] {
        for (_, def) in src.as_object().unwrap() {
            if let Some(sp) = def["sprite"].as_str() { refs.push(sp.to_string()); }
        }
    }
    for r in refs { assert!(sprites.contains(&r), "sprite '{r}' referenced but missing"); }

    let r = s.call("engine", json!({"request": {"type": "new_game", "seed": 9}})).unwrap();
    assert_eq!(r["type"], "state", "{r}");
    assert!(r["hud"].as_array().unwrap().len() >= 4);
    // slime from the project's data is registered (and the loot graph compiled and loaded)
    let r = s.call("engine", json!({"request": {"type": "exec", "code": "assert(rogue.definitions().slime); assert(#rogue.hooks.died >= 2)"}})).unwrap();
    assert_eq!(r["type"], "state", "{r}");
}

#[test]
fn sprite_editing_persists_across_reopen() {
    let (mut s, dir) = studio();
    create(&mut s, &dir);
    s.call("sprite_new", json!({"name": "gem", "width": 8, "height": 8})).unwrap();
    s.call("sprite_op", json!({"op": {"op": "pixel", "x": 2, "y": 3, "color": [255, 0, 0, 255]}})).unwrap();
    let v = s.call("sprite_op", json!({"op": {"op": "add_frame", "duplicate": true}})).unwrap();
    assert_eq!(v["frame_count"], 2);
    s.call("sprite_set_fps", json!({"fps": 10})).unwrap();
    let again = s.call("sprite_open", json!({"name": "gem"})).unwrap();
    assert_eq!((again["frame_count"].as_u64(), again["fps"].as_u64()), (Some(2), Some(10)));
    let px = &again["pixels"].as_array().unwrap();
    let i = (3 * 8 + 2) * 4;
    assert_eq!(px[i], 255);
    assert!(s.call("sprite_new", json!({"name": "gem", "width": 8, "height": 8})).is_err(), "duplicate name");
    let sheet = s.call("sprite_sheet", json!({"name": "gem"})).unwrap();
    assert!(sheet["png"].as_str().unwrap().len() > 20);
}

#[test]
fn names_cannot_escape_the_project_folder() {
    let (mut s, dir) = studio();
    create(&mut s, &dir);
    for bad in ["../evil", "a/b", "..", "", "x.lua", "a\\b"] {
        assert!(s.call("script_save", json!({"name": bad, "text": "x"})).is_err(), "{bad:?}");
        assert!(s.call("sprite_new", json!({"name": bad, "width": 4, "height": 4})).is_err(), "{bad:?}");
        assert!(s.call("graph_load", json!({"name": bad})).is_err(), "{bad:?}");
    }
    assert!(!dir.path().join("evil.lua").exists());
}

#[test]
fn scripts_objects_and_graph_errors_surface_in_new_game() {
    let (mut s, dir) = studio();
    create(&mut s, &dir);
    // custom monster via objects + custom rule via script
    s.call("objects_save", json!({"entities": {"bat": {"name": "Bat", "glyph": "b", "blocks": true, "stats": {"hp": 3, "max_hp": 3}, "tags": ["actor","monster"], "data": {"spawn": {"min_depth": 1, "weight": 1000}}}}, "tiles": {}})).unwrap();
    s.call("script_save", json!({"name": "zz_rules", "text": "rogue.rules.hud = function() return {{'Hello','World'}} end"})).unwrap();
    let r = s.call("engine", json!({"request": {"type": "new_game", "seed": 1}})).unwrap();
    assert_eq!(r["hud"], json!([["Hello", "World"]]));
    // a broken script is reported, not swallowed
    s.call("script_save", json!({"name": "zz_rules", "text": "this is not lua"})).unwrap();
    let r = s.call("engine", json!({"request": {"type": "new_game", "seed": 1}})).unwrap();
    assert_eq!(r["type"], "error");
    assert!(r["message"].as_str().unwrap().contains("zz_rules"), "{r}");
    // a cyclic graph is reported with the graph name
    s.call("script_delete", json!({"name": "zz_rules"})).unwrap();
    let cyc = json!({"nodes": [{"id":1,"type":"event.died"},{"id":2,"type":"flow.sequence"}], "edges": [
        {"from":{"node":1,"pin":"then"},"to":{"node":2,"pin":"exec"}},{"from":{"node":2,"pin":"a"},"to":{"node":2,"pin":"exec"}}]});
    s.call("graph_save", json!({"name": "bad", "graph": cyc})).unwrap();
    let r = s.call("engine", json!({"request": {"type": "new_game", "seed": 1}})).unwrap();
    assert!(r["message"].as_str().unwrap().contains("graph 'bad'"), "{r}");
}

#[test]
fn open_rejects_non_projects_and_reopens_created_ones() {
    let (mut s, dir) = studio();
    assert!(s.call("project_open", json!({"dir": dir.path()})).is_err());
    create(&mut s, &dir);
    s.call("project_close", json!({})).unwrap();
    assert!(s.call("project_info", json!({})).is_err());
    let info = s.call("project_open", json!({"dir": dir.path().join("my-test-game")})).unwrap();
    assert_eq!(info["name"], "My Test Game");
    assert!(s.call("project_create", json!({"parent": dir.path(), "name": "My Test Game"})).is_err(), "must not overwrite");
}
