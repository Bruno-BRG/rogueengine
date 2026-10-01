<script lang="ts">
  import Modal from "./Modal.svelte";
  let { title, text, confirm = "Delete", onyes, onclose }: { title: string; text: string; confirm?: string; onyes: () => void; onclose: () => void } = $props();
</script>

<Modal {title} {onclose} width={380}>
  <p style="margin:0;color:var(--text-2)">{text}</p>
  <div style="display:flex;justify-content:flex-end;gap:8px">
    <button class="btn ghost" onclick={onclose}>Cancel</button>
    <button class="btn danger" style="border-color:var(--bad);color:var(--bad)" onclick={() => { onyes(); onclose(); }}>{confirm}</button>
  </div>
</Modal>
