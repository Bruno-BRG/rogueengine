import { call } from "./api";
import type { ProjectInfo } from "./types";
import { clearSpriteCache } from "./sprites";

export type View = "play" | "graph" | "sprites" | "objects" | "scripts";

export interface Toast { id: number; kind: "info" | "error" | "success"; text: string }

export const app = $state({
  project: null as ProjectInfo | null,
  view: "play" as View,
  toasts: [] as Toast[],
  /** bumped whenever sprites change so canvases know to redraw */
  spriteRev: 0,
  playRequest: 0,
  /** which asset each view has selected (survives tab switches) */
  selected: { graph: "", sprite: "", script: "", object: "" } as Record<string, string>,
});

let toastId = 1;
export function toast(text: string, kind: Toast["kind"] = "info") {
  const id = toastId++;
  app.toasts.push({ id, kind, text });
  setTimeout(() => (app.toasts = app.toasts.filter((t) => t.id !== id)), kind === "error" ? 7000 : 2800);
}

export function fail(e: unknown) {
  toast(e instanceof Error ? e.message : String(e), "error");
}

export async function refreshProject() {
  app.project = await call<ProjectInfo>("project_info");
}

export async function openProject(dir: string) {
  clearSpriteCache();
  app.project = await call<ProjectInfo>("project_open", { dir });
  rememberRecent(app.project.name, app.project.dir);
  app.view = "play";
}

export async function createProject(parent: string, name: string, template: string) {
  clearSpriteCache();
  app.project = await call<ProjectInfo>("project_create", { parent, name, template });
  rememberRecent(app.project.name, app.project.dir);
  app.view = "play";
}

export async function closeProject() {
  await call("project_close");
  app.project = null;
}

export function playNow() {
  app.view = "play";
  app.playRequest++;
}

export interface Recent { name: string; dir: string }
export function recents(): Recent[] {
  try {
    return JSON.parse(localStorage.getItem("rogue.recent") ?? "[]");
  } catch {
    return [];
  }
}
function rememberRecent(name: string, dir: string) {
  try {
    const list = [{ name, dir }, ...recents().filter((r) => r.dir !== dir)].slice(0, 8);
    localStorage.setItem("rogue.recent", JSON.stringify(list));
  } catch { /* storage unavailable */ }
}
export function forgetRecent(dir: string) {
  try { localStorage.setItem("rogue.recent", JSON.stringify(recents().filter((r) => r.dir !== dir))); } catch { /* ignore */ }
}

const pendingSaves = new Set<() => Promise<void>>();

/** Wait for every autosave that is scheduled but not yet written (call before running the game). */
export async function flushSaves() {
  await Promise.all([...pendingSaves].map((f) => f()));
}

/** Debounce helper for autosave. Pending runs can be forced with `flush` or `flushSaves()`. */
export function debounced<A extends unknown[]>(fn: (...a: A) => void | Promise<void>, ms: number) {
  let t: ReturnType<typeof setTimeout> | undefined;
  let args: A | null = null;
  const flush = async (): Promise<void> => {
    clearTimeout(t);
    if (!args) return;
    const a = args; args = null;
    pendingSaves.delete(flush);
    await fn(...a);
  };
  const run = (...a: A) => {
    args = a;
    pendingSaves.add(flush);
    clearTimeout(t);
    t = setTimeout(() => { flush().catch(fail); }, ms);
  };
  run.flush = flush;
  return run;
}
