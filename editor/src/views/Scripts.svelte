<script lang="ts">
  import { app, debounced, fail, playNow, refreshProject, toast } from "../lib/app.svelte";
  import { call } from "../lib/api";
  import { API, GROUPS } from "../lib/apiDocs";
  import Icon from "../ui/Icon.svelte";
  import Prompt from "../ui/Prompt.svelte";
  import Confirm from "../ui/Confirm.svelte";
  import { EditorState } from "@codemirror/state";
  import { EditorView, keymap } from "@codemirror/view";
  import { basicSetup } from "codemirror";
  import { StreamLanguage, HighlightStyle, syntaxHighlighting } from "@codemirror/language";
  import { lua } from "@codemirror/legacy-modes/mode/lua";
  import { autocompletion, type CompletionContext } from "@codemirror/autocomplete";
  import { indentWithTab } from "@codemirror/commands";
  import { tags as t } from "@lezer/highlight";

  const theme = EditorView.theme({
    "&": { height: "100%", backgroundColor: "#0b0d12", color: "#d6dcee", fontSize: "13px" },
    ".cm-scroller": { fontFamily: "var(--mono)", lineHeight: "1.65" },
    ".cm-content": { caretColor: "#a89bff", padding: "10px 0" },
    ".cm-cursor": { borderLeftColor: "#a89bff" },
    ".cm-gutters": { backgroundColor: "#0b0d12", color: "#525c75", border: "none" },
    ".cm-activeLine": { backgroundColor: "rgba(139,123,255,0.07)" },
    ".cm-activeLineGutter": { backgroundColor: "transparent", color: "#a89bff" },
    "&.cm-focused .cm-selectionBackground, .cm-selectionBackground, ::selection": { backgroundColor: "rgba(139,123,255,0.28) !important" },
    ".cm-tooltip": { backgroundColor: "#1c2130", border: "1px solid #323a50", borderRadius: "8px" },
    ".cm-tooltip-autocomplete ul li[aria-selected]": { backgroundColor: "rgba(139,123,255,0.25)", color: "#fff" },
    "&.cm-focused": { outline: "none" },
  }, { dark: true });
  const highlight = HighlightStyle.define([
    { tag: [t.keyword, t.controlKeyword], color: "#c792ea" },
    { tag: [t.string], color: "#9ee07a" },
    { tag: [t.number, t.bool, t.null], color: "#f5b942" },
    { tag: t.comment, color: "#5c6786", fontStyle: "italic" },
    { tag: [t.function(t.variableName), t.function(t.propertyName)], color: "#82aaff" },
    { tag: [t.propertyName], color: "#7fd1e8" },
    { tag: [t.operator, t.punctuation], color: "#9aa4bf" },
    { tag: t.definition(t.variableName), color: "#ffd9a0" },
  ]);

  function complete(ctx: CompletionContext) {
    const m = ctx.matchBefore(/rogue(\.[A-Za-z_.]*)?$/);
    if (!m || (m.from === m.to && !ctx.explicit)) return null;
    return {
      from: m.from,
      options: API.map((a) => ({ label: "rogue." + a.name, detail: a.sig.replace(/^rogue\.[\w.]+/, ""), info: a.doc, apply: a.snippet.startsWith("rogue") || a.snippet.includes("\n") ? a.snippet : "rogue." + a.name, type: "function" })),
      validFor: /^rogue(\.[A-Za-z_.]*)?$/,
    };
  }

  let host = $state<HTMLDivElement>();
  let view: EditorView | undefined;
  const states = new Map<string, EditorState>();
  let current = $state("");
  let saveState = $state<"saved" | "saving" | "error">("saved");
  let syntax = $state<{ ok: boolean; error: string }>({ ok: true, error: "" });
  let newPrompt = $state(false);
  let confirmDelete = $state(false);
  let q = $state("");
  let openGroups = $state<Record<string, boolean>>({ Events: true, Rules: true });
  let loadedName = "";

  const scripts = $derived(app.project?.scripts ?? []);

  const save = debounced(async () => {
    if (!current || !view) return;
    try { await call("script_save", { name: current, text: view.state.doc.toString() }); saveState = "saved"; } catch (e) { saveState = "error"; throw e; }
  }, 500);
  const check = debounced(async () => {
    if (!view) return;
    const r = await call<{ ok: boolean; error?: string }>("script_check", { text: view.state.doc.toString() });
    syntax = { ok: r.ok, error: r.error ?? "" };
  }, 350);

  function makeState(text: string) {
    return EditorState.create({
      doc: text,
      extensions: [
        basicSetup, theme, syntaxHighlighting(highlight), StreamLanguage.define(lua),
        autocompletion({ override: [complete] }), keymap.of([indentWithTab]),
        EditorView.updateListener.of((u) => { if (u.docChanged) { saveState = "saving"; save(); check(); } }),
      ],
    });
  }

  $effect(() => {
    if (host && !view) view = new EditorView({ parent: host, state: makeState("") });
    return () => {};
  });

  async function open(name: string) {
    if (!view) return;
    if (loadedName) { states.set(loadedName, view.state); await save.flush(); }
    try {
      let st = states.get(name);
      if (!st) st = makeState((await call<{ text: string }>("script_load", { name })).text);
      view.setState(st);
      current = name; loadedName = name; app.selected.script = name; saveState = "saved";
      check();
    } catch (e) { fail(e); }
  }

  $effect(() => {
    if (!view) return;
    if (!scripts.length) { current = ""; loadedName = ""; view.setState(makeState("")); return; }
    if (!scripts.includes(current)) open(scripts.includes(app.selected.script) ? app.selected.script : scripts[0]);
  });

  async function create(name: string) {
    newPrompt = false;
    try {
      await call("script_save", { name, text: `-- ${name}.lua\n-- Runs when the game starts, after built-in rules and your objects.\n\n` });
      await refreshProject(); await open(name);
      view?.focus();
    } catch (e) { fail(e); }
  }
  async function remove() {
    const name = current;
    try { await call("script_delete", { name }); states.delete(name); current = ""; loadedName = ""; await refreshProject(); toast(`Deleted ${name}`); } catch (e) { fail(e); }
  }
  function insert(snippet: string) {
    if (!view) return;
    const { from, to } = view.state.selection.main;
    view.dispatch({ changes: { from, to, insert: snippet }, selection: { anchor: from + snippet.length }, scrollIntoView: true });
    view.focus();
  }
  function jump(e: MouseEvent) {
    const m = /:(\d+):/.exec(syntax.error);
    if (!m || !view) return;
    e.preventDefault();
    const line = view.state.doc.line(Math.min(+m[1], view.state.doc.lines));
    view.dispatch({ selection: { anchor: line.from }, scrollIntoView: true });
    view.focus();
  }
  function onkey(e: KeyboardEvent) {
    if (app.view === "scripts" && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") { e.preventDefault(); save.flush().then(() => { saveState = "saved"; toast("Saved", "success"); }).catch(fail); }
  }
  const filtered = $derived(API.filter((a) => { const s = q.trim().toLowerCase(); return !s || `${a.name} ${a.doc} ${a.group}`.toLowerCase().includes(s); }));
</script>

<svelte:window onkeydown={onkey} />

<div class="wrap">
  <aside class="left">
    <div class="panel-title">Scripts <button class="btn ghost icon sm" title="New script" onclick={() => (newPrompt = true)}><Icon name="plus" size={14} /></button></div>
    <div class="list scroll">
      {#each scripts as s (s)}
        <button class="row" class:sel={s === current} onclick={() => open(s)}><Icon name="file" size={15} /><span class="grow">{s}.lua</span></button>
      {:else}
        <div class="empty">No scripts yet.<button class="btn primary" onclick={() => (newPrompt = true)}><Icon name="plus" size={14} /> New script</button></div>
      {/each}
    </div>
    <div class="spacer"></div>
    <div class="tip">
      <strong><Icon name="book" size={13} /> Tips</strong>
      <p>Scripts run in alphabetical order after the base game and your objects. Prefix with <code>01_</code>, <code>02_</code>… to control order.</p>
      <p>Type <code>rogue.</code> for autocomplete.</p>
    </div>
  </aside>

  <div class="center">
    <div class="toolbar">
      <strong>{current ? current + ".lua" : "No script"}</strong>
      {#if current}<span class="state {saveState}">{saveState === "saved" ? "Saved" : saveState === "saving" ? "Saving…" : "Save failed"}</span>{/if}
      <span class="spacer"></span>
      <button class="btn sm" disabled={!current} onclick={() => (confirmDelete = true)}><Icon name="trash" size={13} /> Delete</button>
      <button class="btn sm gold" onclick={playNow}><Icon name="play" size={13} /> Test</button>
    </div>
    <div class="editor" bind:this={host} class:hidden={!current}></div>
    {#if !current}<div class="empty" style="flex:1"><Icon name="code" size={34} /><span>Create a script to start coding in Lua</span></div>{/if}
    <div class="statusbar" class:bad={!syntax.ok}>
      {#if !current}<span class="dim">Lua 5.4</span>
      {:else if syntax.ok}<span style="color:var(--good)"><Icon name="check" size={13} /> No syntax errors</span>
      {:else}<!-- svelte-ignore a11y_invalid_attribute --><a href="#" onclick={jump}><Icon name="x" size={13} /> {syntax.error}</a>{/if}
    </div>
  </div>

  <aside class="right">
    <div class="panel-title">API reference</div>
    <div class="search"><Icon name="search" size={14} /><input type="text" placeholder="Search functions…" bind:value={q} /></div>
    <div class="scroll api">
      {#each GROUPS as g}
        {@const items = filtered.filter((a) => a.group === g)}
        {#if items.length}
          <button class="grp" onclick={() => (openGroups[g] = !openGroups[g])}><span class="chev" class:open={openGroups[g] || q}><Icon name="chevron" size={13} /></span>{g}<span class="dim">{items.length}</span></button>
          {#if openGroups[g] || q}
            {#each items as a}
              <button class="fn" title="Click to insert" onclick={() => insert(a.snippet)}>
                <code>{a.sig}</code><span>{a.doc}</span>
              </button>
            {/each}
          {/if}
        {/if}
      {/each}
    </div>
  </aside>
</div>

{#if newPrompt}<Prompt title="New script" label="Script name" initial="rules" onsubmit={create} onclose={() => (newPrompt = false)} />{/if}
{#if confirmDelete}<Confirm title="Delete script" text={`Delete “${current}.lua”? This cannot be undone.`} onyes={remove} onclose={() => (confirmDelete = false)} />{/if}

<style>
  .wrap { flex: 1; display: flex; min-width: 0; }
  aside { flex: none; padding: 12px; display: flex; flex-direction: column; gap: 8px; background: var(--bg-1); overflow: hidden; }
  .left { width: 250px; border-right: 1px solid var(--line); }
  .right { width: 340px; border-left: 1px solid var(--line); }
  .center { flex: 1; display: flex; flex-direction: column; min-width: 0; }
  .toolbar { display: flex; gap: 8px; padding: 8px 12px; border-bottom: 1px solid var(--line); background: var(--bg-1); align-items: center; }
  .state { font-size: 11.5px; color: var(--dim); } .state.saving { color: var(--warn); } .state.error { color: var(--bad); }
  .editor { flex: 1; min-height: 0; } .editor.hidden { display: none; }
  .editor :global(.cm-editor) { height: 100%; }
  .statusbar { padding: 6px 14px; border-top: 1px solid var(--line); background: var(--bg-1); font-size: 12px; min-height: 30px; display: flex; align-items: center; }
  .statusbar span, .statusbar a { display: inline-flex; gap: 6px; align-items: center; }
  .statusbar.bad a { color: #ffb3b3; text-decoration: none; font-family: var(--mono); }
  .tip { background: var(--bg-2); border: 1px solid var(--line); border-radius: 10px; padding: 10px 12px; font-size: 12px; color: var(--text-2); }
  .tip strong { display: flex; gap: 5px; align-items: center; color: var(--text); margin-bottom: 4px; }
  .tip p { margin: 4px 0; line-height: 1.5; }
  code { font-family: var(--mono); font-size: 11.5px; background: var(--bg-3); padding: 1px 5px; border-radius: 4px; }
  .search { display: flex; align-items: center; gap: 7px; background: var(--bg); border: 1px solid var(--line-2); border-radius: 8px; padding: 0 9px; color: var(--dim); }
  .search input { flex: 1; border: 0; background: none; padding: 7px 0; box-shadow: none !important; }
  .api { display: flex; flex-direction: column; gap: 2px; flex: 1; }
  .grp { display: flex; align-items: center; gap: 6px; background: none; border: 0; padding: 7px 4px 4px; font-size: 11px; font-weight: 700; letter-spacing: 0.07em; text-transform: uppercase; color: var(--dim); text-align: left; }
  .grp .dim { margin-left: auto; font-weight: 500; }
  .chev { display: grid; transform: rotate(-90deg); transition: transform 0.12s; } .chev.open { transform: none; }
  .fn { display: flex; flex-direction: column; gap: 3px; text-align: left; background: none; border: 1px solid transparent; border-radius: 8px; padding: 7px 9px; color: var(--text-2); }
  .fn:hover { background: var(--bg-3); border-color: var(--line-2); }
  .fn code { background: none; padding: 0; color: #82aaff; font-size: 11.5px; word-break: break-word; }
  .fn span { font-size: 11.5px; line-height: 1.45; color: var(--dim); }
</style>
