import { engine } from "./api";

export function mountConsole(root: HTMLElement, status: (s: string) => void) {
  root.innerHTML = `<div class="pane">
    <div style="flex:1;display:flex;flex-direction:column;gap:8px">
      <strong>Lua console (runs inside the live game)</strong>
      <textarea id="code" style="flex:1" spellcheck="false">rogue.log("hello from Lua")</textarea>
      <div><button id="run">Run</button></div>
    </div>
    <div class="side"><strong>Loaded mods</strong><div id="mods"></div><button id="refresh">Refresh</button></div></div>`;
  const run = root.querySelector<HTMLButtonElement>("#run")!;
  run.onclick = async () => {
    const r = await engine({ type: "exec", code: root.querySelector<HTMLTextAreaElement>("#code")!.value });
    status(r.type === "error" ? r.message : "ok");
  };
  const refresh = async () => {
    const r = await engine({ type: "list_mods" });
    root.querySelector("#mods")!.textContent = r.type === "mods" ? r.mods.map((m) => `${m.name || m.id} ${m.version}`).join("\n") : r.type === "error" ? r.message : "";
    (root.querySelector("#mods") as HTMLElement).style.whiteSpace = "pre";
  };
  root.querySelector<HTMLButtonElement>("#refresh")!.onclick = refresh;
}
