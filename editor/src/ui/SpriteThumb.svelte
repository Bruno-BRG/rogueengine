<script lang="ts">
  import { app } from "../lib/app.svelte";
  import { drawSprite, loadSprite } from "../lib/sprites";

  /** Draws a sprite (animated if `animate`) at a fixed size; a placeholder box if missing. */
  let { name, size = 28, animate = false }: { name?: string; size?: number; animate?: boolean } = $props();
  let c: HTMLCanvasElement;

  $effect(() => {
    app.spriteRev;
    const n = name;
    let raf = 0, alive = true;
    const ctx = c.getContext("2d")!;
    c.width = size * 2; c.height = size * 2;
    if (!n) { ctx.clearRect(0, 0, c.width, c.height); return; }
    loadSprite(n).then((s) => {
      if (!alive) return;
      const tick = (t: number) => {
        ctx.clearRect(0, 0, c.width, c.height);
        if (s) drawSprite(ctx, s, 0, 0, c.width, animate ? t : 0);
        if (animate && s && s.frames > 1 && alive) raf = requestAnimationFrame(tick);
      };
      tick(0);
    });
    return () => { alive = false; cancelAnimationFrame(raf); };
  });
</script>

<canvas bind:this={c} class="pixel" style="width:{size}px;height:{size}px"></canvas>
