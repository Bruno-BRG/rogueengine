<script lang="ts">
  import { app } from "../lib/app.svelte";
  import Icon from "./Icon.svelte";
</script>

<div class="toasts">
  {#each app.toasts as t (t.id)}
    <div class="toast {t.kind}">
      <Icon name={t.kind === "error" ? "x" : t.kind === "success" ? "check" : "zap"} size={14} />
      <span>{t.text}</span>
    </div>
  {/each}
</div>

<style>
  .toasts { position: fixed; right: 18px; bottom: 18px; display: flex; flex-direction: column; gap: 8px; z-index: 1000; max-width: 460px; }
  .toast {
    display: flex; gap: 9px; align-items: flex-start; padding: 10px 14px; border-radius: var(--radius);
    background: var(--bg-3); border: 1px solid var(--line-2); box-shadow: var(--shadow);
    animation: in 0.18s ease-out; word-break: break-word;
  }
  .toast :global(svg) { margin-top: 2px; flex: none; }
  .toast.error { border-color: rgba(248, 113, 113, 0.6); } .toast.error :global(svg) { color: var(--bad); }
  .toast.success :global(svg) { color: var(--good); }
  .toast.info :global(svg) { color: var(--accent-2); }
  @keyframes in { from { transform: translateY(8px); opacity: 0; } }
</style>
