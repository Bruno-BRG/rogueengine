//! Visual coding: node graphs compile to plain Lua, so visual and text scripting
//! are the same thing underneath (a graph can be "ejected" to editable Lua).
//!
//! Node types are *data* (`NodeDef`, with a Lua template), so mods can add nodes
//! without touching Rust. Template syntax: `${pin}` is replaced by
//!  - the Lua expression of the connected data pin, or the node's literal prop/pin default;
//!  - for exec output pins, the code of the chain connected to that pin.
use serde::{Deserialize, Serialize};
use serde_json::{Map, Value};
use std::collections::{HashMap, HashSet};

pub const BUILTIN_NODES: &str = include_str!("builtin_nodes.json");

#[derive(Debug, thiserror::Error)]
pub enum GraphError {
    #[error("unknown node type '{0}'")]
    UnknownType(String),
    #[error("unknown node id {0}")]
    UnknownNode(u32),
    #[error("node {node}: no pin '{pin}'")]
    UnknownPin { node: u32, pin: String },
    #[error("data cycle through node {0}")]
    DataCycle(u32),
    #[error("exec loop through node {0}")]
    ExecLoop(u32),
    #[error("bad node definition: {0}")]
    BadDef(String),
}

#[derive(Clone, Copy, Debug, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum Kind {
    Event,
    Flow,
    Data,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Pin {
    pub name: String,
    /// "exec", "number", "string", "bool", "entity", "any" — used by the editor for wire colours/validation.
    #[serde(default = "any")]
    pub ty: String,
    #[serde(default)]
    pub default: Option<Value>,
    /// Event nodes: Lua expression this output pin exposes (e.g. `ev.id`).
    #[serde(default)]
    pub expr: Option<String>,
}
fn any() -> String {
    "any".into()
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct NodeDef {
    #[serde(rename = "type")]
    pub ty: String,
    pub kind: Kind,
    #[serde(default)]
    pub title: String,
    #[serde(default)]
    pub category: String,
    #[serde(default)]
    pub inputs: Vec<Pin>,
    #[serde(default)]
    pub outputs: Vec<Pin>,
    pub template: String,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Node {
    pub id: u32,
    #[serde(rename = "type")]
    pub ty: String,
    #[serde(default)]
    pub props: Map<String, Value>,
    /// Editor position; ignored by the compiler.
    #[serde(default)]
    pub x: f32,
    #[serde(default)]
    pub y: f32,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Endpoint {
    pub node: u32,
    pub pin: String,
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct Edge {
    pub from: Endpoint,
    pub to: Endpoint,
}

#[derive(Clone, Debug, Default, Serialize, Deserialize)]
pub struct Graph {
    pub nodes: Vec<Node>,
    pub edges: Vec<Edge>,
}

#[derive(Clone, Debug)]
pub struct Library {
    defs: HashMap<String, NodeDef>,
}

impl Library {
    pub fn builtin() -> Self {
        let mut l = Library { defs: HashMap::new() };
        l.add_json(BUILTIN_NODES).expect("builtin nodes are valid");
        l
    }
    /// Add or replace node definitions (a JSON array of `NodeDef`). This is the mod hook.
    pub fn add_json(&mut self, json: &str) -> Result<(), GraphError> {
        let defs: Vec<NodeDef> = serde_json::from_str(json).map_err(|e| GraphError::BadDef(e.to_string()))?;
        for d in defs {
            self.defs.insert(d.ty.clone(), d);
        }
        Ok(())
    }
    pub fn get(&self, ty: &str) -> Option<&NodeDef> {
        self.defs.get(ty)
    }
    pub fn all(&self) -> Vec<&NodeDef> {
        let mut v: Vec<_> = self.defs.values().collect();
        v.sort_by(|a, b| (&a.category, &a.ty).cmp(&(&b.category, &b.ty)));
        v
    }
}

/// Lua string literal with every special character escaped, so graph text can never break out of the string.
pub fn lua_string(s: &str) -> String {
    let mut out = String::from("\"");
    for c in s.chars() {
        match c {
            '\\' => out.push_str("\\\\"),
            '"' => out.push_str("\\\""),
            '\n' => out.push_str("\\n"),
            '\r' => out.push_str("\\r"),
            '\0' => out.push_str("\\0"),
            c if (c as u32) < 32 => out.push_str(&format!("\\{}", c as u32)),
            c => out.push(c),
        }
    }
    out.push('"');
    out
}

fn literal(v: &Value) -> String {
    match v {
        Value::Null => "nil".into(),
        Value::Bool(b) => b.to_string(),
        Value::Number(n) => match n.as_f64() {
            Some(f) if f.is_finite() => n.to_string(),
            _ => "0".into(),
        },
        Value::String(s) => lua_string(s),
        // arrays/objects as props are unsupported for now
        _ => "nil".into(),
    }
}

/// Single-pass `${name}` substitution: substituted text is never re-scanned,
/// so user strings containing `${...}` stay literal.
fn fill(template: &str, vals: &HashMap<String, String>) -> String {
    let mut out = String::with_capacity(template.len());
    let mut rest = template;
    while let Some(i) = rest.find("${") {
        out.push_str(&rest[..i]);
        let after = &rest[i + 2..];
        match after.find('}') {
            Some(j) => {
                match vals.get(&after[..j]) {
                    Some(v) => out.push_str(v),
                    None => out.push_str(&rest[i..i + 2 + j + 1]),
                }
                rest = &after[j + 1..];
            }
            None => {
                out.push_str(&rest[i..]);
                rest = "";
            }
        }
    }
    out.push_str(rest);
    out
}

struct Compiler<'a> {
    g: &'a Graph,
    lib: &'a Library,
    nodes: HashMap<u32, &'a Node>,
}

pub fn compile(graph: &Graph, lib: &Library) -> Result<String, GraphError> {
    let c = Compiler { g: graph, lib, nodes: graph.nodes.iter().map(|n| (n.id, n)).collect() };
    let mut out = String::from("-- generated by RogueEngine visual graph; edit the graph, not this file\n");
    let mut events: Vec<&Node> = graph.nodes.iter().filter(|n| lib.get(&n.ty).is_some_and(|d| d.kind == Kind::Event)).collect();
    events.sort_by_key(|n| n.id);
    for n in graph.nodes.iter() {
        if lib.get(&n.ty).is_none() {
            return Err(GraphError::UnknownType(n.ty.clone()));
        }
    }
    for n in events {
        out.push_str(&c.node_code(n, &mut Vec::new())?);
        out.push('\n');
    }
    Ok(out)
}

impl Compiler<'_> {
    fn def(&self, n: &Node) -> Result<&NodeDef, GraphError> {
        self.lib.get(&n.ty).ok_or_else(|| GraphError::UnknownType(n.ty.clone()))
    }

    /// Code for a node (event/flow), with `${exec_out}` filled by the chains behind each exec output.
    fn node_code(&self, n: &Node, stack: &mut Vec<u32>) -> Result<String, GraphError> {
        if stack.contains(&n.id) {
            return Err(GraphError::ExecLoop(n.id));
        }
        stack.push(n.id);
        let def = self.def(n)?;
        let mut vals: HashMap<String, String> = HashMap::new();
        for pin in def.outputs.iter().filter(|p| p.ty == "exec") {
            vals.insert(pin.name.clone(), self.chain(n.id, &pin.name, stack)?);
        }
        for pin in def.inputs.iter().filter(|p| p.ty != "exec") {
            vals.insert(pin.name.clone(), self.input_expr(n, pin, &mut Vec::new())?);
        }
        let code = fill(&def.template, &vals);
        stack.pop();
        Ok(code)
    }

    fn chain(&self, from: u32, pin: &str, stack: &mut Vec<u32>) -> Result<String, GraphError> {
        let mut parts = Vec::new();
        for e in self.g.edges.iter().filter(|e| e.from.node == from && e.from.pin == pin) {
            let next = self.nodes.get(&e.to.node).ok_or(GraphError::UnknownNode(e.to.node))?;
            parts.push(self.node_code(next, stack)?);
        }
        Ok(parts.join("\n"))
    }

    /// Expression feeding input `pin` of `n`: wire, else literal prop, else pin default, else nil.
    fn input_expr(&self, n: &Node, pin: &Pin, visiting: &mut Vec<u32>) -> Result<String, GraphError> {
        if let Some(e) = self.g.edges.iter().find(|e| e.to.node == n.id && e.to.pin == pin.name) {
            let src = self.nodes.get(&e.from.node).ok_or(GraphError::UnknownNode(e.from.node))?;
            return self.output_expr(src, &e.from.pin, visiting);
        }
        if let Some(v) = n.props.get(&pin.name).or(pin.default.as_ref()) {
            return Ok(literal(v));
        }
        Ok("nil".into())
    }

    fn output_expr(&self, n: &Node, pin_name: &str, visiting: &mut Vec<u32>) -> Result<String, GraphError> {
        let def = self.def(n)?;
        let pin = def.outputs.iter().find(|p| p.name == pin_name).ok_or_else(|| GraphError::UnknownPin { node: n.id, pin: pin_name.into() })?;
        if let Some(expr) = &pin.expr {
            return Ok(expr.clone());
        }
        if def.kind != Kind::Data {
            return Err(GraphError::UnknownPin { node: n.id, pin: pin_name.into() });
        }
        if visiting.contains(&n.id) {
            return Err(GraphError::DataCycle(n.id));
        }
        visiting.push(n.id);
        let mut vals = HashMap::new();
        for p in &def.inputs {
            vals.insert(p.name.clone(), self.input_expr(n, p, visiting)?);
        }
        let code = fill(&def.template, &vals);
        visiting.pop();
        Ok(format!("({code})"))
    }
}
