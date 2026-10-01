<script lang="ts">
  import { call } from "../lib/api";
  import { createProject, openProject, recents, forgetRecent, fail } from "../lib/app.svelte";
  import Icon from "../ui/Icon.svelte";

  let name = $state("My Roguelike");
  let parent = $state("");
  let template = $state("dungeon");
  let openPath = $state("");
  let busy = $state(false);
  let list = $state(recents());

  call<{ projects_dir: string }>("app_info").then((i) => (parent = i.projects_dir)).catch(fail);

  async function create() {
    busy = true;
    try { await createProject(parent, name, template); } catch (e) { fail(e); } finally { busy = false; }
  }
  async function open(dir: string) {
    busy = true;
    try { await openProject(dir); } catch (e) { fail(e); } finally { busy = false; }
  }
</script>

<div class="welcome">
  <div class="glow"></div>
  <div class="hero">
    <div class="logo">
      <svg width="46" height="46" viewBox="0 0 32 32"><rect x="2" y="2" width="28" height="28" rx="7" fill="#151923" stroke="#8b7bff" stroke-width="2"/><path d="M9 21 16 8l7 13" fill="none" stroke="#f5b942" stroke-width="2.4" stroke-linejoin="round" stroke-linecap="round"/><circle cx="16" cy="22" r="2" fill="#8b7bff"/></svg>
      <span>RogueEngine</span>
    </div>
    <h1>Build your roguelike.<br /><em>Change anything.</em></h1>
    <p>Draw sprites, wire up logic with nodes or Lua, design monsters and items — then play it instantly. Every rule of the game is yours to rewrite.</p>

    <div class="cards">
      <form class="card" onsubmit={(e) => { e.preventDefault(); create(); }}>
        <h3><Icon name="plus" size={16} /> New project</h3>
        <div class="field"><label for="n">Game name</label><input id="n" type="text" bind:value={name} /></div>
        <div class="field"><label for="t">Start from</label>
          <select id="t" bind:value={template}>
            <option value="dungeon">Dungeon crawler (playable, with art)</option>
            <option value="empty">Empty project</option>
          </select>
        </div>
        <div class="field"><label for="l">Location</label><input id="l" type="text" bind:value={parent} spellcheck="false" /></div>
        <button class="btn primary" disabled={busy || !name.trim()}>Create &amp; play <Icon name="arrow" size={15} /></button>
      </form>

      <div class="card">
        <h3><Icon name="folder" size={16} /> Open project</h3>
        {#if list.length}
          <div class="list">
            {#each list as r (r.dir)}
              <button class="row" onclick={() => open(r.dir)}>
                <Icon name="layers" size={15} />
                <span class="grow"><strong>{r.name}</strong><br /><span class="dim mono" style="font-size:11px">{r.dir}</span></span>
                <span class="act btn ghost icon sm" role="button" tabindex="0" aria-label="Forget"
                  onclick={(e) => { e.stopPropagation(); forgetRecent(r.dir); list = recents(); }}
                  onkeydown={(e) => e.key === "Enter" && (forgetRecent(r.dir), (list = recents()))}><Icon name="x" size={13} /></span>
              </button>
            {/each}
          </div>
        {:else}
          <div class="dim">Projects you create or open show up here.</div>
        {/if}
        <div class="sep"></div>
        <form class="field" onsubmit={(e) => { e.preventDefault(); if (openPath.trim()) open(openPath.trim()); }}>
          <label for="o">Open by folder path</label>
          <div style="display:flex;gap:8px"><input id="o" type="text" bind:value={openPath} placeholder="/path/to/my-game" spellcheck="false" style="flex:1" /><button class="btn" disabled={busy || !openPath.trim()}>Open</button></div>
        </form>
      </div>
    </div>
  </div>
</div>

<style>
  .welcome { position: relative; height: 100%; overflow: auto; display: grid; place-items: center; padding: 40px 20px; background: radial-gradient(1200px 600px at 70% -10%, rgba(139, 123, 255, 0.16), transparent), radial-gradient(900px 500px at 0% 110%, rgba(245, 185, 66, 0.09), transparent), var(--bg); }
  .hero { position: relative; width: min(980px, 100%); display: flex; flex-direction: column; gap: 14px; }
  .logo { display: flex; align-items: center; gap: 12px; font-size: 18px; font-weight: 700; letter-spacing: 0.01em; }
  h1 { font-size: 46px; line-height: 1.05; margin: 14px 0 0; letter-spacing: -0.03em; font-weight: 750; }
  h1 em { font-style: normal; background: linear-gradient(90deg, var(--accent-2), var(--gold)); -webkit-background-clip: text; background-clip: text; color: transparent; }
  p { color: var(--text-2); font-size: 15px; max-width: 620px; margin: 0 0 14px; line-height: 1.6; }
  .cards { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
  @media (max-width: 800px) { .cards { grid-template-columns: 1fr; } h1 { font-size: 34px; } }
  .card { background: rgba(21, 25, 35, 0.8); backdrop-filter: blur(6px); border: 1px solid var(--line-2); border-radius: 16px; padding: 20px; display: flex; flex-direction: column; gap: 14px; box-shadow: var(--shadow); }
  h3 { margin: 0; font-size: 15px; display: flex; align-items: center; gap: 8px; }
  .card :global(.btn.primary) { padding: 9px 14px; }
</style>
