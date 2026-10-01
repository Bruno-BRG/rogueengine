<script lang="ts">
  import { app, playNow } from "../lib/app.svelte";
  import { call, engine } from "../lib/api";
  import { drawSprite, spriteNow, resetReady } from "../lib/sprites";
  import type { Snapshot, ItemView, EntityView } from "../lib/types";
  import Icon from "../ui/Icon.svelte";
  import { fail, flushSaves } from "../lib/app.svelte";

  let snap = $state<Snapshot | null>(null);
  let error = $state("");
  let selected = $state<number | null>(null);
  let canvas: HTMLCanvasElement;
  let wrap: HTMLDivElement;
  let tile = $state(48);
  let consoleOpen = $state(false);
  let consoleInput = $state("");
  let consoleLines = $state<{ kind: "in" | "ok" | "err"; text: string }[]>([]);
  let consoleHist: string[] = [];
  let histPos = -1;
  async function runConsole() {
    const code = consoleInput.trim();
    if (!code) return;
    consoleHist.unshift(code); histPos = -1; consoleInput = "";
    consoleLines.push({ kind: "in", text: code });
    try {
      const r = (await call<{ type: string; value?: unknown; message?: string }>("engine", { request: { type: "eval", code } }));
      if (r.type === "error") consoleLines.push({ kind: "err", text: r.message ?? "error" });
      else {
        const v = r.value;
        if (v !== null && v !== undefined) consoleLines.push({ kind: "ok", text: typeof v === "string" ? v : JSON.stringify(v, null, 1) });
        const st = await engine({ type: "snapshot" });
        if (st.type === "state") accept(st);
      }
    } catch (e) { consoleLines.push({ kind: "err", text: String(e) }); }
    queueMicrotask(() => consoleEl && (consoleEl.scrollTop = consoleEl.scrollHeight));
  }
  let consoleEl = $state<HTMLDivElement>();
  function consoleKey(e: KeyboardEvent) {
    if (e.key === "Enter") { e.preventDefault(); runConsole(); }
    else if (e.key === "ArrowUp") { e.preventDefault(); histPos = Math.min(consoleHist.length - 1, histPos + 1); consoleInput = consoleHist[histPos] ?? ""; }
    else if (e.key === "ArrowDown") { e.preventDefault(); histPos = Math.max(-1, histPos - 1); consoleInput = histPos < 0 ? "" : consoleHist[histPos]; }
    else if (e.key === "Escape") { consoleOpen = false; }
  }

  // ---- running the game --------------------------------------------------
  let busy = false;
  async function newGame() {
    error = "";
    resetReady();
    try {
      await flushSaves();
      const r = await engine({ type: "new_game", seed: Math.floor(Math.random() * 1e9) });
      if (r.type === "error") { error = r.message; snap = null; }
      else if (r.type === "state") accept(r);
    } catch (e) { fail(e); }
  }

  async function act(action: string, arg: Record<string, unknown> = {}) {
    if (busy || !snap || snap.game_over) return;
    busy = true;
    try {
      const r = await engine({ type: "act", action, arg });
      if (r.type === "error") fail(new Error(r.message));
      else if (r.type === "state") accept(r);
    } catch (e) { fail(e); } finally { busy = false; }
  }

  // Smooth movement: remember where each entity was and tween to its new tile.
  const tween = new Map<number, { fx: number; fy: number; tx: number; ty: number; t0: number }>();
  const MOVE_MS = 110;
  function accept(next: Snapshot) {
    const now = performance.now();
    const prev = new Map((snap?.entities ?? []).map((e) => [e.id, e]));
    const live = new Set<number>();
    for (const e of next.entities) {
      live.add(e.id);
      const p = prev.get(e.id);
      if (p && (p.x !== e.x || p.y !== e.y)) {
        const cur = pos(e.id, now);
        const dist = Math.abs(p.x - e.x) + Math.abs(p.y - e.y);
        if (dist <= 3) tween.set(e.id, { fx: cur?.x ?? p.x, fy: cur?.y ?? p.y, tx: e.x, ty: e.y, t0: now });
        else tween.delete(e.id);
      }
    }
    for (const id of [...tween.keys()]) if (!live.has(id)) tween.delete(id);
    // Level change: snap everything.
    if (snap && (snap.width !== next.width || (snap.hud[0]?.[1] !== next.hud[0]?.[1]))) tween.clear();
    if (selected !== null && !next.inventory.some((i) => i.id === selected)) selected = null;
    snap = next;
  }
  function pos(id: number, now: number) {
    const e = snap?.entities.find((x) => x.id === id);
    const t = tween.get(id);
    if (!e) return null;
    if (!t) return { x: e.x, y: e.y };
    const k = Math.min(1, (now - t.t0) / MOVE_MS);
    const s = k * (2 - k); // ease-out
    return { x: t.fx + (t.tx - t.fx) * s, y: t.fy + (t.ty - t.fy) * s };
  }

  $effect(() => {
    app.playRequest; // re-run when the Play button is pressed
    if (app.project) newGame();
  });

  // ---- input ----------------------------------------------------------------
  const MOVES: Record<string, [number, number]> = {
    ArrowUp: [0, -1], ArrowDown: [0, 1], ArrowLeft: [-1, 0], ArrowRight: [1, 0],
    w: [0, -1], s: [0, 1], a: [-1, 0], d: [1, 0],
    k: [0, -1], j: [0, 1], h: [-1, 0], l: [1, 0], y: [-1, -1], u: [1, -1], b: [-1, 1], n: [1, 1],
    Home: [-1, -1], PageUp: [1, -1], End: [-1, 1], PageDown: [1, 1],
    "8": [0, -1], "2": [0, 1], "4": [-1, 0], "6": [1, 0], "7": [-1, -1], "9": [1, -1], "1": [-1, 1], "3": [1, 1],
  };
  function onkey(e: KeyboardEvent) {
    if (app.view !== "play" || e.altKey || e.ctrlKey || e.metaKey) return;
    const t = e.target as HTMLElement;
    if (t instanceof HTMLInputElement || t instanceof HTMLTextAreaElement || t.closest?.(".cm-editor")) return;
    if (e.key === "`" ) { e.preventDefault(); consoleOpen = !consoleOpen; return; }
    const m = MOVES[e.key];
    if (m && !(e.code.startsWith("Digit"))) { e.preventDefault(); act("move", { dx: m[0], dy: m[1] }); return; }
    if (e.key === ".") { e.preventDefault(); act("wait"); }
    else if (e.key === " ") { e.preventDefault(); act("wait"); }
    else if (e.key === "g" || e.key === ",") { e.preventDefault(); act("pickup"); }
    else if (e.key === ">" || e.key === "Enter") { e.preventDefault(); act("descend"); }
    else if (/^[1-9]$/.test(e.key) && e.code.startsWith("Digit") && snap) {
      const it = snap.inventory[+e.key - 1];
      if (it) { e.preventDefault(); useItem(it); }
    }
  }
  function useItem(it: ItemView) {
    if (it.usable) act("use", { item: it.id });
    else if (it.slot) act(it.equipped ? "unequip" : "equip", { item: it.id });
  }

  // ---- rendering -------------------------------------------------------------
  const cam = { x: 0, y: 0, init: false };
  let raf = 0;
  function frame(now: number) {
    raf = requestAnimationFrame(frame);
    if (app.view !== "play" || !canvas) return;
    const dpr = window.devicePixelRatio || 1;
    const w = wrap.clientWidth, h = wrap.clientHeight;
    if (canvas.width !== Math.round(w * dpr) || canvas.height !== Math.round(h * dpr)) {
      canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
      canvas.style.width = w + "px"; canvas.style.height = h + "px";
    }
    const ctx = canvas.getContext("2d")!;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.fillStyle = "#07080c";
    ctx.fillRect(0, 0, w, h);
    if (!snap) return;
    const s = snap;
    const pp = s.player ? pos(s.player.id, now) : null;
    const tx = (pp?.x ?? s.width / 2) + 0.5, ty = (pp?.y ?? s.height / 2) + 0.5;
    if (!cam.init) { cam.x = tx; cam.y = ty; cam.init = true; }
    cam.x += (tx - cam.x) * 0.25; cam.y += (ty - cam.y) * 0.25;
    const ox = w / 2 - cam.x * tile, oy = h / 2 - cam.y * tile;
    const x0 = Math.max(0, Math.floor(-ox / tile)), x1 = Math.min(s.width - 1, Math.ceil((w - ox) / tile));
    const y0 = Math.max(0, Math.floor(-oy / tile)), y1 = Math.min(s.height - 1, Math.ceil((h - oy) / tile));
    ctx.font = `${Math.round(tile * 0.7)}px ui-monospace, monospace`;
    ctx.textAlign = "center"; ctx.textBaseline = "middle";

    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      const i = y * s.width + x, fog = s.fog[i];
      if (!fog) continue;
      const def = s.tile_defs[s.tiles[i]];
      const px = Math.round(ox + x * tile), py = Math.round(oy + y * tile);
      const sp = spriteNow(def.sprite);
      if (sp) drawSprite(ctx, sp, px, py, tile, now);
      else {
        ctx.fillStyle = def.walkable ? "#232837" : "#4a5266"; ctx.fillRect(px, py, tile, tile);
        if (def.glyph && def.glyph !== " ") { ctx.fillStyle = def.color ?? "#8b94a7"; ctx.fillText(def.glyph, px + tile / 2, py + tile / 2 + 1); }
      }
      if (fog === 1) { ctx.fillStyle = "rgba(6,7,11,0.62)"; ctx.fillRect(px, py, tile, tile); }
      else {
        // soft light falloff around the player
        const d = Math.hypot(x - (pp?.x ?? x), y - (pp?.y ?? y));
        const a = Math.min(0.5, Math.max(0, (d - 3) * 0.075));
        if (a > 0) { ctx.fillStyle = `rgba(6,7,11,${a})`; ctx.fillRect(px, py, tile, tile); }
      }
    }

    // items first, then monsters, player on top
    const order = (e: EntityView) => (e.id === s.player?.id ? 2 : e.hp !== undefined ? 1 : 0);
    for (const e of [...s.entities].sort((a, b) => order(a) - order(b))) {
      const p = pos(e.id, now); if (!p) continue;
      const px = Math.round(ox + p.x * tile), py = Math.round(oy + p.y * tile);
      const bob = e.hp !== undefined ? Math.sin(now / 340 + e.id) * 1.2 : 0;
      const sp = spriteNow(e.sprite);
      if (e.hp === undefined) { ctx.fillStyle = "rgba(0,0,0,0.35)"; ctx.beginPath(); ctx.ellipse(px + tile / 2, py + tile * 0.82, tile * 0.28, tile * 0.1, 0, 0, 7); ctx.fill(); }
      if (sp) drawSprite(ctx, sp, px, py + bob, tile, now);
      else {
        ctx.fillStyle = e.color ?? (e.id === s.player?.id ? "#ffd166" : "#ef6f6c");
        ctx.fillText(e.glyph ?? "?", px + tile / 2, py + tile / 2 + bob);
      }
      if (e.hp !== undefined && e.max_hp && e.hp < e.max_hp && e.id !== s.player?.id) {
        const bw = tile * 0.7, bx = px + (tile - bw) / 2, by = py - 3;
        ctx.fillStyle = "rgba(0,0,0,0.65)"; ctx.fillRect(bx - 1, by - 1, bw + 2, 6);
        ctx.fillStyle = "#e5484d"; ctx.fillRect(bx, by, bw * Math.max(0, e.hp / e.max_hp), 4);
      }
    }
  }
  $effect(() => { raf = requestAnimationFrame(frame); return () => cancelAnimationFrame(raf); });

  // ---- HUD helpers -------------------------------------------------------------
  const hpPct = $derived(snap && snap.max_hp ? Math.max(0, Math.min(100, (snap.hp / snap.max_hp) * 100)) : 0);
  const underfoot = $derived(snap?.player ? snap.entities.filter((e) => e.id !== snap!.player!.id && e.x === snap!.player!.x && e.y === snap!.player!.y && e.hp === undefined) : []);
  const sel = $derived(snap?.inventory.find((i) => i.id === selected));
  let logEl = $state<HTMLDivElement>();
  $effect(() => { snap?.log.length; if (logEl) logEl.scrollTop = logEl.scrollHeight; });
  function tone(line: string) {
    if (/you die|dies\./i.test(line) && /you die/i.test(line)) return "bad";
    if (/hits you|poison/i.test(line)) return "bad";
    if (/level|welcome/i.test(line)) return "gold";
    if (/feel better|\+\d/i.test(line)) return "good";
    if (/descend|stairs/i.test(line)) return "accent";
    return "";
  }

  function itemIcon(node: HTMLCanvasElement, it: ItemView) {
    const draw = () => {
      const ctx = node.getContext("2d")!; ctx.clearRect(0, 0, node.width, node.height);
      const sp = spriteNow(it.sprite);
      if (sp) drawSprite(ctx, sp, 0, 0, node.width, 0);
      else { ctx.fillStyle = it.color ?? "#ddd"; ctx.font = "26px monospace"; ctx.textAlign = "center"; ctx.textBaseline = "middle"; ctx.fillText(it.glyph ?? "?", node.width / 2, node.height / 2); }
    };
    draw();
    const t = setInterval(draw, 250);
    return { update(n: ItemView) { it = n; draw(); }, destroy() { clearInterval(t); } };
  }
</script>

<svelte:window onkeydown={onkey} />

<div class="play">
  <div class="stage" bind:this={wrap}>
    <canvas bind:this={canvas}></canvas>

    {#if error}
      <div class="overlay"><div class="card err">
        <h2><Icon name="x" size={18} /> Your game has an error</h2>
        <pre class="mono">{error}</pre>
        <div class="actions">
          <button class="btn" onclick={() => (app.view = "scripts")}><Icon name="code" size={14} /> Open scripts</button>
          <button class="btn" onclick={() => (app.view = "graph")}><Icon name="graph" size={14} /> Open graphs</button>
          <button class="btn primary" onclick={playNow}><Icon name="refresh" size={14} /> Try again</button>
        </div>
      </div></div>
    {:else if snap?.game_over}
      <div class="overlay death"><div class="card">
        <h2>You died</h2>
        <p class="dim">{snap.hud.map(([k, v]) => `${k} ${v}`).join("  ·  ")}</p>
        <button class="btn gold" onclick={playNow}><Icon name="refresh" size={14} /> New run</button>
      </div></div>
    {/if}

    <button class="btn sm conbtn" class:on={consoleOpen} onclick={() => (consoleOpen = !consoleOpen)} title="Lua console (`)"><Icon name="terminal" size={14} /> Console</button>
    {#if consoleOpen}
      <div class="console">
        <div class="clog mono" bind:this={consoleEl}>
          {#if !consoleLines.length}<div class="dim">Run Lua in the live game. Try <b>rogue.find_tagged("monster")</b> or <b>rogue.spawn("goblin", 10, 10)</b></div>{/if}
          {#each consoleLines as l}<div class="cl {l.kind}">{l.kind === "in" ? "› " : ""}{l.text}</div>{/each}
        </div>
        <!-- svelte-ignore a11y_autofocus -->
        <input class="cin mono" type="text" placeholder="Lua…  (Enter to run, ↑↓ history, Esc to close)" bind:value={consoleInput} onkeydown={consoleKey} spellcheck="false" autofocus />
      </div>
    {/if}

    {#if snap && !snap.game_over && underfoot.length}
      <div class="hint"><kbd>G</kbd> pick up {underfoot[0].name}</div>
    {/if}
  </div>

  <aside>
    {#if snap}
      <div class="panel block">
        <div class="hp">
          <div class="hp-top"><span><Icon name="heart" size={14} /> Health</span><strong class="mono">{snap.hp} / {snap.max_hp}</strong></div>
          <div class="bar"><i style="width:{hpPct}%" class:low={hpPct < 30}></i></div>
        </div>
        <div class="stats">
          {#each snap.hud as [k, v]}<div class="stat"><span class="dim">{k}</span><strong class="mono">{v}</strong></div>{/each}
        </div>
      </div>

      <div class="panel block">
        <div class="panel-title">Inventory <span class="mono">{snap.inventory.length}</span></div>
        <div class="grid">
          {#each snap.inventory as it, i (it.id)}
            <button class="slot" class:eq={it.equipped} class:sel={selected === it.id} title={it.name}
              onclick={() => (selected = selected === it.id ? null : it.id)} ondblclick={() => useItem(it)}>
              <canvas width="40" height="40" use:itemIcon={it}></canvas>
              <kbd>{i + 1}</kbd>{#if it.equipped}<b>E</b>{/if}
            </button>
          {/each}
          {#each Array(Math.max(0, 8 - snap.inventory.length)) as _}<div class="slot empty"></div>{/each}
        </div>
        {#if sel}
          <div class="selbar">
            <strong>{sel.name}</strong>
            <div class="btns">
              {#if sel.usable}<button class="btn sm primary" onclick={() => act("use", { item: sel.id })}>Use</button>{/if}
              {#if sel.slot}<button class="btn sm" onclick={() => act(sel.equipped ? "unequip" : "equip", { item: sel.id })}>{sel.equipped ? "Unequip" : "Equip " + sel.slot}</button>{/if}
              <button class="btn sm danger" onclick={() => act("drop", { item: sel.id })}>Drop</button>
            </div>
          </div>
        {:else if snap.inventory.length}
          <div class="dim tip">Click an item • double-click or press its number to use/equip</div>
        {/if}
      </div>

      <div class="panel block log-block">
        <div class="panel-title">Messages</div>
        <div class="log" bind:this={logEl}>
          {#each snap.log as line, i}<div class="line {tone(line)}" style="opacity:{0.45 + 0.55 * ((i + 1) / snap.log.length) ** 2}">{line}</div>{/each}
        </div>
      </div>

      <div class="keys dim">
        <span><kbd>↑↓←→</kbd>/<kbd>WASD</kbd> move &amp; attack</span>
        <span><kbd>G</kbd> pick up</span><span><kbd>&gt;</kbd> stairs</span><span><kbd>.</kbd> wait</span>
        <span class="zoom"><button class="btn ghost icon sm" onclick={() => (tile = Math.max(24, tile - 8))}>−</button>zoom<button class="btn ghost icon sm" onclick={() => (tile = Math.min(96, tile + 8))}>+</button></span>
      </div>
    {:else if !error}
      <div class="empty"><span class="dim">Starting game…</span></div>
    {/if}
  </aside>
</div>

<style>
  .play { flex: 1; display: flex; min-width: 0; }
  .stage { flex: 1; position: relative; min-width: 0; background: #07080c; overflow: hidden; }
  canvas { display: block; }
  aside { width: 330px; flex: none; background: var(--bg-1); border-left: 1px solid var(--line); padding: 12px; display: flex; flex-direction: column; gap: 10px; overflow: auto; }
  .block { padding: 12px; display: flex; flex-direction: column; gap: 10px; }
  .hp-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
  .hp-top span { display: flex; gap: 6px; align-items: center; color: var(--text-2); } .hp-top :global(svg) { color: #ef6f6c; }
  .bar { height: 12px; background: var(--bg-3); border-radius: 99px; overflow: hidden; border: 1px solid var(--line-2); }
  .bar i { display: block; height: 100%; background: linear-gradient(90deg, #d93f4a, #ff7a85); border-radius: 99px; transition: width 0.25s; box-shadow: 0 0 12px rgba(255, 90, 100, 0.5); }
  .bar i.low { animation: pulse 0.9s infinite alternate; }
  @keyframes pulse { to { filter: brightness(1.5); } }
  .stats { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px; }
  .stat { background: var(--bg-2); border: 1px solid var(--line); border-radius: 8px; padding: 6px 8px; display: flex; flex-direction: column; gap: 1px; }
  .stat span { font-size: 10px; text-transform: uppercase; letter-spacing: 0.06em; }
  .grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 6px; }
  .slot { position: relative; aspect-ratio: 1; background: var(--bg-2); border: 1px solid var(--line-2); border-radius: 9px; display: grid; place-items: center; padding: 0; transition: border-color 0.1s, transform 0.08s; }
  .slot:hover:not(.empty) { border-color: var(--accent); transform: translateY(-1px); }
  .slot.empty { border-style: dashed; border-color: var(--line); background: transparent; pointer-events: none; }
  .slot.sel { border-color: var(--accent); box-shadow: 0 0 0 2px var(--accent-bg); }
  .slot.eq { border-color: var(--gold); background: rgba(245, 185, 66, 0.08); }
  .slot canvas { width: 80%; height: 80%; image-rendering: pixelated; }
  .slot kbd { position: absolute; top: 2px; left: 3px; font-size: 9px; padding: 0 4px; border-bottom-width: 1px; opacity: 0.7; }
  .slot b { position: absolute; bottom: 2px; right: 4px; font-size: 10px; color: var(--gold); }
  .selbar { display: flex; flex-direction: column; gap: 8px; background: var(--bg-2); border: 1px solid var(--line); border-radius: 9px; padding: 9px; }
  .btns { display: flex; gap: 6px; flex-wrap: wrap; }
  .tip { font-size: 11px; }
  .log-block { flex: 1; min-height: 120px; }
  .log { flex: 1; overflow: auto; display: flex; flex-direction: column; gap: 3px; font-size: 12.5px; min-height: 0; }
  .line.bad { color: #ff8b8b; } .line.good { color: var(--good); } .line.gold { color: var(--gold); font-weight: 600; } .line.accent { color: var(--accent-2); }
  .keys { display: flex; flex-wrap: wrap; gap: 6px 12px; font-size: 11px; align-items: center; padding: 2px 4px; }
  .zoom { margin-left: auto; display: flex; align-items: center; gap: 2px; }
  .overlay { position: absolute; inset: 0; display: grid; place-items: center; background: rgba(5, 6, 10, 0.72); backdrop-filter: blur(3px); animation: fade 0.3s; }
  .overlay.death { background: radial-gradient(circle, rgba(60, 8, 12, 0.6), rgba(5, 6, 10, 0.88)); }
  @keyframes fade { from { opacity: 0; } }
  .card { background: var(--bg-2); border: 1px solid var(--line-2); border-radius: 16px; padding: 24px 28px; text-align: center; box-shadow: var(--shadow); max-width: min(680px, 90%); display: flex; flex-direction: column; align-items: center; gap: 12px; }
  .card h2 { margin: 0; font-size: 28px; letter-spacing: -0.02em; display: flex; gap: 8px; align-items: center; }
  .card.err h2 { font-size: 18px; color: var(--bad); }
  .card pre { margin: 0; text-align: left; background: var(--bg); padding: 12px; border-radius: 8px; max-height: 240px; overflow: auto; font-size: 12px; white-space: pre-wrap; width: 100%; color: #ffb3b3; }
  .actions { display: flex; gap: 8px; flex-wrap: wrap; justify-content: center; }
  .conbtn { position: absolute; top: 10px; right: 10px; background: rgba(15, 18, 24, 0.85); }
  .console { position: absolute; left: 12px; right: 12px; bottom: 12px; height: 230px; display: flex; flex-direction: column; background: rgba(11, 13, 18, 0.94); border: 1px solid var(--line-2); border-radius: 12px; box-shadow: var(--shadow); backdrop-filter: blur(6px); overflow: hidden; }
  .clog { flex: 1; overflow: auto; padding: 10px 12px; font-size: 12px; display: flex; flex-direction: column; gap: 2px; }
  .cl.in { color: var(--accent-2); } .cl.ok { color: #9ee07a; white-space: pre-wrap; } .cl.err { color: #ff8b8b; white-space: pre-wrap; }
  .cin { border: 0; border-top: 1px solid var(--line); border-radius: 0; background: transparent; padding: 10px 12px; }
  .hint { position: absolute; left: 50%; bottom: 22px; transform: translateX(-50%); background: rgba(15, 18, 24, 0.9); border: 1px solid var(--line-2); padding: 6px 12px; border-radius: 99px; display: flex; gap: 8px; align-items: center; }
</style>
