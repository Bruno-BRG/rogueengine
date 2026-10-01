<script lang="ts">
  import type { NodeDef, Pin } from "../lib/types";
  import Icon from "./Icon.svelte";

  let { defs, from, x, y, onpick, onclose }: {
    defs: NodeDef[]; from: { pin: Pin; isOutput: boolean } | null; x: number; y: number;
    onpick: (d: NodeDef) => void; onclose: () => void;
  } = $props();

  let q = $state("");
  let idx = $state(0);

  const compat = (d: NodeDef) => {
    if (!from) return true;
    const pins = from.isOutput ? d.inputs : d.outputs;
    return pins.some((p) => (p.ty === "exec") === (from.pin.ty === "exec"));
  };
  const list = $derived(
    defs.filter(compat).filter((d) => {
      const s = q.trim().toLowerCase();
      return !s || `${d.title} ${d.type} ${d.category}`.toLowerCase().includes(s);
    }),
  );
  const grouped = $derived.by(() => {
    const m = new Map<string, NodeDef[]>();
    for (const d of list) m.set(d.category, [...(m.get(d.category) ?? []), d]);
    return [...m.entries()];
  });
  $effect(() => { q; idx = 0; });

  function key(e: KeyboardEvent) {
    if (e.key === "ArrowDown") { e.preventDefault(); idx = Math.min(list.length - 1, idx + 1); scroll(); }
    else if (e.key === "ArrowUp") { e.preventDefault(); idx = Math.max(0, idx - 1); scroll(); }
    else if (e.key === "Enter" && list[idx]) { e.preventDefault(); onpick(list[idx]); }
    else if (e.key === "Escape") onclose();
  }
  function scroll() { queueMicrotask(() => document.querySelector(".pal .hit")?.scrollIntoView({ block: "nearest" })); }
  const left = $derived(Math.min(x, window.innerWidth - 340));
  const top = $derived(Math.min(y, window.innerHeight - 440));
</script>

<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
<div class="veil" onmousedown={onclose}></div>
<div class="pal" style="left:{left}px;top:{top}px" role="dialog" aria-label="Add node">
  <div class="search"><Icon name="search" size={15} />
    <!-- svelte-ignore a11y_autofocus -->
    <input type="text" placeholder="Search nodes…" bind:value={q} onkeydown={key} autofocus spellcheck="false" />
  </div>
  <div class="items">
    {#each grouped as [cat, items]}
      <div class="cat">{cat}</div>
      {#each items as d}
        {@const i = list.indexOf(d)}
        <button class="item" class:hit={i === idx} onmouseenter={() => (idx = i)} onclick={() => onpick(d)}>
          <span class="dot {d.kind}"></span>{d.title || d.type}
        </button>
      {/each}
    {:else}
      <div class="none dim">No matching nodes</div>
    {/each}
  </div>
</div>

<style>
  .veil { position: fixed; inset: 0; z-index: 90; }
  .pal { position: fixed; z-index: 100; width: 320px; max-height: 420px; display: flex; flex-direction: column; background: var(--bg-2); border: 1px solid var(--line-2); border-radius: 12px; box-shadow: var(--shadow); animation: pop 0.12s ease-out; overflow: hidden; }
  .search { display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-bottom: 1px solid var(--line); color: var(--dim); }
  .search input { flex: 1; background: none; border: 0; padding: 0; box-shadow: none !important; font-size: 14px; color: var(--text); }
  .items { overflow: auto; padding: 6px; }
  .cat { font-size: 10px; font-weight: 700; letter-spacing: 0.09em; text-transform: uppercase; color: var(--dim); padding: 8px 8px 3px; }
  .item { display: flex; align-items: center; gap: 9px; width: 100%; text-align: left; padding: 6px 9px; background: none; border: 0; border-radius: 7px; color: var(--text-2); }
  .item.hit { background: var(--accent-bg); color: var(--text); }
  .dot { width: 8px; height: 8px; border-radius: 50%; background: var(--dim); }
  .dot.event { background: var(--gold); } .dot.flow { background: var(--accent); } .dot.data { background: #4ade80; }
  .none { padding: 20px; text-align: center; }
  @keyframes pop { from { transform: scale(0.97); opacity: 0; } }
</style>
