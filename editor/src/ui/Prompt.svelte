<script lang="ts">
  import Modal from "./Modal.svelte";
  let { title, label, initial = "", confirm = "Create", onsubmit, onclose }: {
    title: string; label: string; initial?: string; confirm?: string;
    onsubmit: (v: string) => void; onclose: () => void;
  } = $props();
  // svelte-ignore state_referenced_locally
  let value = $state(initial);
  const ok = $derived(/^[A-Za-z0-9_-]+$/.test(value));
</script>

<Modal {title} {onclose} width={380}>
  <form class="field" onsubmit={(e) => { e.preventDefault(); if (ok) onsubmit(value); }}>
    <label for="p">{label}</label>
    <!-- svelte-ignore a11y_autofocus -->
    <input id="p" type="text" bind:value autofocus autocomplete="off" spellcheck="false" />
    <span class="dim" style="font-size:11px">Letters, digits, <code>_</code> and <code>-</code> only.</span>
    <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:10px">
      <button type="button" class="btn ghost" onclick={onclose}>Cancel</button>
      <button class="btn primary" disabled={!ok}>{confirm}</button>
    </div>
  </form>
</Modal>
