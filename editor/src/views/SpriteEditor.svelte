<script lang="ts">
  import { app, fail, refreshProject, toast, debounced } from "../lib/app.svelte";
  import { call } from "../lib/api";
  import { invalidateSprite, loadSprite, drawSprite } from "../lib/sprites";
  import type { SpriteView } from "../lib/types";
  import Icon from "../ui/Icon.svelte";
  import Modal from "../ui/Modal.svelte";
  import Confirm from "../ui/Confirm.svelte";
  import SpriteThumb from "../ui/SpriteThumb.svelte";

  type Tool = "pencil" | "eraser" | "line" | "rect" | "fill" | "picker";
  const tools: { id: Tool; icon: string; label: string; key: string }[] = [
    { id: "pencil", icon: "pencil", label: "Pencil", key: "b" },
    { id: "eraser", icon: "eraser", label: "Eraser", key: "e" },
    { id: "line", icon: "line", label: "Line", key: "l" },
    { id: "rect", icon: "square", label: "Rectangle (Shift = filled)", key: "r" },
    { id: "fill", icon: "bucket", label: "Fill", key: "g" },
    { id: "picker", icon: "pipette", label: "Pick color", key: "i" },
  ];
  const PALETTE = ["#000000", "#1d2b53", "#7e2553", "#008751", "#ab5236", "#5f574f", "#c2c3c7", "#fff1e8", "#ff004d", "#ffa300", "#ffec27", "#00e436", "#29adff", "#83769c", "#ff77a8", "#ffccaa",
    "#f5b942", "#8b7bff", "#e5484d", "#62d48f", "#3d6fd1", "#b8864b", "#dfe3ea", "#14161c"];

  let view = $state<SpriteView | null>(null);
  let tool = $state<Tool>("pencil");
  let color = $state("#f5b942");
  let recent = $state<string[]>([]);
  let grid = $state(true);
  let showNew = $state(false);
  let confirmDelete = $state(false);
  let newName = $state("");
  let newSize = $state(16);
  let hover = $state<{ x: number; y: number } | null>(null);
  let box = $state({ w: 600, h: 500 });
  let canvas = $state<HTMLCanvasElement>();
  let preview = $state<{ x0: number; y0: number; x1: number; y1: number; filled: boolean } | null>(null);

  const sprites = $derived(app.project?.sprites ?? []);
  const zoom = $derived(view ? Math.max(2, Math.min(48, Math.floor(Math.min((box.w - 32) / view.width, (box.h - 32) / view.height)))) : 8);

  // ---- backend ops, serialized so rapid mouse moves apply in order -----------
  let chain: Promise<unknown> = Promise.resolve();
  function send(op: Record<string, unknown>) {
    chain = chain
      .then(() => call<SpriteView>("sprite_op", { op }))
      .then((v) => { view = v; })
      .catch(fail);
    return chain;
  }
  const bump = debounced(async () => {
    if (!view) return;
    invalidateSprite(view.name);
    app.spriteRev++;
    await refreshProject();
  }, 250);

  async function open(name: string) {
    await chain;
    try { view = await call<SpriteView>("sprite_open", { name }); app.selected.sprite = name; } catch (e) { fail(e); }
  }
  $effect(() => {
    // open the selected (or first) sprite once the project is available
    const names = sprites.map((s) => s.name);
    if (!names.length) { view = null; return; }
    if (!view || !names.includes(view.name)) open(names.includes(app.selected.sprite) ? app.selected.sprite : names[0]);
  });

  // ---- drawing ----------------------------------------------------------------
  function render() {
    if (!view || !canvas) return;
    const { width: w, height: h, pixels: p } = view;
    const Z = zoom;
    canvas.width = w * Z; canvas.height = h * Z;
    const ctx = canvas.getContext("2d")!;
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      ctx.fillStyle = (x + y) % 2 ? "#1f2431" : "#262c3b";
      ctx.fillRect(x * Z, y * Z, Z, Z);
      const i = (y * w + x) * 4;
      if (p[i + 3]) { ctx.fillStyle = `rgba(${p[i]},${p[i + 1]},${p[i + 2]},${p[i + 3] / 255})`; ctx.fillRect(x * Z, y * Z, Z, Z); }
    }
    if (preview) {
      ctx.fillStyle = tool === "eraser" ? "rgba(255,255,255,.35)" : color + "cc";
      for (const [x, y] of shapePixels(preview)) ctx.fillRect(x * Z, y * Z, Z, Z);
    }
    if (grid && Z >= 8) {
      ctx.strokeStyle = "rgba(255,255,255,0.07)"; ctx.lineWidth = 1; ctx.beginPath();
      for (let x = 1; x < w; x++) { ctx.moveTo(x * Z + 0.5, 0); ctx.lineTo(x * Z + 0.5, h * Z); }
      for (let y = 1; y < h; y++) { ctx.moveTo(0, y * Z + 0.5); ctx.lineTo(w * Z, y * Z + 0.5); }
      ctx.stroke();
    }
    if (hover && tool !== "picker") {
      ctx.strokeStyle = "rgba(255,255,255,0.85)"; ctx.lineWidth = 1; ctx.strokeRect(hover.x * Z + 0.5, hover.y * Z + 0.5, Z - 1, Z - 1);
    }
  }
  $effect(() => { view; zoom; grid; preview; hover; color; tool; render(); });

  function shapePixels(p: { x0: number; y0: number; x1: number; y1: number; filled: boolean }): [number, number][] {
    const out: [number, number][] = [];
    if (tool === "rect") {
      const [xa, xb, ya, yb] = [Math.min(p.x0, p.x1), Math.max(p.x0, p.x1), Math.min(p.y0, p.y1), Math.max(p.y0, p.y1)];
      for (let y = ya; y <= yb; y++) for (let x = xa; x <= xb; x++) if (p.filled || x === xa || x === xb || y === ya || y === yb) out.push([x, y]);
    } else {
      let [x, y] = [p.x0, p.y0];
      const dx = Math.abs(p.x1 - x), dy = -Math.abs(p.y1 - y), sx = x < p.x1 ? 1 : -1, sy = y < p.y1 ? 1 : -1;
      let err = dx + dy;
      for (;;) { out.push([x, y]); if (x === p.x1 && y === p.y1) break; const e2 = 2 * err; if (e2 >= dy) { err += dy; x += sx; } if (e2 <= dx) { err += dx; y += sy; } }
    }
    return out;
  }

  const rgba = (hex = color): number[] => [parseInt(hex.slice(1, 3), 16), parseInt(hex.slice(3, 5), 16), parseInt(hex.slice(5, 7), 16), 255];
  function cellAt(e: PointerEvent) {
    const r = canvas!.getBoundingClientRect();
    return {
      x: Math.max(0, Math.min(view!.width - 1, Math.floor(((e.clientX - r.left) / r.width) * view!.width))),
      y: Math.max(0, Math.min(view!.height - 1, Math.floor(((e.clientY - r.top) / r.height) * view!.height))),
    };
  }
  function useColor(hex: string) {
    color = hex;
    if (tool === "eraser" || tool === "picker") tool = "pencil";
    recent = [hex, ...recent.filter((c) => c !== hex)].slice(0, 8);
  }

  let drag: { x: number; y: number; sx: number; sy: number } | null = null;
  function down(e: PointerEvent) {
    if (!view) return;
    canvas!.setPointerCapture(e.pointerId);
    const c = cellAt(e);
    const erase = tool === "eraser" || e.button === 2;
    if (tool === "picker") {
      const i = (c.y * view.width + c.x) * 4, p = view.pixels;
      if (p[i + 3]) useColor("#" + [p[i], p[i + 1], p[i + 2]].map((v) => v.toString(16).padStart(2, "0")).join(""));
      tool = "pencil"; return;
    }
    if (tool === "fill") { send({ op: "fill", x: c.x, y: c.y, color: erase ? [0, 0, 0, 0] : rgba() }); bump(); recent = [color, ...recent.filter((x) => x !== color)].slice(0, 8); return; }
    drag = { x: c.x, y: c.y, sx: c.x, sy: c.y };
    if (tool === "pencil" || tool === "eraser") {
      send({ op: "stroke_start" });
      send({ op: "pixel", x: c.x, y: c.y, color: erase ? [0, 0, 0, 0] : rgba() });
    } else preview = { x0: c.x, y0: c.y, x1: c.x, y1: c.y, filled: e.shiftKey };
  }
  function move(e: PointerEvent) {
    if (!view) return;
    const c = cellAt(e);
    hover = c;
    if (!drag) return;
    if (tool === "pencil" || tool === "eraser") {
      if (c.x !== drag.x || c.y !== drag.y) {
        send({ op: "line", x0: drag.x, y0: drag.y, x1: c.x, y1: c.y, color: tool === "eraser" || (e.buttons & 2) ? [0, 0, 0, 0] : rgba() });
        drag.x = c.x; drag.y = c.y;
      }
    } else preview = { x0: drag.sx, y0: drag.sy, x1: c.x, y1: c.y, filled: e.shiftKey };
  }
  function up(e: PointerEvent) {
    if (!drag || !view) return;
    const c = cellAt(e);
    if (tool === "pencil" || tool === "eraser") send({ op: "stroke_end" });
    else if (tool === "line") send({ op: "line", x0: drag.sx, y0: drag.sy, x1: c.x, y1: c.y, color: rgba() });
    else if (tool === "rect") send({ op: "rect", x0: drag.sx, y0: drag.sy, x1: c.x, y1: c.y, color: rgba(), filled: e.shiftKey });
    if (tool !== "picker") { recent = [color, ...recent.filter((x) => x !== color)].slice(0, 8); }
    drag = null; preview = null; bump();
  }

  function onkey(e: KeyboardEvent) {
    if (app.view !== "sprites") return;
    const t = e.target as HTMLElement;
    if (t instanceof HTMLInputElement || t instanceof HTMLTextAreaElement) return;
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "z") { e.preventDefault(); send({ op: e.shiftKey ? "redo" : "undo" }); bump(); return; }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "y") { e.preventDefault(); send({ op: "redo" }); bump(); return; }
    if (e.ctrlKey || e.metaKey || e.altKey) return;
    const tl = tools.find((x) => x.key === e.key.toLowerCase());
    if (tl) tool = tl.id;
    else if (e.key === "#") grid = !grid;
  }

  // ---- sprite management -----------------------------------------------------
  async function create() {
    try {
      const v = await call<SpriteView>("sprite_new", { name: newName, width: newSize, height: newSize });
      app.selected.sprite = v.name;
      showNew = false; newName = "";
      // refresh the list first: the "open first sprite" effect must not see a view that isn't listed yet
      await refreshProject(); view = v; app.spriteRev++;
    } catch (e) { fail(e); }
  }
  async function remove() {
    if (!view) return;
    const name = view.name;
    try { await call("sprite_delete", { name }); invalidateSprite(name); view = null; app.selected.sprite = ""; await refreshProject(); app.spriteRev++; toast(`Deleted ${name}`); } catch (e) { fail(e); }
  }
  async function importPng(ev: Event) {
    const f = (ev.target as HTMLInputElement).files?.[0];
    if (!f) return;
    try {
      const buf = new Uint8Array(await f.arrayBuffer());
      let bin = ""; for (const b of buf) bin += String.fromCharCode(b);
      const img = await createImageBitmap(f);
      const name = f.name.replace(/\.[^.]+$/, "").replace(/[^A-Za-z0-9_-]/g, "_").slice(0, 40) || "imported";
      const fw = img.width % img.height === 0 && img.width > img.height ? img.height : img.width;
      const v = await call<SpriteView>("sprite_import", { name, png: btoa(bin), frame_width: fw });
      app.selected.sprite = v.name;
      await refreshProject(); view = v; app.spriteRev++;
      toast(`Imported ${name} (${v.frame_count} frame${v.frame_count > 1 ? "s" : ""})`, "success");
    } catch (e) { fail(e); }
    (ev.target as HTMLInputElement).value = "";
  }
  const setFps = debounced(async (fps: number) => { view = await call<SpriteView>("sprite_set_fps", { fps }); bump(); }, 200);

  // frame thumbnails: slice from the saved sheet
  function frameThumb(node: HTMLCanvasElement, args: { name: string; i: number; rev: number }) {
    let a = args;
    const draw = async () => {
      const s = await loadSprite(a.name);
      const ctx = node.getContext("2d")!;
      ctx.clearRect(0, 0, node.width, node.height);
      if (!s) return;
      ctx.imageSmoothingEnabled = false;
      ctx.drawImage(s.img, a.i * s.w, 0, s.w, s.h, 0, 0, node.width, node.height);
    };
    draw();
    return { update(n: typeof args) { a = n; draw(); } };
  }
</script>

<svelte:window onkeydown={onkey} />

<div class="wrap">
  <aside class="left">
    <div class="panel-title">Sprites <span>
      <label class="btn ghost icon sm" title="Import PNG"><Icon name="upload" size={14} /><input type="file" accept="image/png" hidden onchange={importPng} /></label>
      <button class="btn ghost icon sm" title="New sprite" onclick={() => (showNew = true)}><Icon name="plus" size={14} /></button></span></div>
    <div class="list scroll">
      {#each sprites as s (s.name)}
        <button class="row" class:sel={view?.name === s.name} onclick={() => open(s.name)}>
          <SpriteThumb name={s.name} size={28} />
          <span class="grow">{s.name}</span>
          <span class="dim mono" style="font-size:10px">{s.width}×{s.height}{s.frames > 1 ? ` · ${s.frames}f` : ""}</span>
        </button>
      {:else}
        <div class="empty">No sprites yet.<button class="btn primary" onclick={() => (showNew = true)}><Icon name="plus" size={14} /> New sprite</button></div>
      {/each}
    </div>
  </aside>

  <div class="center">
    <div class="toolbar">
      {#each tools as t}
        <button class="btn icon" class:on={tool === t.id} title="{t.label} ({t.key.toUpperCase()})" onclick={() => (tool = t.id)}><Icon name={t.icon} size={17} /></button>
      {/each}
      <div class="sep-v"></div>
      <button class="btn icon" title="Flip horizontally" onclick={() => { send({ op: "flip_horizontal" }); bump(); }}><Icon name="flip" size={17} /></button>
      <button class="btn icon" class:on={grid} title="Toggle grid (#)" onclick={() => (grid = !grid)}><Icon name="grid" size={17} /></button>
      <div class="sep-v"></div>
      <button class="btn icon" title="Undo (Ctrl+Z)" onclick={() => { send({ op: "undo" }); bump(); }}><Icon name="undo" size={17} /></button>
      <button class="btn icon" title="Redo (Ctrl+Y)" onclick={() => { send({ op: "redo" }); bump(); }}><Icon name="redo" size={17} /></button>
    </div>
    <div class="canvas-area" bind:clientWidth={box.w} bind:clientHeight={box.h}>
      {#if view}
        <canvas bind:this={canvas} class="pixel" style="cursor:{tool === 'picker' ? 'copy' : 'crosshair'}"
          onpointerdown={down} onpointermove={move} onpointerup={up} onpointerleave={() => (hover = null)} oncontextmenu={(e) => e.preventDefault()}></canvas>
      {:else}
        <div class="empty"><Icon name="image" size={34} /><span>Create or select a sprite to start drawing</span></div>
      {/if}
    </div>
    <div class="status dim mono">
      {#if view}{view.name} · {view.width}×{view.height} · frame {view.frame + 1}/{view.frame_count} · {zoom}×{#if hover} · ({hover.x}, {hover.y}){/if}{/if}
    </div>
  </div>

  <aside class="right">
    {#if view}
      <div class="panel-title">Color</div>
      <div class="colorrow">
        <input type="color" value={color} oninput={(e) => useColor(e.currentTarget.value)} />
        <input type="text" class="mono" value={color} onchange={(e) => /^#[0-9a-f]{6}$/i.test(e.currentTarget.value) && useColor(e.currentTarget.value)} style="width:92px" />
        <span class="swatch big" style="background:{color}"></span>
      </div>
      <div class="palette">
        {#each PALETTE as c}<button class="swatch" class:cur={c === color} style="background:{c}" title={c} aria-label={c} onclick={() => useColor(c)}></button>{/each}
      </div>
      {#if recent.length}
        <div class="label">Recent</div>
        <div class="palette">{#each recent as c}<button class="swatch" style="background:{c}" aria-label={c} onclick={() => useColor(c)}></button>{/each}</div>
      {/if}

      <div class="sep"></div>
      <div class="panel-title">Frames
        <span><button class="btn ghost icon sm" title="Add blank frame" onclick={() => { send({ op: "add_frame", duplicate: false }); bump(); }}><Icon name="plus" size={14} /></button>
        <button class="btn ghost icon sm" title="Duplicate frame" onclick={() => { send({ op: "add_frame", duplicate: true }); bump(); }}><Icon name="copy" size={14} /></button>
        <button class="btn ghost icon sm" title="Delete frame" disabled={view.frame_count < 2} onclick={() => { send({ op: "remove_frame" }); bump(); }}><Icon name="trash" size={14} /></button></span></div>
      <div class="frames">
        {#each Array(view.frame_count) as _, i}
          <button class="frame" class:cur={i === view.frame} onclick={() => send({ op: "select_frame", index: i })}>
            <canvas width="48" height="48" class="pixel" use:frameThumb={{ name: view.name, i, rev: app.spriteRev }}></canvas><span>{i + 1}</span>
          </button>
        {/each}
      </div>

      <div class="panel-title">Animation</div>
      <div class="preview"><SpriteThumb name={view.name} size={72} animate /></div>
      <div class="field"><label for="fps">Speed: {view.fps} fps</label><input id="fps" type="range" min="1" max="24" value={view.fps} oninput={(e) => setFps(+e.currentTarget.value)} /></div>
      <div class="dim" style="font-size:11.5px">Sprites with several frames animate automatically in the game.</div>

      <div class="spacer"></div>
      <button class="btn danger" onclick={() => (confirmDelete = true)}><Icon name="trash" size={14} /> Delete sprite</button>
    {/if}
  </aside>
</div>

{#if showNew}
  <Modal title="New sprite" onclose={() => (showNew = false)} width={380}>
    <form class="field" onsubmit={(e) => { e.preventDefault(); create(); }}>
      <label for="sn">Name</label>
      <!-- svelte-ignore a11y_autofocus -->
      <input id="sn" type="text" bind:value={newName} autofocus placeholder="e.g. dragon" />
      <label for="ss" style="margin-top:8px">Size</label>
      <div class="sizes">{#each [8, 16, 24, 32, 48, 64] as s}<button type="button" class="btn" class:on={newSize === s} onclick={() => (newSize = s)}>{s}×{s}</button>{/each}</div>
      <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:12px">
        <button type="button" class="btn ghost" onclick={() => (showNew = false)}>Cancel</button>
        <button class="btn primary" disabled={!/^[A-Za-z0-9_-]+$/.test(newName)}>Create</button>
      </div>
    </form>
  </Modal>
{/if}
{#if confirmDelete && view}
  <Confirm title="Delete sprite" text={`Delete “${view.name}”? Objects using it will fall back to their glyph.`} onyes={remove} onclose={() => (confirmDelete = false)} />
{/if}

<style>
  .wrap { flex: 1; display: flex; min-width: 0; }
  aside { flex: none; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: var(--bg-1); overflow: auto; }
  .left { width: 250px; border-right: 1px solid var(--line); }
  .right { width: 290px; border-left: 1px solid var(--line); }
  .center { flex: 1; display: flex; flex-direction: column; min-width: 0; }
  .toolbar { display: flex; gap: 4px; padding: 8px 12px; border-bottom: 1px solid var(--line); background: var(--bg-1); align-items: center; }
  .sep-v { width: 1px; align-self: stretch; background: var(--line); margin: 2px 6px; }
  .canvas-area { flex: 1; min-height: 0; display: grid; place-items: center; background: radial-gradient(circle at 50% 40%, #171b27, #0c0e14); overflow: hidden; }
  canvas { border-radius: 6px; box-shadow: 0 12px 40px rgba(0, 0, 0, 0.6), 0 0 0 1px var(--line-2); touch-action: none; }
  .status { padding: 6px 14px; border-top: 1px solid var(--line); font-size: 11px; background: var(--bg-1); min-height: 28px; }
  .colorrow { display: flex; gap: 8px; align-items: center; }
  .swatch { width: 100%; aspect-ratio: 1; border-radius: 5px; border: 1px solid rgba(255, 255, 255, 0.12); padding: 0; }
  .swatch:hover { transform: scale(1.12); }
  .swatch.cur { outline: 2px solid var(--text); outline-offset: 1px; }
  .swatch.big { width: 30px; height: 30px; margin-left: auto; aspect-ratio: auto; }
  .palette { display: grid; grid-template-columns: repeat(8, 1fr); gap: 4px; }
  .frames { display: flex; flex-wrap: wrap; gap: 6px; }
  .frame { position: relative; width: 52px; height: 52px; padding: 2px; background: var(--bg-2); border: 1px solid var(--line-2); border-radius: 7px; }
  .frame.cur { border-color: var(--accent); box-shadow: 0 0 0 2px var(--accent-bg); }
  .frame canvas { width: 100%; height: 100%; box-shadow: none; border-radius: 4px; background: #1f2431; }
  .frame span { position: absolute; bottom: 1px; right: 4px; font-size: 9px; color: var(--text); text-shadow: 0 0 3px #000; }
  .preview { display: grid; place-items: center; padding: 10px; background: #1f2431; border-radius: 9px; border: 1px solid var(--line); }
  .sizes { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px; }
</style>
