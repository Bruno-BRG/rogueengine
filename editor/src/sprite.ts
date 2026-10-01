import { spriteApply, spriteExportPng, spriteNew, type SpriteView } from "./api";

const ZOOM = 20;

export function mountSprite(root: HTMLElement, status: (s: string) => void) {
  root.innerHTML = `<div class="pane">
    <canvas id="sp" width="320" height="320"></canvas>
    <div class="side">
      <div><input id="w" type="number" value="16" min="1" max="256" style="width:60px"> ×
           <input id="h" type="number" value="16" min="1" max="256" style="width:60px">
           <button id="new">New sprite</button></div>
      <div>Tool <select id="tool"><option>pencil</option><option>line</option><option>rect</option><option>fill</option><option>erase</option></select>
           <input id="color" type="color" value="#e0a030"></div>
      <div><button id="undo">Undo</button> <button id="redo">Redo</button> <button id="flip">Flip H</button></div>
      <div id="frames"></div>
      <div><button id="addf">+ Frame</button> <button id="dupf">Duplicate</button> <button id="delf">Delete</button></div>
      <button id="png">Export PNG sheet</button>
    </div></div>`;
  const $ = <T extends HTMLElement>(s: string) => root.querySelector<T>(s)!;
  const canvas = $<HTMLCanvasElement>("#sp");
  const ctx = canvas.getContext("2d")!;
  let view: SpriteView | null = null;
  let drag: { x: number; y: number } | null = null;

  const rgba = (): number[] => {
    const h = $<HTMLInputElement>("#color").value;
    return [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16), 255];
  };
  const run = async (op: Record<string, unknown>) => {
    try { view = await spriteApply(op); render(); } catch (e) { status(String(e)); }
  };
  function render() {
    if (!view) return;
    canvas.width = view.width * ZOOM; canvas.height = view.height * ZOOM;
    for (let y = 0; y < view.height; y++) for (let x = 0; x < view.width; x++) {
      ctx.fillStyle = (x + y) % 2 ? "#2b2f36" : "#23262c";
      ctx.fillRect(x * ZOOM, y * ZOOM, ZOOM, ZOOM);
      const i = (y * view.width + x) * 4, p = view.pixels;
      if (p[i + 3]) { ctx.fillStyle = `rgba(${p[i]},${p[i + 1]},${p[i + 2]},${p[i + 3] / 255})`; ctx.fillRect(x * ZOOM, y * ZOOM, ZOOM, ZOOM); }
    }
    $("#frames").replaceChildren(...Array.from({ length: view.frame_count }, (_, i) => {
      const b = document.createElement("button");
      b.textContent = String(i + 1); b.style.fontWeight = i === view!.frame ? "bold" : "normal";
      b.onclick = () => run({ op: "select_frame", index: i });
      return b;
    }));
  }
  const cell = (ev: MouseEvent) => {
    const r = canvas.getBoundingClientRect();
    return { x: Math.floor(((ev.clientX - r.left) / r.width) * view!.width), y: Math.floor(((ev.clientY - r.top) / r.height) * view!.height) };
  };
  const color = () => ($<HTMLSelectElement>("#tool").value === "erase" ? [0, 0, 0, 0] : rgba());

  canvas.onmousedown = (ev) => {
    if (!view) return;
    const c = cell(ev), tool = $<HTMLSelectElement>("#tool").value;
    if (tool === "fill") return void run({ op: "fill", ...c, color: color() });
    if (tool === "pencil" || tool === "erase") { drag = c; run({ op: "pixel", ...c, color: color() }); }
    else drag = c;
  };
  canvas.onmousemove = (ev) => {
    if (!view || !drag) return;
    const tool = $<HTMLSelectElement>("#tool").value;
    if (tool === "pencil" || tool === "erase") {
      const c = cell(ev); run({ op: "line", x0: drag.x, y0: drag.y, x1: c.x, y1: c.y, color: color() }); drag = c;
    }
  };
  canvas.onmouseup = (ev) => {
    if (!view || !drag) return;
    const c = cell(ev), tool = $<HTMLSelectElement>("#tool").value;
    if (tool === "line") run({ op: "line", x0: drag.x, y0: drag.y, x1: c.x, y1: c.y, color: color() });
    if (tool === "rect") run({ op: "rect", x0: drag.x, y0: drag.y, x1: c.x, y1: c.y, color: color(), filled: ev.shiftKey });
    drag = null;
  };
  canvas.onmouseleave = () => { drag = null; };

  $("#new").onclick = async () => {
    try {
      view = await spriteNew("sprite", +$<HTMLInputElement>("#w").value, +$<HTMLInputElement>("#h").value);
      render();
    } catch (e) { status(String(e)); }
  };
  $("#undo").onclick = () => run({ op: "undo" });
  $("#redo").onclick = () => run({ op: "redo" });
  $("#flip").onclick = () => run({ op: "flip_horizontal" });
  $("#addf").onclick = () => run({ op: "add_frame", duplicate: false });
  $("#dupf").onclick = () => run({ op: "add_frame", duplicate: true });
  $("#delf").onclick = () => run({ op: "remove_frame" });
  $("#png").onclick = async () => {
    try {
      const bytes = new Uint8Array(await spriteExportPng());
      const a = document.createElement("a");
      a.href = URL.createObjectURL(new Blob([bytes], { type: "image/png" }));
      a.download = `${view?.name ?? "sprite"}.png`; a.click();
    } catch (e) { status(String(e)); }
  };
}
