<script lang="ts">
  import { app, closeProject, fail, playNow, type View } from "./lib/app.svelte";
  import Icon from "./ui/Icon.svelte";
  import Toasts from "./ui/Toasts.svelte";
  import Welcome from "./views/Welcome.svelte";
  import Play from "./views/Play.svelte";
  import SpriteEditor from "./views/SpriteEditor.svelte";
  import GraphEditor from "./views/GraphEditor.svelte";
  import Objects from "./views/Objects.svelte";
  import Scripts from "./views/Scripts.svelte";

  const tabs: { id: View; label: string; icon: string; key: string }[] = [
    { id: "play", label: "Play", icon: "play", key: "1" },
    { id: "graph", label: "Visual Script", icon: "graph", key: "2" },
    { id: "sprites", label: "Sprites", icon: "image", key: "3" },
    { id: "objects", label: "Objects", icon: "box", key: "4" },
    { id: "scripts", label: "Lua Scripts", icon: "code", key: "5" },
  ];

  function onkey(e: KeyboardEvent) {
    if (!app.project || e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement || (e.target as HTMLElement)?.closest?.(".cm-editor")) return;
    const t = tabs.find((t) => t.key === e.key && (e.altKey || e.ctrlKey || e.metaKey));
    if (t) { e.preventDefault(); app.view = t.id; }
    if (e.key === "F5") { e.preventDefault(); playNow(); }
  }
</script>

<svelte:window onkeydown={onkey} />

{#if !app.project}
  <Welcome />
{:else}
  <div class="shell">
    <header class="top">
      <button class="brand" onclick={() => closeProject().catch(fail)} title="Back to projects">
        <svg width="24" height="24" viewBox="0 0 32 32"><rect x="2" y="2" width="28" height="28" rx="7" fill="#151923" stroke="#8b7bff" stroke-width="2.4"/><path d="M9 21 16 8l7 13" fill="none" stroke="#f5b942" stroke-width="2.8" stroke-linejoin="round" stroke-linecap="round"/></svg>
      </button>
      <div class="proj"><strong>{app.project.name}</strong><span class="dim mono">{app.project.dir}</span></div>
      <nav>
        {#each tabs as t (t.id)}
          <button class="tab" class:on={app.view === t.id} onclick={() => (app.view = t.id)}>
            <Icon name={t.icon} size={15} />{t.label}
          </button>
        {/each}
      </nav>
      <span class="saved dim"><Icon name="check" size={13} /> Auto-saved</span>
      <button class="btn gold" onclick={playNow} title="Run the game (F5)"><Icon name="play" size={14} /> Play <kbd>F5</kbd></button>
    </header>
    <main>
      <!-- Views stay mounted so editors keep their state when you switch tabs. -->
      <section class:hidden={app.view !== "play"}><Play /></section>
      <section class:hidden={app.view !== "graph"}><GraphEditor /></section>
      <section class:hidden={app.view !== "sprites"}><SpriteEditor /></section>
      <section class:hidden={app.view !== "objects"}><Objects /></section>
      <section class:hidden={app.view !== "scripts"}><Scripts /></section>
    </main>
  </div>
{/if}
<Toasts />

<style>
  .shell { height: 100%; display: flex; flex-direction: column; }
  .top { display: flex; align-items: center; gap: 14px; padding: 0 14px; height: 52px; background: var(--bg-1); border-bottom: 1px solid var(--line); flex: none; }
  .brand { background: none; border: 0; padding: 4px; border-radius: 8px; display: grid; }
  .brand:hover { background: var(--bg-3); }
  .proj { display: flex; flex-direction: column; line-height: 1.2; min-width: 0; max-width: 260px; }
  .proj strong { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .proj span { font-size: 10px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  nav { display: flex; gap: 4px; margin: 0 auto; background: var(--bg-2); padding: 4px; border-radius: 12px; border: 1px solid var(--line); }
  .tab { display: flex; align-items: center; gap: 7px; background: none; border: 0; padding: 6px 13px; border-radius: 8px; color: var(--dim); font-weight: 500; transition: color 0.1s, background 0.1s; }
  .tab:hover { color: var(--text); }
  .tab.on { background: var(--bg-4); color: var(--text); box-shadow: 0 1px 3px rgba(0, 0, 0, 0.4); }
  .saved { display: flex; align-items: center; gap: 5px; font-size: 12px; }
  .saved :global(svg) { color: var(--good); }
  main { flex: 1; min-height: 0; position: relative; }
  section { position: absolute; inset: 0; display: flex; }
  section.hidden { display: none; }
  @media (max-width: 1000px) { .proj, .saved { display: none; } }
</style>
