use rogue_graph::*;
use rogue_script::Engine;
use serde_json::json;

fn node(id: u32, ty: &str, props: serde_json::Value) -> Node {
    Node { id, ty: ty.into(), props: props.as_object().cloned().unwrap_or_default(), x: 0.0, y: 0.0 }
}
fn edge(a: u32, ap: &str, b: u32, bp: &str) -> Edge {
    Edge { from: Endpoint { node: a, pin: ap.into() }, to: Endpoint { node: b, pin: bp.into() } }
}

#[test]
fn builtin_library_parses() {
    assert!(Library::builtin().all().len() > 20);
}

#[test]
fn graph_compiles_and_runs_in_engine() {
    // On Died -> Branch(chance(1.0)) -> true: Spawn potion at the death... (log marker)
    let g = Graph {
        nodes: vec![
            node(1, "event.died", json!({})),
            node(2, "flow.branch", json!({})),
            node(3, "logic.chance", json!({"p": 1.0})),
            node(4, "action.log", json!({"text": "it dropped \"loot\"\nline2"})),
            node(5, "action.log", json!({"text": "nothing"})),
        ],
        edges: vec![edge(1, "then", 2, "exec"), edge(3, "out", 2, "condition"), edge(2, "true", 4, "exec"), edge(2, "false", 5, "exec")],
    };
    let lua = compile(&g, &Library::builtin()).unwrap();
    let e = Engine::new(10, 10, 3).unwrap();
    e.exec(r#"for y=0,9 do for x=0,9 do rogue.set_tile(x,y,"floor") end end"#).unwrap();
    e.exec(&lua).expect(&lua);
    e.exec(r#"G = rogue.spawn("goblin", 2, 2); rogue.damage(nil, G, 999)"#).unwrap();
    e.dispatch_events().unwrap();
    let log = e.world.borrow().log.clone();
    assert!(log.iter().any(|l| l == "it dropped \"loot\"\nline2"), "{log:?}");
    assert!(!log.iter().any(|l| l == "nothing"));
}

#[test]
fn event_outputs_wire_into_data_nodes() {
    let g = Graph {
        nodes: vec![
            node(1, "event.attacked", json!({})),
            node(2, "action.heal", json!({})),
            node(3, "math.mul", json!({"b": 2})),
        ],
        edges: vec![edge(1, "then", 2, "exec"), edge(1, "target", 2, "target"), edge(1, "damage", 3, "a"), edge(3, "out", 2, "amount")],
    };
    let lua = compile(&g, &Library::builtin()).unwrap();
    assert!(lua.contains("rogue.heal(ev.target, (ev.damage * 2))"), "{lua}");
}

#[test]
fn string_props_cannot_inject_code() {
    let g = Graph {
        nodes: vec![node(1, "event.died", json!({})), node(2, "action.log", json!({"text": "\"); os.exit() --"}))],
        edges: vec![edge(1, "then", 2, "exec")],
    };
    let lua = compile(&g, &Library::builtin()).unwrap();
    assert!(lua.contains(r#"rogue.log("\"); os.exit() --")"#), "{lua}");
}

#[test]
fn cycles_and_unknowns_are_errors() {
    let lib = Library::builtin();
    let g = Graph {
        nodes: vec![node(1, "event.died", json!({})), node(2, "flow.sequence", json!({}))],
        edges: vec![edge(1, "then", 2, "exec"), edge(2, "a", 2, "exec")],
    };
    assert!(matches!(compile(&g, &lib), Err(GraphError::ExecLoop(2))));
    let g = Graph { nodes: vec![node(1, "nope", json!({}))], edges: vec![] };
    assert!(matches!(compile(&g, &lib), Err(GraphError::UnknownType(_))));
    let g = Graph {
        nodes: vec![node(1, "event.died", json!({})), node(2, "action.log", json!({})), node(3, "math.add", json!({}))],
        edges: vec![edge(1, "then", 2, "exec"), edge(3, "out", 3, "a"), edge(3, "out", 2, "text")],
    };
    assert!(matches!(compile(&g, &lib), Err(GraphError::DataCycle(3))));
}

#[test]
fn mods_can_add_node_types() {
    let mut lib = Library::builtin();
    lib.add_json(r#"[{"type":"my.shout","kind":"flow","inputs":[{"name":"exec","ty":"exec"}],"outputs":[{"name":"then","ty":"exec"}],"template":"rogue.log('AAA')\n${then}"}]"#).unwrap();
    let g = Graph { nodes: vec![node(1, "event.died", json!({})), node(2, "my.shout", json!({}))], edges: vec![edge(1, "then", 2, "exec")] };
    assert!(compile(&g, &lib).unwrap().contains("AAA"));
}

#[test]
fn placeholder_text_in_strings_is_not_reexpanded() {
    let g = Graph {
        nodes: vec![
            node(1, "event.died", json!({})),
            node(2, "flow.branch", json!({"condition": true})),
            node(3, "action.log", json!({"text": "${condition} ${true}"})),
        ],
        edges: vec![edge(1, "then", 2, "exec"), edge(2, "true", 3, "exec")],
    };
    let lua = compile(&g, &Library::builtin()).unwrap();
    assert!(lua.contains(r#"rogue.log("${condition} ${true}")"#), "{lua}");
}
