use rogue_host::{Host, Request, Response};
use serde_json::json;

fn state(r: Response) -> Box<rogue_host::Snapshot> {
    match r {
        Response::State(s) => s,
        other => panic!("expected state, got {other:?}"),
    }
}

#[test]
fn full_game_loop_over_the_json_protocol() {
    let mods = concat!(env!("CARGO_MANIFEST_DIR"), "/../../mods");
    let host = Host::spawn(Some(mods.into()));
    assert!(matches!(host.request(Request::Act { action: "wait".into(), arg: json!({}) }), Response::Error { .. }));

    let s = state(host.request(serde_json::from_value(json!({"type":"new_game","seed":5})).unwrap()));
    assert_eq!((s.width, s.height), (60, 36));
    assert_eq!(s.tiles.len(), 60 * 36);
    assert!(s.player.is_some() && s.hp > 0);
    assert!(s.fog.contains(&2), "player must see something");

    // deterministic: same seed → same level
    let s2 = state(host.request(Request::NewGame { seed: 5, width: 60, height: 36 }));
    assert_eq!(s.tiles, s2.tiles);

    // the player can take many turns without errors
    for i in 0..40 {
        let (dx, dy) = [(1, 0), (0, 1), (-1, 0), (0, -1)][i % 4];
        let r = host.request(Request::Act { action: "move".into(), arg: json!({"dx": dx, "dy": dy}) });
        assert!(!matches!(r, Response::Error { .. }), "{r:?}");
    }

    match host.request(Request::ListMods) {
        Response::Mods { mods } => assert!(mods.iter().any(|m| m.id == "base") && mods.iter().any(|m| m.id == "example-poison")),
        o => panic!("{o:?}"),
    }
    match host.request(Request::Exec { code: "error('nope')".into() }) {
        Response::Error { message } => assert!(message.contains("nope")),
        o => panic!("{o:?}"),
    }
}

#[test]
fn graph_apply_changes_live_behaviour() {
    let host = Host::spawn(None);
    host.request(Request::NewGame { seed: 1, width: 40, height: 24 });
    let graph = json!({
        "nodes": [
            {"id": 1, "type": "event.moved"},
            {"id": 2, "type": "action.log", "props": {"text": "graph says hi"}}
        ],
        "edges": [{"from": {"node": 1, "pin": "then"}, "to": {"node": 2, "pin": "exec"}}]
    });
    match host.request(Request::CompileGraph { graph: serde_json::from_value(graph.clone()).unwrap() }) {
        Response::Lua { code } => assert!(code.contains("graph says hi")),
        o => panic!("{o:?}"),
    }
    assert!(matches!(host.request(Request::ApplyGraph { graph: serde_json::from_value(graph).unwrap() }), Response::Ok));
    let mut saw = false;
    for dir in [(1, 0), (-1, 0), (0, 1), (0, -1)] {
        let s = state(host.request(Request::Act { action: "move".into(), arg: json!({"dx": dir.0, "dy": dir.1}) }));
        saw |= s.log.iter().any(|l| l == "graph says hi");
    }
    assert!(saw, "graph hook should fire when the player moves");
}

#[test]
fn wire_format_matches_the_frontend_types() {
    let host = Host::spawn(None);
    let v = serde_json::to_value(host.request(Request::NewGame { seed: 2, width: 30, height: 20 })).unwrap();
    assert_eq!(v["type"], "state");
    assert_eq!(v["width"], 30);
    assert!(v["tiles"].is_array() && v["tile_defs"][0]["id"] == "void");
    assert!(v["player"]["id"].is_u64());
    let err = serde_json::to_value(host.request(Request::Exec { code: "(".into() })).unwrap();
    assert_eq!(err["type"], "error");
}

#[test]
fn console_eval_returns_values_and_runs_statements() {
    let host = Host::spawn(None);
    host.request(Request::NewGame { seed: 4, width: 40, height: 24 });
    let v = |code: &str| match host.request(Request::Eval { code: code.into() }) {
        Response::Value { value } => value,
        o => panic!("{o:?}"),
    };
    assert_eq!(v("1 + 2"), json!(3));
    assert_eq!(v("rogue.find_tagged('player')[1] ~= nil"), json!(true));
    assert_eq!(v("rogue.get(rogue.find_tagged('player')[1]).stats.max_hp"), json!(30));
    assert_eq!(v("rogue.set_data(rogue.find_tagged('player')[1], 'x', 5)"), json!(null), "statements return null");
    assert_eq!(v("rogue.get_data(rogue.find_tagged('player')[1], 'x')"), json!(5));
    assert!(v("print").as_str().unwrap().starts_with("function"));
    assert!(matches!(host.request(Request::Eval { code: "error('boom')".into() }), Response::Error { message } if message.contains("boom")));
    assert!(matches!(host.request(Request::Snapshot), Response::State(_)));
}
