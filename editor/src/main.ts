import "./style.css";
import { mountConsole } from "./console";
import { mountGame } from "./game";
import { mountGraph } from "./graph";
import { mountSprite } from "./sprite";

const status = (s: string) => { document.querySelector("#status")!.textContent = s; };
const tabs: [string, (root: HTMLElement, status: (s: string) => void) => void][] = [
  ["Game", mountGame], ["Visual Script", mountGraph], ["Sprite Editor", mountSprite], ["Lua & Mods", mountConsole],
];
const view = document.querySelector<HTMLElement>("#view")!;
const nav = document.querySelector<HTMLElement>("#tabs")!;
const panes = tabs.map(([, mount]) => {
  const el = document.createElement("div");
  el.style.cssText = "display:none;flex:1;min-width:0";
  view.append(el); mount(el, status); return el;
});
tabs.forEach(([name], i) => {
  const b = document.createElement("button"); b.textContent = name;
  b.onclick = () => { panes.forEach((p, j) => (p.style.display = i === j ? "flex" : "none")); nav.querySelectorAll("button").forEach((x, j) => x.classList.toggle("on", i === j)); };
  nav.append(b);
});
(nav.querySelector("button") as HTMLButtonElement).click();
