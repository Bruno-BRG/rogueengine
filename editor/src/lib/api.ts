import type { Snapshot } from "./types";

const isTauri = typeof window !== "undefined" && "__TAURI_INTERNALS__" in window;

/** One backend entry point. Tauri: `invoke("api")`. Browser: POST /api/<cmd> to `rogue-server`. */
export async function call<T = any>(cmd: string, args: Record<string, unknown> = {}): Promise<T> {
  if (isTauri) {
    const { invoke } = await import("@tauri-apps/api/core");
    try {
      return (await invoke("api", { cmd, args })) as T;
    } catch (e) {
      throw new Error(String(e));
    }
  }
  let res: Response;
  try {
    res = await fetch(`/api/${cmd}`, { method: "POST", body: JSON.stringify(args) });
  } catch {
    throw new Error("Cannot reach the RogueEngine backend. Start it with: cargo run -p rogue-studio --bin rogue-server");
  }
  const j = await res.json();
  if ("err" in j) throw new Error(j.err);
  return j.ok as T;
}

export type EngineResponse =
  | Snapshot
  | { type: "error"; message: string }
  | { type: "ok" }
  | { type: "nodes"; nodes: import("./types").NodeDef[] }
  | { type: "mods"; mods: { id: string; name: string; version: string }[] };

export const engine = (request: Record<string, unknown>) => call<EngineResponse>("engine", { request });
