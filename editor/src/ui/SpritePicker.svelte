<script lang="ts">
  import { app } from "../lib/app.svelte";
  import SpriteThumb from "./SpriteThumb.svelte";
  import Icon from "./Icon.svelte";

  let { value, onchange }: { value?: string; onchange: (v: string | undefined) => void } = $props();
  let open = $state(false);
  const sprites = $derived(app.project?.sprites ?? []);
  const missing = $derived(!!value && !sprites.some((s) => s.name === value));
</script>

<div class="pick">
  <button class="btn trigger" class:warn={missing} onclick={() => (open = !open)} type="button">
    <SpriteThumb name={missing ? undefined : value} size={26} />
    <span class="grow">{value ?? "No sprite"}{missing ? " (missing)" : ""}</span>
    <Icon name="chevron" size={14} />
  </button>
  {#if open}
    <!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
    <div class="veil" onmousedown={() => (open = false)}></div>
    <div class="pop">
      <button class="cell none" class:cur={!value} onclick={() => { onchange(undefined); open = false; }} type="button" title="No sprite (use glyph)"><Icon name="x" size={16} /></button>
      {#each sprites as s (s.name)}
        <button class="cell" class:cur={value === s.name} onclick={() => { onchange(s.name); open = false; }} type="button" title={s.name}>
          <SpriteThumb name={s.name} size={36} animate />
        </button>
      {/each}
      {#if !sprites.length}<div class="dim" style="grid-column:1/-1;padding:8px">No sprites yet — draw some in the Sprites tab.</div>{/if}
    </div>
  {/if}
</div>

<style>
  .pick { position: relative; }
  .trigger { width: 100%; justify-content: flex-start; padding: 4px 10px; }
  .trigger.warn { border-color: var(--warn); }
  .grow { flex: 1; text-align: left; overflow: hidden; text-overflow: ellipsis; }
  .veil { position: fixed; inset: 0; z-index: 40; }
  .pop { position: absolute; z-index: 50; top: calc(100% + 6px); left: 0; width: 290px; max-height: 280px; overflow: auto; display: grid; grid-template-columns: repeat(5, 1fr); gap: 6px; padding: 8px; background: var(--bg-2); border: 1px solid var(--line-2); border-radius: 12px; box-shadow: var(--shadow); }
  .cell { aspect-ratio: 1; display: grid; place-items: center; background: var(--bg-1); border: 1px solid var(--line); border-radius: 8px; padding: 0; color: var(--dim); }
  .cell:hover { border-color: var(--accent); } .cell.cur { border-color: var(--accent); background: var(--accent-bg); }
</style>
