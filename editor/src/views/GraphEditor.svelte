<script lang="ts">
  import { app, debounced, fail, playNow, refreshProject, toast } from "../lib/app.svelte";
  import { call, engine } from "../lib/api";
  import type { Graph, GraphNode, NodeDef, Pin } from "../lib/types";
  import Icon from "../ui/Icon.svelte";
  import NodePalette from "../ui/NodePalette.svelte";
  import Prompt from "../ui/Prompt.svelte";
  import Confirm from "../ui/Confirm.svelte";

  // ---- geometry ---------------------------------------------------------------
  const W = 244, HEAD = 34, ROW = 27, PAD = 8;
  const rowsOf = (d: NodeDef) => Math.max(d.inputs.length, d.outputs.length, 1);
  const heightOf = (d: NodeDef) => HEAD + PAD * 2 + rowsOf(d) * ROW;
  const pinY = (n: GraphNode, i: number) => n.y + HEAD + PAD + i * ROW + ROW / 2;
  const COLORS: Record<string, string> = { exec: "#e8ebf3", number: "#4ade80", string: "#f472b6", bool: "#f87171", entity: "#60a5fa", any: "#a1a1aa" };
  const col = (ty: string) => COLORS[ty] ?? COLORS.any;

  // ---- state --------------------------------------------------------------------
  let defs = $state<NodeDef[]>([]);
  const defMap = $derived(new Map(defs.map((d) => [d.type, d])));
  let current = $state("");
  let graph = $state<Graph>({ nodes: [], edges: [] });
  let sel = $state<{ node?: number; edge?: number }>({});
  let pan = $state({ x: 80, y: 60 });
  let k = $state(1);
  let link = $state<{ node: number; pin: Pin; isOutput: boolean; x: number; y: number } | null>(null);
  let palette = $state<{ x: number; y: number; wx: number; wy: number; from: { node: number; pin: Pin; isOutput: boolean } | null } | null>(null);
  let showLua = $state(true);
  let compiled = $state<{ ok: boolean; code: string; error: string }>({ ok: true, code: "", error: "" });
  let newPrompt = $state(false);
  let confirmDelete = $state(false);
  let surface = $state<HTMLDivElement>();
  let loaded = "";
  let needsFit = false;

  const graphs = $derived(app.project?.graphs ?? []);

  engine({ type: "node_library" }).then((r) => { if (r.type === "nodes") defs = r.nodes; }).catch(fail);

  // ---- history ---------------------------------------------------------------
  let hist: string[] = [];
  let hi = -1;
  function commit() {
    const s = JSON.stringify(graph);
    if (hist[hi] === s) return;
    hist = hist.slice(0, hi + 1); hist.push(s); hi = hist.length - 1;
    if (hist.length > 200) { hist.shift(); hi--; }
    changed();
  }
  function undo(dir: -1 | 1) {
    const n = hi + dir;
    if (n < 0 || n >= hist.length) return;
    hi = n; graph = JSON.parse(hist[hi]); sel = {}; changed();
  }

  // ---- load / save / compile ---------------------------------------------------
  const persist = debounced(async () => {
    if (!current) return;
    await call("graph_save", { name: current, graph });
  }, 450);
  const recompile = debounced(async () => {
    try {
      const r = await call<{ code: string }>("graph_compile", { graph });
      compiled = { ok: true, code: r.code, error: "" };
    } catch (e) {
      compiled = { ok: false, code: compiled.code, error: e instanceof Error ? e.message : String(e) };
    }
  }, 200);
  function changed() { persist(); recompile(); }

  async function load(name: string) {
    if (loaded && loaded !== name) await persist.flush();
    try {
      const g = await call<Graph>("graph_load", { name });
      graph = g; current = name; loaded = name; app.selected.graph = name;
      hist = [JSON.stringify(g)]; hi = 0; sel = {}; link = null;
      recompile();
      needsFit = true;
    } catch (e) { fail(e); }
  }
  $effect(() => {
    if (!graphs.length) { current = ""; loaded = ""; graph = { nodes: [], edges: [] }; return; }
    if (!graphs.includes(current)) load(graphs.includes(app.selected.graph) ? app.selected.graph : graphs[0]);
  });

  async function create(name: string) {
    newPrompt = false;
    try {
      await call("graph_save", { name, graph: { nodes: [], edges: [] } });
      await refreshProject();
      await load(name);
    } catch (e) { fail(e); }
  }
  async function remove() {
    const name = current;
    try { await call("graph_delete", { name }); current = ""; loaded = ""; await refreshProject(); toast(`Deleted ${name}`); } catch (e) { fail(e); }
  }

  // ---- coordinates -------------------------------------------------------------
  function toWorld(cx: number, cy: number) {
    const r = surface!.getBoundingClientRect();
    return { x: (cx - r.left - pan.x) / k, y: (cy - r.top - pan.y) / k };
  }
  function fit() {
    if (!surface || !graph.nodes.length) { pan = { x: 80, y: 60 }; k = 1; return; }
    let [x0, y0, x1, y1] = [1e9, 1e9, -1e9, -1e9];
    for (const n of graph.nodes) {
      const d = defMap.get(n.type); const h = d ? heightOf(d) : 80;
      x0 = Math.min(x0, n.x); y0 = Math.min(y0, n.y); x1 = Math.max(x1, n.x + W); y1 = Math.max(y1, n.y + h);
    }
    const r = surface.getBoundingClientRect();
    k = Math.max(0.35, Math.min(1.2, Math.min((r.width - 100) / (x1 - x0), (r.height - 100) / (y1 - y0))));
    pan = { x: (r.width - (x1 - x0) * k) / 2 - x0 * k, y: (r.height - (y1 - y0) * k) / 2 - y0 * k };
  }

  // The tab is `display:none` until opened, so we can only measure (and fit) once it is visible.
  $effect(() => {
    if (app.view === "graph" && needsFit && defs.length && surface && surface.clientWidth > 0) {
      needsFit = false;
      fit();
    }
  });

  function wheel(node: HTMLElement) {
    const h = (e: WheelEvent) => {
      e.preventDefault();
      const r = node.getBoundingClientRect();
      const nk = Math.max(0.25, Math.min(2, k * Math.exp(-e.deltaY * 0.0015)));
      const mx = e.clientX - r.left, my = e.clientY - r.top;
      pan = { x: mx - ((mx - pan.x) / k) * nk, y: my - ((my - pan.y) / k) * nk };
      k = nk;
    };
    node.addEventListener("wheel", h, { passive: false });
    return { destroy: () => node.removeEventListener("wheel", h) };
  }

  // ---- node & edge operations -----------------------------------------------------
  const nextId = () => graph.nodes.reduce((m, n) => Math.max(m, n.id), 0) + 1;
  function addNode(d: NodeDef, x: number, y: number, from: NonNullable<typeof palette>["from"] = null) {
    const n: GraphNode = { id: nextId(), type: d.type, props: {}, x: Math.round(x / 10) * 10, y: Math.round(y / 10) * 10 };
    graph.nodes.push(n);
    if (from) {
      if (from.isOutput) {
        const target = d.inputs.find((p) => (p.ty === "exec") === (from.pin.ty === "exec") && (p.ty === "exec" || p.ty === from.pin.ty)) ?? d.inputs.find((p) => (p.ty === "exec") === (from.pin.ty === "exec"));
        if (target) connect(from.node, from.pin.name, n.id, target.name);
      } else {
        const src = d.outputs.find((p) => (p.ty === "exec") === (from.pin.ty === "exec") && (p.ty === "exec" || p.ty === from.pin.ty)) ?? d.outputs.find((p) => (p.ty === "exec") === (from.pin.ty === "exec"));
        if (src) connect(n.id, src.name, from.node, from.pin.name);
      }
    }
    sel = { node: n.id };
    commit();
  }
  function pinOf(nodeId: number, name: string, out: boolean): Pin | undefined {
    const n = graph.nodes.find((x) => x.id === nodeId); const d = n && defMap.get(n.type);
    return (out ? d?.outputs : d?.inputs)?.find((p) => p.name === name);
  }
  function connect(fn: number, fp: string, tn: number, tp: string) {
    if (fn === tn) return;
    const a = pinOf(fn, fp, true), b = pinOf(tn, tp, false);
    if (!a || !b || (a.ty === "exec") !== (b.ty === "exec")) return;
    // an input takes one wire; an exec output leads to one place
    graph.edges = graph.edges.filter((e) => !(e.to.node === tn && e.to.pin === tp) && !(a.ty === "exec" && e.from.node === fn && e.from.pin === fp));
    graph.edges.push({ from: { node: fn, pin: fp }, to: { node: tn, pin: tp } });
  }
  function removeNode(id: number) {
    graph.nodes = graph.nodes.filter((n) => n.id !== id);
    graph.edges = graph.edges.filter((e) => e.from.node !== id && e.to.node !== id);
    sel = {}; commit();
  }
  function duplicate(id: number) {
    const n = graph.nodes.find((x) => x.id === id); const d = n && defMap.get(n.type);
    if (n && d) addNode(d, n.x + 30, n.y + 30);
    const last = graph.nodes[graph.nodes.length - 1];
    if (n && last) last.props = JSON.parse(JSON.stringify(n.props));
  }

  // ---- pointer interaction ------------------------------------------------------
  let drag: null | { kind: "pan"; sx: number; sy: number; px: number; py: number } | { kind: "node"; id: number; dx: number; dy: number; moved: boolean } = null;

  function bgDown(e: PointerEvent) {
    if ((e.target as HTMLElement).closest(".node, .pal, button, input")) return;
    surface!.setPointerCapture(e.pointerId);
    sel = {};
    drag = { kind: "pan", sx: e.clientX, sy: e.clientY, px: pan.x, py: pan.y };
  }
  function nodeDown(e: PointerEvent, n: GraphNode) {
    if ((e.target as HTMLElement).closest("button, input")) return;
    e.stopPropagation();
    surface!.setPointerCapture(e.pointerId);
    const w = toWorld(e.clientX, e.clientY);
    sel = { node: n.id };
    drag = { kind: "node", id: n.id, dx: w.x - n.x, dy: w.y - n.y, moved: false };
  }
  function pinDown(e: PointerEvent, n: GraphNode, pin: Pin, isOutput: boolean) {
    e.stopPropagation(); e.preventDefault();
    surface!.setPointerCapture(e.pointerId);
    const w = toWorld(e.clientX, e.clientY);
    // grabbing a connected input detaches the wire so it can be re-plugged elsewhere
    if (!isOutput) {
      const ex = graph.edges.find((x) => x.to.node === n.id && x.to.pin === pin.name);
      if (ex) {
        const sp = pinOf(ex.from.node, ex.from.pin, true)!;
        graph.edges = graph.edges.filter((x) => x !== ex);
        link = { node: ex.from.node, pin: sp, isOutput: true, x: w.x, y: w.y };
        return;
      }
    }
    link = { node: n.id, pin, isOutput, x: w.x, y: w.y };
  }
  function move(e: PointerEvent) {
    if (link) { const w = toWorld(e.clientX, e.clientY); link.x = w.x; link.y = w.y; return; }
    if (!drag) return;
    const d = drag;
    if (d.kind === "pan") pan = { x: d.px + e.clientX - d.sx, y: d.py + e.clientY - d.sy };
    else {
      const w = toWorld(e.clientX, e.clientY);
      const n = graph.nodes.find((x) => x.id === d.id);
      if (!n) return;
      n.x = Math.round((w.x - d.dx) / 10) * 10; n.y = Math.round((w.y - d.dy) / 10) * 10; d.moved = true;
    }
  }
  function up(e: PointerEvent) {
    if (link) {
      const hit = (document.elementFromPoint(e.clientX, e.clientY) as HTMLElement | null)?.closest<HTMLElement>("[data-pin]");
      const l = link; link = null;
      if (hit) {
        const [nid, name, dir] = [Number(hit.dataset.node), hit.dataset.pin!, hit.dataset.dir];
        if (dir === "out" && !l.isOutput) connect(nid, name, l.node, l.pin.name);
        else if (dir === "in" && l.isOutput) connect(l.node, l.pin.name, nid, name);
      } else {
        const r = surface!.getBoundingClientRect(), w = toWorld(e.clientX, e.clientY);
        palette = { x: e.clientX, y: e.clientY, wx: w.x, wy: w.y, from: { node: l.node, pin: l.pin, isOutput: l.isOutput } };
        void r;
      }
      commit();
    } else if (drag?.kind === "node" && drag.moved) commit();
    drag = null;
  }
  function openPalette(e: MouseEvent) {
    if ((e.target as HTMLElement).closest(".node")) return;
    e.preventDefault();
    const w = toWorld(e.clientX, e.clientY);
    palette = { x: e.clientX, y: e.clientY, wx: w.x, wy: w.y, from: null };
  }
  function openPaletteCenter() {
    const r = surface!.getBoundingClientRect();
    const w = toWorld(r.left + r.width / 2 - 100, r.top + r.height / 2 - 60);
    palette = { x: r.left + r.width / 2 - 160, y: r.top + 80, wx: w.x, wy: w.y, from: null };
  }
  function pick(d: NodeDef) {
    if (!palette) return;
    addNode(d, palette.wx, palette.wy, palette.from);
    palette = null;
  }

  function onkey(e: KeyboardEvent) {
    if (app.view !== "graph") return;
    const t = e.target as HTMLElement;
    if (t instanceof HTMLInputElement || t instanceof HTMLTextAreaElement) return;
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "z") { e.preventDefault(); undo(e.shiftKey ? 1 : -1); }
    else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "y") { e.preventDefault(); undo(1); }
    else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "d" && sel.node) { e.preventDefault(); duplicate(sel.node); }
    else if ((e.key === "Delete" || e.key === "Backspace") && (sel.node || sel.edge !== undefined)) {
      e.preventDefault();
      if (sel.node) removeNode(sel.node);
      else if (sel.edge !== undefined) { graph.edges = graph.edges.filter((_, i) => i !== sel.edge); sel = {}; commit(); }
    }
    else if (e.key === "f" && !e.ctrlKey) fit();
    else if (e.key === "Escape") { palette = null; link = null; }
    else if ((e.key === " " || e.key === "Tab") && !palette) { e.preventDefault(); openPaletteCenter(); }
  }

  // ---- rendering helpers ----------------------------------------------------------
  const connectedIn = (n: GraphNode, pin: string) => graph.edges.some((e) => e.to.node === n.id && e.to.pin === pin);
  const connectedOut = (n: GraphNode, pin: string) => graph.edges.some((e) => e.from.node === n.id && e.from.pin === pin);
  function wirePath(x1: number, y1: number, x2: number, y2: number) {
    const dx = Math.max(50, Math.abs(x2 - x1) * 0.5);
    return `M${x1},${y1} C${x1 + dx},${y1} ${x2 - dx},${y2} ${x2},${y2}`;
  }
  const edges = $derived(graph.edges.map((e, i) => {
    const a = graph.nodes.find((n) => n.id === e.from.node), b = graph.nodes.find((n) => n.id === e.to.node);
    const da = a && defMap.get(a.type), db = b && defMap.get(b.type);
    if (!a || !b || !da || !db) return null;
    const ia = da.outputs.findIndex((p) => p.name === e.from.pin), ib = db.inputs.findIndex((p) => p.name === e.to.pin);
    if (ia < 0 || ib < 0) return null;
    return { i, d: wirePath(a.x + W, pinY(a, ia), b.x, pinY(b, ib)), ty: da.outputs[ia].ty };
  }).filter((x) => x !== null));
  const linkPath = $derived.by(() => {
    if (!link) return "";
    const n = graph.nodes.find((x) => x.id === link!.node); const d = n && defMap.get(n.type);
    if (!n || !d) return "";
    const list = link.isOutput ? d.outputs : d.inputs;
    const i = list.findIndex((p) => p.name === link!.pin.name);
    const px = link.isOutput ? n.x + W : n.x, py = pinY(n, Math.max(0, i));
    return link.isOutput ? wirePath(px, py, link.x, link.y) : wirePath(link.x, link.y, px, py);
  });

  function setProp(n: GraphNode, p: Pin, raw: string | boolean) {
    n.props[p.name] = p.ty === "number" ? (raw === "" ? undefined : Number(raw)) : raw;
    persist(); recompile();
  }
  const propVal = (n: GraphNode, p: Pin) => (n.props[p.name] ?? p.default ?? "") as string | number | boolean;

  function starter(type: string) {
    const d = defMap.get(type); if (!d) return;
    const r = surface!.getBoundingClientRect(), w = toWorld(r.left + 140, r.top + 120);
    addNode(d, w.x, w.y);
  }
  async function copyLua() { try { await navigator.clipboard.writeText(compiled.code); toast("Copied Lua", "success"); } catch { toast("Copy failed", "error"); } }
</script>

<svelte:window onkeydown={onkey} />

<div class="wrap">
  <aside class="left">
    <div class="panel-title">Graphs <button class="btn ghost icon sm" title="New graph" onclick={() => (newPrompt = true)}><Icon name="plus" size={14} /></button></div>
    <div class="list scroll">
      {#each graphs as g (g)}
        <button class="row" class:sel={g === current} onclick={() => load(g)}>
          <Icon name="graph" size={15} /><span class="grow">{g}</span>
        </button>
      {:else}
        <div class="empty">No graphs yet.<button class="btn primary" onclick={() => (newPrompt = true)}><Icon name="plus" size={14} /> New graph</button></div>
      {/each}
    </div>
    <div class="spacer"></div>
    <div class="tip">
      <strong><Icon name="zap" size={13} /> How it works</strong>
      <p>Start from an <b style="color:var(--gold)">event</b> (when something happens), then chain <b style="color:var(--accent-2)">actions</b>. Data nodes feed values into the circles.</p>
      <p>Everything compiles to Lua — see it on the right.</p>
      <div class="keys"><span><kbd>Space</kbd> add node</span><span><kbd>Del</kbd> delete</span><span><kbd>Ctrl+Z</kbd> undo</span><span><kbd>F</kbd> fit</span><span><kbd>Ctrl+D</kbd> duplicate</span></div>
    </div>
  </aside>

  <div class="center">
    <div class="toolbar">
      <strong>{current || "No graph selected"}</strong>
      {#if current}
        <span class="status" class:bad={!compiled.ok}>
          {#if compiled.ok}<Icon name="check" size={13} /> Compiles{:else}<Icon name="x" size={13} /> Error{/if}
        </span>
      {/if}
      <span class="spacer"></span>
      <button class="btn sm" disabled={!current} onclick={openPaletteCenter}><Icon name="plus" size={14} /> Add node</button>
      <button class="btn icon sm" title="Undo" disabled={!current} onclick={() => undo(-1)}><Icon name="undo" size={15} /></button>
      <button class="btn icon sm" title="Redo" disabled={!current} onclick={() => undo(1)}><Icon name="redo" size={15} /></button>
      <button class="btn icon sm" title="Fit to view (F)" disabled={!current} onclick={fit}><Icon name="fit" size={15} /></button>
      <button class="btn sm" class:on={showLua} onclick={() => (showLua = !showLua)}><Icon name="code" size={14} /> Lua</button>
      <button class="btn sm gold" disabled={!current} onclick={playNow}><Icon name="play" size={13} /> Test</button>
    </div>

    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div class="surface" bind:this={surface} use:wheel onpointerdown={bgDown} onpointermove={move} onpointerup={up} oncontextmenu={openPalette} ondblclick={openPalette}
      style="background-size:{24 * k}px {24 * k}px;background-position:{pan.x}px {pan.y}px">
      {#if current}
        <div class="world" style="transform:translate({pan.x}px,{pan.y}px) scale({k})">
          <svg class="wires" width="1" height="1" overflow="visible">
            {#each edges as e}
              <path d={e.d} class="hit" role="button" tabindex="-1" aria-label="wire" onpointerdown={(ev) => { ev.stopPropagation(); sel = { edge: e.i }; }} />
              <path d={e.d} stroke={col(e.ty)} class="wire" class:sel={sel.edge === e.i} stroke-width={e.ty === "exec" ? 3 : 2.2} />
            {/each}
            {#if link}<path d={linkPath} class="wire ghost" stroke={col(link.pin.ty)} stroke-width="2.4" stroke-dasharray="6 5" />{/if}
          </svg>

          {#each graph.nodes as n (n.id)}
            {@const d = defMap.get(n.type)}
            {#if d}
              <!-- svelte-ignore a11y_no_static_element_interactions -->
              <div class="node {d.kind}" class:sel={sel.node === n.id} style="left:{n.x}px;top:{n.y}px;width:{W}px;height:{heightOf(d)}px">
                <!-- svelte-ignore a11y_no_static_element_interactions -->
                <div class="head" onpointerdown={(e) => nodeDown(e, n)}>
                  <span class="kind"></span><span class="title">{d.title || d.type}</span>
                  <button class="x" title="Delete node" aria-label="Delete" onclick={() => removeNode(n.id)}><Icon name="x" size={12} /></button>
                </div>
                {#each Array(rowsOf(d)) as _, i}
                  {@const pi = d.inputs[i]}
                  {@const po = d.outputs[i]}
                  <div class="row2" style="top:{HEAD + PAD + i * ROW}px;height:{ROW}px">
                    {#if pi}
                      <div class="cell in">
                        <button class="pin {pi.ty === 'exec' ? 'exec' : ''}" class:on={connectedIn(n, pi.name)} style="--c:{col(pi.ty)}" data-node={n.id} data-pin={pi.name} data-dir="in"
                          aria-label={pi.name} onpointerdown={(e) => pinDown(e, n, pi, false)}></button>
                        <span class="lbl">{pi.ty === "exec" && pi.name === "exec" ? "" : pi.name}</span>
                        {#if pi.ty !== "exec" && !connectedIn(n, pi.name)}
                          {#if pi.ty === "bool"}
                            <input type="checkbox" checked={!!propVal(n, pi)} onchange={(e) => setProp(n, pi, e.currentTarget.checked)} />
                          {:else if pi.ty === "entity"}
                            <span class="nil">none</span>
                          {:else}
                            <input class="lit" type={pi.ty === "number" ? "number" : "text"} step="any" value={propVal(n, pi) as string} oninput={(e) => setProp(n, pi, e.currentTarget.value)} onchange={commit} spellcheck="false" />
                          {/if}
                        {/if}
                      </div>
                    {/if}
                    {#if po}
                      <div class="cell out">
                        <span class="lbl">{po.ty === "exec" && po.name === "then" ? "" : po.name}</span>
                        <button class="pin {po.ty === 'exec' ? 'exec' : ''}" class:on={connectedOut(n, po.name)} style="--c:{col(po.ty)}" data-node={n.id} data-pin={po.name} data-dir="out"
                          aria-label={po.name} onpointerdown={(e) => pinDown(e, n, po, true)}></button>
                      </div>
                    {/if}
                  </div>
                {/each}
              </div>
            {:else}
              <div class="node data bad" style="left:{n.x}px;top:{n.y}px;width:{W}px;height:70px"><div class="head"><span class="title">Unknown node: {n.type}</span><button class="x" aria-label="Delete" onclick={() => removeNode(n.id)}><Icon name="x" size={12} /></button></div></div>
            {/if}
          {/each}
        </div>

        {#if !graph.nodes.length}
          <div class="hero">
            <h3>Empty graph</h3>
            <p class="dim">Pick an event to start, or press <kbd>Space</kbd> / double-click to browse every node.</p>
            <div class="starters">
              <button class="btn" onclick={() => starter("event.died")}><span class="dotg"></span> When something dies</button>
              <button class="btn" onclick={() => starter("event.moved")}><span class="dotg"></span> When something moves</button>
              <button class="btn" onclick={() => starter("event.turn_started")}><span class="dotg"></span> Every turn</button>
              <button class="btn" onclick={() => starter("event.attacked")}><span class="dotg"></span> When attacked</button>
            </div>
          </div>
        {/if}
      {:else}
        <div class="hero"><h3>Visual scripting</h3><p class="dim">Create a graph to start wiring up game logic without writing code.</p>
          <button class="btn primary" onclick={() => (newPrompt = true)}><Icon name="plus" size={14} /> New graph</button></div>
      {/if}
      <div class="zoomtag mono dim">{Math.round(k * 100)}%</div>
    </div>
  </div>

  {#if showLua && current}
    <aside class="right">
      <div class="panel-title">Generated Lua <button class="btn ghost icon sm" title="Copy" onclick={copyLua}><Icon name="copy" size={14} /></button></div>
      {#if !compiled.ok}<div class="err">{compiled.error}</div>{/if}
      <pre class="code mono scroll">{compiled.code || "-- (empty)"}</pre>
      <div class="spacer"></div>
      <button class="btn danger" onclick={() => (confirmDelete = true)}><Icon name="trash" size={14} /> Delete graph</button>
    </aside>
  {/if}
</div>

{#if palette}<NodePalette {defs} from={palette.from ? { pin: palette.from.pin, isOutput: palette.from.isOutput } : null} x={palette.x} y={palette.y} onpick={pick} onclose={() => (palette = null)} />{/if}
{#if newPrompt}<Prompt title="New graph" label="Graph name" initial="my_graph" onsubmit={create} onclose={() => (newPrompt = false)} />{/if}
{#if confirmDelete}<Confirm title="Delete graph" text={`Delete “${current}”? This cannot be undone.`} onyes={remove} onclose={() => (confirmDelete = false)} />{/if}

<style>
  .wrap { flex: 1; display: flex; min-width: 0; }
  aside { flex: none; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: var(--bg-1); overflow: auto; }
  .left { width: 250px; border-right: 1px solid var(--line); }
  .right { width: 380px; border-left: 1px solid var(--line); }
  .center { flex: 1; display: flex; flex-direction: column; min-width: 0; }
  .toolbar { display: flex; gap: 6px; padding: 8px 12px; border-bottom: 1px solid var(--line); background: var(--bg-1); align-items: center; }
  .status { display: inline-flex; align-items: center; gap: 4px; font-size: 11.5px; color: var(--good); background: rgba(74, 222, 128, 0.1); padding: 2px 9px; border-radius: 99px; }
  .status.bad { color: var(--bad); background: rgba(248, 113, 113, 0.12); }
  .surface { flex: 1; position: relative; overflow: hidden; cursor: grab; touch-action: none; background-color: #0d1017; background-image: radial-gradient(circle, #2a3145 1.2px, transparent 1.4px); }
  .surface:active { cursor: grabbing; }
  .world { position: absolute; left: 0; top: 0; transform-origin: 0 0; }
  .wires { position: absolute; left: 0; top: 0; pointer-events: none; overflow: visible; }
  .wire { fill: none; stroke-linecap: round; transition: stroke-width 0.1s; filter: drop-shadow(0 0 3px rgba(0, 0, 0, 0.6)); }
  .wire.sel { stroke-width: 4.5; filter: drop-shadow(0 0 6px currentColor) brightness(1.3); }
  .wire.ghost { opacity: 0.8; }
  .hit { fill: none; stroke: transparent; stroke-width: 14; pointer-events: stroke; cursor: pointer; }

  .node { position: absolute; background: #161a25; border: 1px solid var(--line-2); border-radius: 11px; box-shadow: 0 6px 18px rgba(0, 0, 0, 0.5); cursor: default; user-select: none; }
  .node.sel { border-color: var(--accent); box-shadow: 0 0 0 2px var(--accent-bg), 0 10px 26px rgba(0, 0, 0, 0.6); }
  .node.bad { border-color: var(--bad); }
  .head { height: 34px; display: flex; align-items: center; gap: 8px; padding: 0 8px 0 11px; border-radius: 10px 10px 0 0; cursor: grab; background: linear-gradient(180deg, #232a3b, #1c2232); border-bottom: 1px solid var(--line-2); }
  .node.event .head { background: linear-gradient(180deg, #5a4216, #3e2e10); border-bottom-color: #7a5a1c; }
  .node.data .head { background: linear-gradient(180deg, #1f4a38, #173628); border-bottom-color: #266049; }
  .node.flow .head { background: linear-gradient(180deg, #38307a, #2a2460); border-bottom-color: #4b41a0; }
  .title { flex: 1; font-weight: 600; font-size: 12.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .x { opacity: 0; background: none; border: 0; padding: 3px; border-radius: 5px; color: var(--text-2); display: grid; }
  .head:hover .x { opacity: 1; } .x:hover { background: rgba(248, 113, 113, 0.25); color: var(--bad); }
  .row2 { position: absolute; left: 0; right: 0; display: flex; justify-content: space-between; align-items: center; }
  .cell { display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--text-2); min-width: 0; }
  .cell.in { padding-left: 0; flex: 1; } .cell.out { margin-left: auto; padding-right: 0; }
  .lbl { white-space: nowrap; }
  .pin { width: 14px; height: 14px; border-radius: 50%; border: 2.5px solid var(--c); background: #0d1017; padding: 0; flex: none; margin: 0 -7px; position: relative; z-index: 2; transition: transform 0.1s; }
  .pin.on { background: var(--c); }
  .pin:hover { transform: scale(1.35); box-shadow: 0 0 8px var(--c); }
  .pin.exec { border-radius: 3px 8px 8px 3px; width: 15px; height: 15px; clip-path: polygon(0 0, 62% 0, 100% 50%, 62% 100%, 0 100%); border-width: 0; background: var(--c); opacity: 0.45; }
  .pin.exec.on { opacity: 1; }
  .cell.in .pin { margin-right: 4px; } .cell.out .pin { margin-left: 4px; }
  .lit { flex: 1; min-width: 36px; width: auto; padding: 1px 5px; font-size: 11px; height: 20px; background: #0d1017; border-radius: 5px; }
  input[type="checkbox"] { margin: 0; }
  .nil { color: var(--dim); font-size: 11px; font-style: italic; }
  .hero { position: absolute; inset: 0; display: grid; place-content: center; text-align: center; gap: 10px; pointer-events: none; justify-items: center; }
  .hero > * { pointer-events: auto; }
  .hero h3 { margin: 0; font-size: 20px; }
  .hero p { margin: 0; max-width: 360px; }
  .starters { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; margin-top: 8px; }
  .dotg { width: 8px; height: 8px; border-radius: 50%; background: var(--gold); }
  .zoomtag { position: absolute; right: 12px; bottom: 10px; font-size: 11px; }
  .tip { background: var(--bg-2); border: 1px solid var(--line); border-radius: 10px; padding: 10px 12px; font-size: 12px; color: var(--text-2); }
  .tip strong { display: flex; gap: 5px; align-items: center; color: var(--text); margin-bottom: 4px; }
  .tip p { margin: 4px 0; line-height: 1.5; }
  .keys { display: flex; flex-wrap: wrap; gap: 5px 10px; margin-top: 8px; font-size: 11px; color: var(--dim); }
  .code { margin: 0; flex: 1; min-height: 120px; background: #0b0d12; border: 1px solid var(--line); border-radius: 9px; padding: 12px; font-size: 12px; line-height: 1.6; color: #c8d0e4; white-space: pre; tab-size: 2; }
  .err { background: rgba(248, 113, 113, 0.1); border: 1px solid rgba(248, 113, 113, 0.4); color: #ffb3b3; padding: 8px 10px; border-radius: 8px; font-size: 12px; }
</style>
