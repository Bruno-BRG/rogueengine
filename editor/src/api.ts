import { invoke } from "@tauri-apps/api/core";

export type TileDef = { id: string; glyph?: string; walkable: boolean; transparent: boolean; name: string; sprite?: string };
export type EntityView = { id: number; x: number; y: number; name: string; kind: string; glyph?: string; sprite?: string };
export type ItemView = { id: number; name: string; glyph?: string; equipped: boolean; usable: boolean };
export type Snapshot = {
  width: number; height: number; tile_defs: TileDef[]; tiles: number[]; fog: number[];
  entities: EntityView[]; player?: EntityView; hp: number; max_hp: number;
  inventory: ItemView[]; log: string[]; game_over: boolean;
};
export type Pin = { name: string; ty: string; default?: unknown; expr?: string };
export type NodeDef = { type: string; kind: "event" | "flow" | "data"; title: string; category: string; inputs: Pin[]; outputs: Pin[]; template: string };
export type GraphNode = { id: number; type: string; props: Record<string, unknown>; x: number; y: number };
export type GraphEdge = { from: { node: number; pin: string }; to: { node: number; pin: string } };
export type Graph = { nodes: GraphNode[]; edges: GraphEdge[] };
export type Response =
  | { type: "state" } & Snapshot
  | { type: "lua"; code: string }
  | { type: "nodes"; nodes: NodeDef[] }
  | { type: "mods"; mods: { id: string; name: string; version: string }[] }
  | { type: "ok" }
  | { type: "error"; message: string };
export type SpriteView = { name: string; width: number; height: number; frame: number; frame_count: number; pixels: number[] };

export async function engine(request: Record<string, unknown>): Promise<Response> {
  return (await invoke("engine_request", { request })) as Response;
}
export const spriteNew = (name: string, width: number, height: number) =>
  invoke<SpriteView>("sprite_new", { name, width, height });
export const spriteApply = (op: Record<string, unknown>) => invoke<SpriteView>("sprite_apply", { op });
export const spriteExportPng = () => invoke<number[]>("sprite_export_png");
