export type Json = null | boolean | number | string | Json[] | { [k: string]: Json };

export interface SpriteInfo { name: string; width: number; height: number; frames: number; fps: number }
export interface Def { [k: string]: any }
export interface ObjectSet { entities: Record<string, Def>; tiles: Record<string, Def> }
export interface ProjectInfo {
  name: string;
  dir: string;
  sprites: SpriteInfo[];
  graphs: string[];
  scripts: string[];
  objects: ObjectSet;
  base: ObjectSet;
}

export interface TileDef { id: string; glyph?: string; color?: string; sprite?: string; walkable: boolean; transparent: boolean; name: string }
export interface EntityView { id: number; x: number; y: number; name: string; kind: string; glyph?: string; sprite?: string; color?: string; hp?: number; max_hp?: number }
export interface ItemView { id: number; name: string; glyph?: string; sprite?: string; color?: string; slot?: string; equipped: boolean; usable: boolean }
export interface Snapshot {
  type: "state";
  width: number; height: number;
  tile_defs: TileDef[]; tiles: number[]; fog: number[];
  entities: EntityView[]; player?: EntityView;
  hp: number; max_hp: number;
  inventory: ItemView[]; log: string[]; hud: [string, string][];
  game_over: boolean;
}

export interface Pin { name: string; ty: string; default?: unknown; expr?: string }
export interface NodeDef { type: string; kind: "event" | "flow" | "data"; title: string; category: string; inputs: Pin[]; outputs: Pin[]; template: string }
export interface GraphNode { id: number; type: string; props: Record<string, unknown>; x: number; y: number }
export interface GraphEdge { from: { node: number; pin: string }; to: { node: number; pin: string } }
export interface Graph { nodes: GraphNode[]; edges: GraphEdge[] }

export interface SpriteView { name: string; width: number; height: number; frame: number; frame_count: number; pixels: number[]; fps: number }
