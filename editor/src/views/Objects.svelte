<script lang="ts">
  import { app, debounced, fail, refreshProject, toast, playNow } from "../lib/app.svelte";
  import { call } from "../lib/api";
  import type { Def } from "../lib/types";
  import Icon from "../ui/Icon.svelte";
  import Modal from "../ui/Modal.svelte";
  import Confirm from "../ui/Confirm.svelte";
  import SpritePicker from "../ui/SpritePicker.svelte";
  import SpriteThumb from "../ui/SpriteThumb.svelte";

  /** Deep copy that also works on Svelte's reactive proxies (structuredClone does not). */
  const clone = <T,>(v: T): T => JSON.parse(JSON.stringify(v));
  type Kind = "entities" | "tiles";
  let kind = $state<Kind>("entities");
  let q = $state("");
  let ents = $state<Record<string, Def>>({});
  let tiles = $state<Record<string, Def>>({});
  let selected = $state("");
  let showNew = $state(false);
  let confirmDel = $state(false);
  let newId = $state("");
  let newPreset = $state("monster");
  let tagInput = $state("");
  let customKey = $state("");
  let synced = "";

  // Pull project data in whenever the project changes underneath us (open/create).
  $effect(() => {
    const p = app.project;
    if (!p) return;
    const sig = p.dir;
    if (sig !== synced) {
      synced = sig;
      ents = clone(p.objects.entities);
      tiles = clone(p.objects.tiles);
      selected = "";
    }
  });

  const store = $derived(kind === "entities" ? ents : tiles);
  const base = $derived<Record<string, Def>>(app.project?.base[kind] ?? {});
  const ids = $derived([...new Set([...Object.keys(base), ...Object.keys(store)])].sort());
  const filtered = $derived(ids.filter((id) => { const s = q.trim().toLowerCase(); return !s || id.includes(s) || String((store[id] ?? base[id]).name ?? "").toLowerCase().includes(s); }));
  const isOverride = (id: string) => id in store && id in base;
  const isBuiltin = (id: string) => id in base && !(id in store);
  const yours = $derived(filtered.filter((id) => id in store && !(id in base)));
  const overrides = $derived(filtered.filter((id) => isOverride(id)));
  const builtins = $derived(filtered.filter((id) => isBuiltin(id)));
  const def = $derived<Def | undefined>(selected ? (store[selected] ?? base[selected]) : undefined);

  $effect(() => { kind; if (selected && !ids.includes(selected)) selected = ""; });

  const persist = debounced(async () => {
    await call("objects_save", { entities: $state.snapshot(ents), tiles: $state.snapshot(tiles) });
    await refreshProject();
  }, 500);

  /** Edit a definition. Built-ins are copied into the project first (an override). */
  function edit(fn: (d: Def) => void) {
    if (!selected) return;
    const target = kind === "entities" ? ents : tiles;
    if (!(selected in target)) target[selected] = clone(base[selected]);
    fn(target[selected]);
    persist();
  }
  function kindOf(d: Def): string {
    if (d.tags?.includes("player")) return "Player";
    if (d.tags?.includes("monster")) return "Monster";
    if (d.item?.slot) return "Equipment";
    if (d.item) return "Item";
    return "Object";
  }

  const PRESETS: Record<string, (name: string) => Def> = {
    monster: (name) => ({ name, glyph: "m", color: "#ef6f6c", blocks: true, stats: { hp: 8, max_hp: 8, attack: 3, defense: 0, speed: 100 }, tags: ["actor", "monster"], data: { sight: 8, xp: 5, spawn: { min_depth: 1, max_depth: 99, weight: 5 } } }),
    item: (name) => ({ name, glyph: "*", color: "#f5b942", item: {}, data: { spawn: { min_depth: 1, max_depth: 99, weight: 3, kind: "item" } } }),
    weapon: (name) => ({ name, glyph: "/", color: "#b8c4d6", item: { slot: "weapon", modifiers: { attack: 2 } }, data: { spawn: { min_depth: 1, max_depth: 99, weight: 3, kind: "item" } } }),
    armor: (name) => ({ name, glyph: "[", color: "#b8864b", item: { slot: "body", modifiers: { defense: 2 } }, data: { spawn: { min_depth: 1, max_depth: 99, weight: 3, kind: "item" } } }),
    potion: (name) => ({ name, glyph: "!", color: "#e5484d", item: { on_use: "heal" }, data: { heal_amount: 10, spawn: { min_depth: 1, max_depth: 99, weight: 6, kind: "item" } } }),
    blank: (name) => ({ name, glyph: "?", color: "#d8dce3", blocks: false }),
    floor: (name) => ({ name, glyph: ".", color: "#3b4252", walkable: true, transparent: true }),
    wall: (name) => ({ name, glyph: "#", color: "#6b7488", walkable: false, transparent: false }),
  };
  const titleCase = (s: string) => s.replace(/[-_]+/g, " ").replace(/\b\w/g, (c) => c.toUpperCase());

  function create() {
    const id = newId.trim();
    if ((kind === "entities" ? ents : tiles)[id] || base[id]) { toast(`“${id}” already exists`, "error"); return; }
    const preset = kind === "tiles" ? (newPreset === "wall" ? "wall" : "floor") : newPreset;
    (kind === "entities" ? ents : tiles)[id] = PRESETS[preset](titleCase(id));
    selected = id; showNew = false; newId = ""; persist();
  }
  function remove() {
    const id = selected;
    delete (kind === "entities" ? ents : tiles)[id];
    if (!(id in base)) selected = "";
    persist(); toast(id in base ? `Reset “${id}” to built-in` : `Deleted “${id}”`);
  }

  // ---- field helpers ---------------------------------------------------------
  const num = (v: string, d = 0) => (v === "" || isNaN(+v) ? d : +v);
  function setData(key: string, v: unknown) { edit((d) => { d.data ??= {}; if (v === undefined || v === "") delete d.data[key]; else d.data[key] = v; }); }
  function toggle(section: "stats" | "item" | "spawn", on: boolean) {
    edit((d) => {
      if (section === "stats") { if (on) d.stats ??= { hp: 10, max_hp: 10, attack: 1, defense: 0, speed: 100 }; else delete d.stats; }
      else if (section === "item") { if (on) d.item ??= {}; else delete d.item; }
      else { d.data ??= {}; if (on) d.data.spawn ??= { min_depth: 1, max_depth: 99, weight: 5, ...(d.item ? { kind: "item" } : {}) }; else delete d.data.spawn; }
    });
  }
  function addTag() {
    const t = tagInput.trim();
    if (t) edit((d) => { d.tags ??= []; if (!d.tags.includes(t)) d.tags.push(t); });
    tagInput = "";
  }
  const SLOTS = ["weapon", "body", "head", "hands", "feet", "ring", "amulet"];
  const STATS = ["attack", "defense", "max_hp", "speed"];
  const RESERVED = ["xp", "sight", "spawn", "heal_amount"];
  const customData = $derived(def?.data ? Object.entries(def.data).filter(([k]) => !RESERVED.includes(k)) : []);
  function setCustom(k: string, raw: string) {
    let v: unknown = raw;
    try { v = JSON.parse(raw); } catch { /* keep as string */ }
    setData(k, v);
  }
  const TAG_HELP: Record<string, string> = { actor: "takes turns", monster: "hostile to the player", player: "controlled by you" };
</script>

<div class="wrap">
  <aside class="left">
    <div class="seg">
      <button class:on={kind === "entities"} onclick={() => (kind = "entities")}><Icon name="sword" size={14} /> Entities</button>
      <button class:on={kind === "tiles"} onclick={() => (kind = "tiles")}><Icon name="grid" size={14} /> Tiles</button>
    </div>
    <div class="searchrow">
      <div class="search"><Icon name="search" size={14} /><input type="text" placeholder="Search…" bind:value={q} /></div>
      <button class="btn primary icon" title="New {kind === 'entities' ? 'entity' : 'tile'}" onclick={() => (showNew = true)}><Icon name="plus" size={15} /></button>
    </div>
    <div class="list scroll">
      {#each [["Your game", yours], ["Overrides", overrides], ["Built-in", builtins]] as [title, group]}
        {#if (group as string[]).length}
          <div class="grp">{title}</div>
          {#each group as string[] as id (id)}
            {@const d = store[id] ?? base[id]}
            <button class="row" class:sel={selected === id} onclick={() => (selected = id)}>
              <span class="ico">{#if d.sprite}<SpriteThumb name={d.sprite} size={26} />{:else}<span style="color:{d.color ?? '#aaa'}" class="mono">{d.glyph ?? "?"}</span>{/if}</span>
              <span class="grow">{d.name || id}</span>
              {#if kind === "entities"}<span class="badge {kindOf(d) === 'Monster' ? 'gold' : kindOf(d) === 'Player' ? 'accent' : ''}">{kindOf(d)}</span>{/if}
            </button>
          {/each}
        {/if}
      {/each}
    </div>
  </aside>

  <div class="main scroll">
    {#if def}
      <div class="form">
        <div class="hdr">
          <div class="big">
            {#if def.sprite}<SpriteThumb name={def.sprite} size={72} animate />{:else}<span class="mono" style="color:{def.color ?? '#aaa'};font-size:44px">{def.glyph ?? "?"}</span>{/if}
          </div>
          <div class="hf">
            <input class="title" type="text" value={def.name ?? ""} oninput={(e) => edit((d) => (d.name = e.currentTarget.value))} placeholder="Display name" />
            <div class="meta"><span class="mono dim">id: {selected}</span>
              {#if kind === "entities"}<span class="badge accent">{kindOf(def)}</span>{/if}
              {#if isBuiltin(selected)}<span class="badge">Built-in · editing makes an override</span>{/if}
              {#if isOverride(selected)}<span class="badge gold">Overrides built-in</span>{/if}</div>
          </div>
          <div class="hbtns">
            {#if isOverride(selected)}<button class="btn sm" onclick={() => (confirmDel = true)}><Icon name="refresh" size={13} /> Reset</button>
            {:else if !(selected in base)}<button class="btn sm danger" onclick={() => (confirmDel = true)}><Icon name="trash" size={13} /> Delete</button>{/if}
            <button class="btn sm gold" onclick={playNow}><Icon name="play" size={13} /> Test</button>
          </div>
        </div>

        <section>
          <h4>Appearance</h4>
          <div class="field-row">
            <div class="field"><label for="sp">Sprite</label><SpritePicker value={def.sprite} onchange={(v) => edit((d) => { if (v) d.sprite = v; else delete d.sprite; })} /></div>
            <div class="field" style="max-width:90px"><label for="gl">Glyph</label><input id="gl" type="text" maxlength="1" class="mono" value={def.glyph ?? ""} oninput={(e) => edit((d) => (d.glyph = e.currentTarget.value || undefined))} /></div>
            <div class="field" style="max-width:70px"><label for="co">Color</label><input id="co" type="color" value={def.color ?? "#d8dce3"} oninput={(e) => edit((d) => (d.color = e.currentTarget.value))} /></div>
          </div>
          <div class="hint">The sprite is used when available; the colored glyph is the fallback.</div>
        </section>

        {#if kind === "tiles"}
          <section>
            <h4>Behavior</h4>
            <label class="chk"><input type="checkbox" checked={def.walkable ?? true} onchange={(e) => edit((d) => (d.walkable = e.currentTarget.checked))} /> Walkable <span class="dim">— creatures can stand here</span></label>
            <label class="chk"><input type="checkbox" checked={def.transparent ?? true} onchange={(e) => edit((d) => (d.transparent = e.currentTarget.checked))} /> Transparent <span class="dim">— doesn’t block line of sight</span></label>
          </section>
        {:else}
          <section>
            <h4>Behavior</h4>
            <label class="chk"><input type="checkbox" checked={!!def.blocks} onchange={(e) => edit((d) => (d.blocks = e.currentTarget.checked))} /> Blocks movement <span class="dim">— others can’t walk through it</span></label>
            <div class="label" style="margin-top:6px">Tags</div>
            <div class="chips">
              {#each def.tags ?? [] as t}
                <span class="chip" title={TAG_HELP[t] ?? ""}>{t}<button class="x" aria-label="Remove tag {t}" onclick={() => edit((d) => (d.tags = d.tags.filter((x: string) => x !== t)))}><Icon name="x" size={11} /></button></span>
              {/each}
              <form onsubmit={(e) => { e.preventDefault(); addTag(); }}><input class="tagin" type="text" placeholder="add tag…" bind:value={tagInput} list="taglist" /></form>
              <datalist id="taglist"><option value="actor">takes turns</option><option value="monster">hostile</option><option value="player"></option></datalist>
            </div>
            <div class="hint"><b>actor</b> = takes turns · <b>monster</b> = hostile AI · <b>player</b> = you. Any other tag is yours to use in scripts.</div>
          </section>

          <section>
            <h4><label class="chk"><input type="checkbox" checked={!!def.stats} onchange={(e) => toggle("stats", e.currentTarget.checked)} /> Stats</label></h4>
            {#if def.stats}
              <div class="stats">
                {#each [["hp", "HP"], ["max_hp", "Max HP"], ["attack", "Attack"], ["defense", "Defense"], ["speed", "Speed"]] as [key, label]}
                  <div class="field"><label for="st-{key}">{label}</label><input id="st-{key}" type="number" value={def.stats[key] ?? 0} oninput={(e) => edit((d) => (d.stats[key] = num(e.currentTarget.value)))} /></div>
                {/each}
              </div>
              <div class="hint">Speed 100 = one action per turn; 200 acts twice as often.</div>
              {#if def.tags?.includes("monster")}
                <div class="field-row" style="margin-top:8px">
                  <div class="field"><label for="xp">XP reward</label><input id="xp" type="number" value={def.data?.xp ?? 0} oninput={(e) => setData("xp", num(e.currentTarget.value))} /></div>
                  <div class="field"><label for="sg">Sight range</label><input id="sg" type="number" value={def.data?.sight ?? 8} oninput={(e) => setData("sight", num(e.currentTarget.value, 8))} /></div>
                </div>
              {/if}
            {/if}
          </section>

          <section>
            <h4><label class="chk"><input type="checkbox" checked={!!def.item} onchange={(e) => toggle("item", e.currentTarget.checked)} /> Item <span class="dim">— can be picked up</span></label></h4>
            {#if def.item}
              <div class="field-row">
                <div class="field"><label for="slot">Equip slot</label>
                  <select id="slot" value={def.item.slot ?? ""} onchange={(e) => edit((d) => { if (e.currentTarget.value) d.item.slot = e.currentTarget.value; else delete d.item.slot; })}>
                    <option value="">Not equippable</option>{#each SLOTS as s}<option value={s}>{s}</option>{/each}
                    {#if def.item.slot && !SLOTS.includes(def.item.slot)}<option value={def.item.slot}>{def.item.slot}</option>{/if}
                  </select></div>
                <div class="field"><label for="use">On use (effect name)</label>
                  <input id="use" type="text" class="mono" value={def.item.on_use ?? ""} placeholder="e.g. heal" oninput={(e) => edit((d) => { if (e.currentTarget.value) d.item.on_use = e.currentTarget.value; else delete d.item.on_use; })} /></div>
              </div>
              {#if def.item.on_use === "heal"}
                <div class="field" style="max-width:200px;margin-top:8px"><label for="ha">Heal amount</label><input id="ha" type="number" value={def.data?.heal_amount ?? 5} oninput={(e) => setData("heal_amount", num(e.currentTarget.value, 5))} /></div>
              {:else if def.item.on_use}
                <div class="hint">Define this effect in a script: <code>rogue.item_effect("{def.item.on_use}", function(who, item) … end)</code></div>
              {/if}
              {#if def.item.slot}
                <div class="label" style="margin-top:10px">Stat bonuses while equipped</div>
                {#each Object.entries(def.item.modifiers ?? {}) as [stat, val]}
                  <div class="modrow">
                    <select value={stat} onchange={(e) => edit((d) => { const v = d.item.modifiers[stat]; delete d.item.modifiers[stat]; d.item.modifiers[e.currentTarget.value] = v; })}>{#each STATS as s}<option value={s}>{s}</option>{/each}</select>
                    <input type="number" value={val as number} oninput={(e) => edit((d) => (d.item.modifiers[stat] = num(e.currentTarget.value)))} />
                    <button class="btn ghost icon sm" aria-label="Remove" onclick={() => edit((d) => delete d.item.modifiers[stat])}><Icon name="x" size={13} /></button>
                  </div>
                {/each}
                <button class="btn sm" style="align-self:flex-start" onclick={() => edit((d) => { d.item.modifiers ??= {}; const free = STATS.find((s) => !(s in d.item.modifiers)); if (free) d.item.modifiers[free] = 1; })}><Icon name="plus" size={13} /> Add bonus</button>
              {/if}
            {/if}
          </section>

          <section>
            <h4><label class="chk"><input type="checkbox" checked={!!def.data?.spawn} onchange={(e) => toggle("spawn", e.currentTarget.checked)} /> Appears in generated dungeons</label></h4>
            {#if def.data?.spawn}
              {@const sp = def.data.spawn}
              <div class="stats">
                <div class="field"><label for="sk">Spawns as</label><select id="sk" value={sp.kind ?? "monster"} onchange={(e) => setData("spawn", { ...sp, kind: e.currentTarget.value })}><option value="monster">Monster</option><option value="item">Floor item</option></select></div>
                <div class="field"><label for="mn">From floor</label><input id="mn" type="number" min="1" value={sp.min_depth ?? 1} oninput={(e) => setData("spawn", { ...sp, min_depth: num(e.currentTarget.value, 1) })} /></div>
                <div class="field"><label for="mx">To floor</label><input id="mx" type="number" min="1" value={sp.max_depth ?? 99} oninput={(e) => setData("spawn", { ...sp, max_depth: num(e.currentTarget.value, 99) })} /></div>
                <div class="field"><label for="wt">Weight</label><input id="wt" type="number" min="1" value={sp.weight ?? 1} oninput={(e) => setData("spawn", { ...sp, weight: num(e.currentTarget.value, 1) })} /></div>
              </div>
              <div class="hint">Appears on floors {sp.min_depth ?? 1}–{sp.max_depth >= 99 ? "∞" : sp.max_depth ?? "∞"}. Higher weight = more common.</div>
            {/if}
          </section>

          <section>
            <h4>Custom data <span class="dim">— values your scripts can read with <code>rogue.get_data</code></span></h4>
            {#each customData as [k, v]}
              <div class="modrow"><input type="text" class="mono" value={k} readonly style="opacity:.8" />
                <input type="text" class="mono" value={typeof v === "string" ? v : JSON.stringify(v)} onchange={(e) => setCustom(k, e.currentTarget.value)} />
                <button class="btn ghost icon sm" aria-label="Remove" onclick={() => setData(k, undefined)}><Icon name="x" size={13} /></button></div>
            {/each}
            <form class="modrow" onsubmit={(e) => { e.preventDefault(); if (customKey.trim()) { setData(customKey.trim(), 0); customKey = ""; } }}>
              <input type="text" class="mono" placeholder="new key, e.g. mana" bind:value={customKey} /><button class="btn sm"><Icon name="plus" size={13} /> Add</button>
            </form>
          </section>
        {/if}
      </div>
    {:else}
      <div class="empty big-empty">
        <Icon name="box" size={40} />
        <h3>Objects</h3>
        <p class="dim">Design monsters, items, weapons and tiles with simple forms — no code needed.<br />Pick one on the left, or create something new.</p>
        <button class="btn primary" onclick={() => (showNew = true)}><Icon name="plus" size={14} /> New {kind === "entities" ? "entity" : "tile"}</button>
      </div>
    {/if}
  </div>
</div>

{#if showNew}
  <Modal title={kind === "entities" ? "New entity" : "New tile"} onclose={() => (showNew = false)} width={420}>
    <form class="field" onsubmit={(e) => { e.preventDefault(); if (/^[A-Za-z0-9_-]+$/.test(newId)) create(); }}>
      <label for="ni">ID</label>
      <!-- svelte-ignore a11y_autofocus -->
      <input id="ni" type="text" bind:value={newId} autofocus placeholder={kind === "entities" ? "e.g. dragon" : "e.g. lava"} spellcheck="false" />
      <div class="label" style="margin-top:10px">Start from</div>
      <div class="presets">
        {#each (kind === "entities" ? [["monster", "Monster", "sword"], ["weapon", "Weapon", "sword"], ["armor", "Armor", "shield"], ["potion", "Potion", "heart"], ["item", "Item", "bag"], ["blank", "Blank", "box"]] : [["floor", "Walkable floor", "grid"], ["wall", "Solid wall", "square"]]) as [id, label, icon]}
          <button type="button" class="btn" class:on={newPreset === id || (kind === "tiles" && id === "floor" && newPreset !== "wall")} onclick={() => (newPreset = id)}><Icon name={icon} size={14} /> {label}</button>
        {/each}
      </div>
      <div style="display:flex;justify-content:flex-end;gap:8px;margin-top:14px">
        <button type="button" class="btn ghost" onclick={() => (showNew = false)}>Cancel</button>
        <button class="btn primary" disabled={!/^[A-Za-z0-9_-]+$/.test(newId)}>Create</button>
      </div>
    </form>
  </Modal>
{/if}
{#if confirmDel}
  <Confirm title={selected in base ? "Reset to built-in" : "Delete"} confirm={selected in base ? "Reset" : "Delete"}
    text={selected in base ? `Discard your changes to “${selected}” and use the built-in version?` : `Delete “${selected}”? Spawned copies in scripts will fail to create.`}
    onyes={remove} onclose={() => (confirmDel = false)} />
{/if}

<style>
  .wrap { flex: 1; display: flex; min-width: 0; }
  .left { width: 300px; flex: none; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: var(--bg-1); border-right: 1px solid var(--line); }
  .seg { display: grid; grid-template-columns: 1fr 1fr; background: var(--bg-2); border: 1px solid var(--line); padding: 3px; border-radius: 9px; }
  .seg button { background: none; border: 0; border-radius: 6px; padding: 6px; display: flex; gap: 6px; align-items: center; justify-content: center; color: var(--dim); font-weight: 500; }
  .seg button.on { background: var(--bg-4); color: var(--text); }
  .searchrow { display: flex; gap: 8px; }
  .search { flex: 1; display: flex; align-items: center; gap: 7px; background: var(--bg); border: 1px solid var(--line-2); border-radius: 8px; padding: 0 9px; color: var(--dim); }
  .search input { flex: 1; border: 0; background: none; padding: 6px 0; box-shadow: none !important; }
  .grp { font-size: 10px; font-weight: 700; letter-spacing: 0.09em; text-transform: uppercase; color: var(--dim); padding: 10px 6px 3px; }
  .ico { width: 28px; height: 28px; display: grid; place-items: center; background: var(--bg-2); border-radius: 7px; flex: none; font-size: 16px; }
  .main { flex: 1; min-width: 0; background: var(--bg); }
  .form { max-width: 760px; margin: 0 auto; padding: 24px; display: flex; flex-direction: column; gap: 16px; }
  .hdr { display: flex; gap: 16px; align-items: center; background: var(--bg-1); border: 1px solid var(--line); border-radius: 14px; padding: 16px; }
  .big { width: 96px; height: 96px; display: grid; place-items: center; background: radial-gradient(circle, #232a3b, #14171f); border-radius: 12px; border: 1px solid var(--line-2); flex: none; }
  .hf { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 6px; }
  .title { font-size: 20px; font-weight: 650; background: transparent; border-color: transparent; padding: 3px 6px; margin: 0 -6px; letter-spacing: -0.01em; }
  .title:hover { border-color: var(--line-2); }
  .meta { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; font-size: 12px; }
  .hbtns { display: flex; flex-direction: column; gap: 6px; }
  section { background: var(--bg-1); border: 1px solid var(--line); border-radius: 14px; padding: 14px 16px 16px; display: flex; flex-direction: column; gap: 10px; }
  h4 { margin: 0; font-size: 13px; font-weight: 650; display: flex; gap: 8px; align-items: center; }
  h4 .dim { font-weight: 400; font-size: 12px; }
  .chk { display: flex; align-items: center; gap: 8px; cursor: pointer; font-weight: 500; }
  .chk .dim { font-weight: 400; }
  .hint { font-size: 11.5px; color: var(--dim); line-height: 1.5; }
  .hint b { color: var(--text-2); }
  code { font-family: var(--mono); font-size: 11.5px; background: var(--bg-3); padding: 1px 5px; border-radius: 4px; }
  .stats { display: grid; grid-template-columns: repeat(auto-fill, minmax(110px, 1fr)); gap: 10px; }
  .chips { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; }
  .chip .x { background: none; border: 0; padding: 0; display: grid; color: var(--dim); } .chip .x:hover { color: var(--bad); }
  .tagin { width: 110px; padding: 2px 9px; border-radius: 99px; font-size: 12px; }
  .modrow { display: flex; gap: 8px; align-items: center; } .modrow > input, .modrow > select { flex: 1; }
  .presets { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
  .presets .btn { justify-content: flex-start; }
  .big-empty { height: 100%; align-content: center; gap: 10px; }
  .big-empty h3 { margin: 0; }
</style>
