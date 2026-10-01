<script lang="ts">
  import type { Snippet } from "svelte";
  import Icon from "./Icon.svelte";
  let { title, onclose, children, width = 440 }: { title: string; onclose: () => void; children: Snippet; width?: number } = $props();
</script>

<svelte:window onkeydown={(e) => e.key === "Escape" && onclose()} />
<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
<div class="backdrop" onmousedown={(e) => e.target === e.currentTarget && onclose()}>
  <div class="modal" style="width:{width}px" role="dialog" aria-label={title}>
    <header><strong>{title}</strong><button class="btn ghost icon" onclick={onclose} aria-label="Close"><Icon name="x" /></button></header>
    <div class="body">{@render children()}</div>
  </div>
</div>

<style>
  .backdrop { position: fixed; inset: 0; background: rgba(5, 6, 10, 0.65); backdrop-filter: blur(3px); display: grid; place-items: center; z-index: 500; animation: fade 0.12s; }
  .modal { max-width: 94vw; background: var(--bg-2); border: 1px solid var(--line-2); border-radius: 14px; box-shadow: var(--shadow); animation: pop 0.16s ease-out; }
  header { display: flex; align-items: center; justify-content: space-between; padding: 14px 16px 6px; font-size: 15px; }
  .body { padding: 8px 16px 16px; display: flex; flex-direction: column; gap: 12px; }
  @keyframes fade { from { opacity: 0; } }
  @keyframes pop { from { transform: scale(0.97) translateY(6px); opacity: 0; } }
</style>
