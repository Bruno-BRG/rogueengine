import { call } from "./api";

export interface LoadedSprite {
  img: HTMLImageElement;
  frames: number;
  fps: number;
  w: number;
  h: number;
}

const cache = new Map<string, Promise<LoadedSprite | null>>();
const ready = new Map<string, LoadedSprite | null>();

export function clearSpriteCache() {
  cache.clear();
  ready.clear();
}
/** Forget one sprite (after it was edited) so the next draw re-fetches it. */
export function invalidateSprite(name: string) {
  cache.delete(name);
  ready.delete(name);
}

export function loadSprite(name: string): Promise<LoadedSprite | null> {
  let p = cache.get(name);
  if (!p) {
    p = call<{ png: string; frames: number; fps: number; width: number; height: number }>("sprite_sheet", { name })
      .then(
        (s) =>
          new Promise<LoadedSprite | null>((resolve) => {
            const img = new Image();
            img.onload = () => resolve({ img, frames: s.frames, fps: s.fps, w: s.width, h: s.height });
            img.onerror = () => resolve(null);
            img.src = "data:image/png;base64," + s.png;
          }),
      )
      .catch(() => null);
    cache.set(name, p);
  }
  return p;
}

/** Synchronous lookup once loaded (renderers call this every frame). */
export function spriteNow(name: string | undefined): LoadedSprite | null {
  if (!name) return null;
  if (ready.has(name)) return ready.get(name)!;
  ready.set(name, null);
  loadSprite(name).then((s) => ready.set(name, s));
  return null;
}
export function resetReady() {
  ready.clear();
}

export function drawSprite(ctx: CanvasRenderingContext2D, s: LoadedSprite, x: number, y: number, size: number, timeMs: number) {
  const frame = s.frames > 1 ? Math.floor((timeMs / 1000) * s.fps) % s.frames : 0;
  ctx.imageSmoothingEnabled = false;
  ctx.drawImage(s.img, frame * s.w, 0, s.w, s.h, x, y, size, size * (s.h / s.w));
}
