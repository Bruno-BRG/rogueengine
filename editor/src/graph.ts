import { engine, type Graph, type GraphNode, type NodeDef } from "./api";

/** Minimal node-graph editor: drag nodes, click an output pin then an input pin to wire. */
export function mountGraph(root: HTMLElement, status: (s: string) => void) {
  root.innerHTML = `<div class="pane">
    <div class="graph" id="canvas"><svg class="wires" id="wires"></svg></div>
    <div class="side">
      <select id="palette"></select><button id="add">Add node</button>
      <button id="apply">Apply to running game</button>
      <button id="show">Show generated Lua</button>
      <pre id="lua" style="white-space:pre-wrap"></pre>
    </div></div>`;
  const $ = <T extends HTMLElement>(s: string) => root.querySelector<T>(s)!;
  const canvas = $("#canvas"), wires = root.querySelector<SVGSVGElement>("#wires")!;
  const graph: Graph = { nodes: [], edges: [] };
  let defs = new Map<string, NodeDef>();
  let pending: { node: number; pin: string } | null = null;
  let nextId = 1;

  engine({ type: "node_library" }).then((r) => {
    if (r.type !== "nodes") return;
    defs = new Map(r.nodes.map((n) => [n.type, n]));
    $("#palette").replaceChildren(...r.nodes.map((n) => {
      const o = document.createElement("option"); o.value = n.type; o.textContent = `${n.category} / ${n.title || n.type}`; return o;
    }));
  });

  function render() {
    canvas.querySelectorAll(".node").forEach((n) => n.remove());
    for (const n of graph.nodes) {
      const def = defs.get(n.type); if (!def) continue;
      const el = document.createElement("div");
      el.className = `node ${def.kind}`; el.dataset.id = String(n.id);
      el.style.left = `${n.x}px`; el.style.top = `${n.y}px`;
      const h = document.createElement("h4"); h.textContent = def.title || def.type; el.append(h);
      const pins = document.createElement("div"); pins.className = "pins";
      const ins = document.createElement("div"), outs = document.createElement("div");
      for (const p of def.inputs) {
        const d = document.createElement("div"); d.className = `pin ${p.ty}`; d.textContent = `● ${p.name}`; d.dataset.pin = `in:${p.name}`;
        d.onclick = () => connect(n.id, p.name, false); ins.append(d);
        if (p.ty !== "exec") {
          const inp = document.createElement("input");
          inp.value = String(n.props[p.name] ?? p.default ?? "");
          inp.onchange = () => { n.props[p.name] = p.ty === "number" ? Number(inp.value) : p.ty === "bool" ? inp.value === "true" : inp.value; };
          ins.append(inp);
        }
      }
      for (const p of def.outputs) {
        const d = document.createElement("div"); d.className = `pin ${p.ty}` + (pending?.node === n.id && pending.pin === p.name ? " sel" : "");
        d.textContent = `${p.name} ●`; d.dataset.pin = `out:${p.name}`; d.onclick = () => connect(n.id, p.name, true); outs.append(d);
      }
      pins.append(ins, outs); el.append(pins);
      h.onmousedown = (ev) => {
        const sx = ev.clientX - n.x, sy = ev.clientY - n.y;
        const move = (m: MouseEvent) => { n.x = m.clientX - sx; n.y = m.clientY - sy; el.style.left = `${n.x}px`; el.style.top = `${n.y}px`; drawWires(); };
        const up = () => { removeEventListener("mousemove", move); removeEventListener("mouseup", up); };
        addEventListener("mousemove", move); addEventListener("mouseup", up);
      };
      canvas.append(el);
    }
    drawWires();
  }

  function connect(node: number, pin: string, isOutput: boolean) {
    if (isOutput) { pending = { node, pin }; return render(); }
    if (!pending) return;
    graph.edges = graph.edges.filter((e) => !(e.to.node === node && e.to.pin === pin));
    graph.edges.push({ from: pending, to: { node, pin } });
    pending = null; render();
  }

  function pinPos(node: number, key: string): [number, number] | null {
    const el = canvas.querySelector<HTMLElement>(`.node[data-id="${node}"] [data-pin="${key}"]`);
    if (!el) return null;
    const c = canvas.getBoundingClientRect(), r = el.getBoundingClientRect();
    return [r.left - c.left + canvas.scrollLeft + (key.startsWith("out") ? r.width : 0), r.top - c.top + canvas.scrollTop + r.height / 2];
  }
  function drawWires() {
    wires.replaceChildren();
    for (const e of graph.edges) {
      const a = pinPos(e.from.node, `out:${e.from.pin}`), b = pinPos(e.to.node, `in:${e.to.pin}`);
      if (!a || !b) continue;
      const p = document.createElementNS("http://www.w3.org/2000/svg", "path");
      const dx = Math.max(40, Math.abs(b[0] - a[0]) / 2);
      p.setAttribute("d", `M${a[0]},${a[1]} C${a[0] + dx},${a[1]} ${b[0] - dx},${b[1]} ${b[0]},${b[1]}`);
      p.setAttribute("stroke", "#e0a030"); p.setAttribute("fill", "none"); p.setAttribute("stroke-width", "2");
      wires.append(p);
    }
  }

  $("#add").onclick = () => {
    const type = $<HTMLSelectElement>("#palette").value; if (!type) return;
    const n: GraphNode = { id: nextId++, type, props: {}, x: 20 + graph.nodes.length * 24, y: 20 + graph.nodes.length * 24 };
    graph.nodes.push(n); render();
  };
  const send = async (type: "apply_graph" | "compile_graph") => {
    const r = await engine({ type, graph });
    if (r.type === "error") status(r.message);
    else if (r.type === "lua") $("#lua").textContent = r.code;
    else status(type === "apply_graph" ? "Graph applied" : "");
  };
  $("#apply").onclick = () => send("apply_graph");
  $("#show").onclick = () => send("compile_graph");
}
