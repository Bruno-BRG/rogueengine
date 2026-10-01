import { engine, type Snapshot } from "./api";

const CELL = 18;
const KEYS: Record<string, [string, Record<string, number>]> = {
  ArrowUp: ["move", { dx: 0, dy: -1 }], k: ["move", { dx: 0, dy: -1 }],
  ArrowDown: ["move", { dx: 0, dy: 1 }], j: ["move", { dx: 0, dy: 1 }],
  ArrowLeft: ["move", { dx: -1, dy: 0 }], h: ["move", { dx: -1, dy: 0 }],
  ArrowRight: ["move", { dx: 1, dy: 0 }], l: ["move", { dx: 1, dy: 0 }],
  y: ["move", { dx: -1, dy: -1 }], u: ["move", { dx: 1, dy: -1 }],
  b: ["move", { dx: -1, dy: 1 }], n: ["move", { dx: 1, dy: 1 }],
  ".": ["wait", {}], g: ["pickup", {}],
};

export function mountGame(root: HTMLElement, status: (s: string) => void) {
  root.innerHTML = `<div class="pane">
    <canvas id="game" tabindex="0"></canvas>
    <div class="side">
      <button id="new">New game</button>
      <div id="hp"></div>
      <strong>Inventory</strong><div id="inv"></div>
      <strong>Log</strong><div class="log" id="log"></div>
      <small>Arrows/hjklyubn move · . wait · g pick up</small>
    </div></div>`;
  const canvas = root.querySelector<HTMLCanvasElement>("#game")!;
  const ctx = canvas.getContext("2d")!;
  let snap: Snapshot | null = null;

  const apply = (r: Awaited<ReturnType<typeof engine>>) => {
    if (r.type === "error") return status(r.message);
    if (r.type === "state") { snap = r; status(r.game_over ? "Game over" : ""); draw(); }
  };

  function draw() {
    if (!snap) return;
    const s = snap;
    canvas.width = s.width * CELL; canvas.height = s.height * CELL;
    ctx.font = `${CELL - 2}px monospace`; ctx.textBaseline = "top";
    for (let y = 0; y < s.height; y++) for (let x = 0; x < s.width; x++) {
      const f = s.fog[y * s.width + x];
      if (!f) continue;
      const def = s.tile_defs[s.tiles[y * s.width + x]];
      ctx.fillStyle = f === 2 ? (def.walkable ? "#2a2f38" : "#555c68") : "#16181d";
      ctx.fillRect(x * CELL, y * CELL, CELL, CELL);
      ctx.fillStyle = f === 2 ? "#9aa3b2" : "#3a404b";
      ctx.fillText(def.glyph ?? "?", x * CELL + 3, y * CELL + 1);
    }
    for (const e of s.entities) {
      ctx.fillStyle = e.id === s.player?.id ? "#ffd166" : e.kind === "player" ? "#ffd166" : "#ef6f6c";
      ctx.fillText(e.glyph ?? "?", e.x * CELL + 3, e.y * CELL + 1);
    }
    root.querySelector("#hp")!.textContent = `HP ${s.hp}/${s.max_hp}`;
    const inv = root.querySelector("#inv")!;
    inv.replaceChildren(...s.inventory.map((it) => {
      const row = document.createElement("div");
      row.className = "item" + (it.equipped ? " eq" : "");
      row.append(`${it.glyph ?? "?"} ${it.name}`);
      const act = async (action: string) => apply(await engine({ type: "act", action, arg: { item: it.id } }));
      if (it.usable) { const b = document.createElement("button"); b.textContent = "use"; b.onclick = () => act("use"); row.append(b); }
      const e = document.createElement("button"); e.textContent = "equip"; e.onclick = () => act("equip"); row.append(e);
      const d = document.createElement("button"); d.textContent = "drop"; d.onclick = () => act("drop"); row.append(d);
      return row;
    }));
    const log = root.querySelector("#log")!;
    log.textContent = s.log.join("\n"); log.scrollTop = log.scrollHeight;
  }

  root.querySelector("#new")!.addEventListener("click", async () => {
    apply(await engine({ type: "new_game", seed: Date.now() % 1e9 }));
    canvas.focus();
  });
  canvas.addEventListener("keydown", async (ev) => {
    const k = KEYS[ev.key];
    if (!k || !snap || snap.game_over) return;
    ev.preventDefault();
    apply(await engine({ type: "act", action: k[0], arg: k[1] }));
  });
}
