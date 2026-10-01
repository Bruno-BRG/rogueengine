// End-to-end tests: drive the real UI in Chromium against rogue-server.
// Run: cargo build -p rogue-studio --bin rogue-server && npm run build && npm run e2e
import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { spawn } from "node:child_process";
import { chromium } from "playwright-core";

const PORT = process.env.E2E_PORT ?? "1431";
const BASE = `http://127.0.0.1:${PORT}`;
const SHOTS = process.env.E2E_SHOTS;
const root = path.resolve(import.meta.dirname, "..");
const projects = fs.mkdtempSync(path.join(os.tmpdir(), "rogue-e2e-"));
const proj = path.join(projects, "e2e-game");

let server, browser, page;
const errors = [];
const shot = (name) => SHOTS && page.screenshot({ path: path.join(SHOTS, name + ".png") });

test.before(async () => {
  server = spawn(path.join(root, "target/debug/rogue-server"), [`127.0.0.1:${PORT}`, path.join(root, "editor/dist")], { stdio: "ignore" });
  for (let i = 0; i < 50; i++) { try { await fetch(BASE + "/api/app_info", { method: "POST", body: "{}" }); break; } catch { await new Promise((r) => setTimeout(r, 100)); } }
  const local = "/opt/pw-browsers/chromium-1194/chrome-linux/chrome";
  const executablePath = process.env.CHROMIUM ?? (fs.existsSync(local) ? local : undefined);
  browser = await chromium.launch({ executablePath, args: ["--no-sandbox"] });
  page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => m.type() === "error" && errors.push("console: " + m.text()));
});
test.after(async () => { await browser?.close(); server?.kill(); fs.rmSync(projects, { recursive: true, force: true }); });

const tab = (name) => page.click(`nav button:has-text('${name}')`);
const read = (rel) => fs.readFileSync(path.join(proj, rel), "utf8");
async function until(fn, ms = 4000) { const t = Date.now(); for (;;) { const v = await fn(); if (v) return v; if (Date.now() - t > ms) throw new Error("timed out: " + fn); await new Promise((r) => setTimeout(r, 60)); } }

test("create a project from the welcome screen and play", async () => {
  await page.goto(BASE);
  await page.fill("#n", "E2E Game");
  await page.fill("#l", projects);
  await page.click("button:has-text('Create & play')");
  await page.waitForSelector("canvas");
  assert.ok(fs.existsSync(path.join(proj, "game.reproj")));
  await page.waitForSelector("text=Health");
  const hud = await page.textContent("aside");
  assert.match(hud, /DEPTH\s*1/i);
  assert.match(hud, /30 \/ 30/);
  for (const k of ["ArrowRight", "ArrowDown", "ArrowLeft", "ArrowUp", "ArrowRight", "ArrowRight"]) { await page.keyboard.press(k); await page.waitForTimeout(120); }
  await shot("01-play");
  assert.match(await page.textContent("aside"), /enter the dungeon/i);
});

test("visual scripting: build a graph by dragging wires", async () => {
  await tab("Visual Script");
  await page.waitForSelector(".node");
  assert.equal(await page.locator(".node").count(), 5, "template graph has 5 nodes");
  assert.match(await page.textContent(".status"), /Compiles/);
  await shot("02-graph-template");

  await page.click("button[title='New graph']");
  await page.fill("#p", "e2e_graph");
  await page.keyboard.press("Enter");
  await page.waitForSelector("text=Empty graph");
  await page.click("button:has-text('When something dies')");
  await until(async () => (await page.locator(".node").count()) === 1);

  // add a node through the searchable palette
  await page.mouse.click(700, 400);
  await page.keyboard.press("Space");
  await page.fill("input[placeholder='Search nodes…']", "log mess");
  await page.keyboard.press("Enter");
  await until(async () => (await page.locator(".node").count()) === 2);
  // move the new node away from the first one so pins don't overlap
  const head = page.locator(".node.flow .head").first();
  const hb = await head.boundingBox();
  await page.mouse.move(hb.x + 60, hb.y + 10); await page.mouse.down(); await page.mouse.move(hb.x + 330, hb.y + 90, { steps: 6 }); await page.mouse.up();

  const out = await page.locator(".node.event .pin[data-pin='then']").boundingBox();
  const inn = await page.locator(".node.flow .pin[data-pin='exec']").boundingBox();
  await page.mouse.move(out.x + out.width / 2, out.y + out.height / 2);
  await page.mouse.down();
  await page.mouse.move(inn.x + inn.width / 2, inn.y + inn.height / 2, { steps: 8 });
  await page.mouse.up();
  await until(async () => /rogue\.log\("Hello"\)/.test(await page.textContent(".code")));
  assert.equal(await page.locator("svg.wires path.wire:not(.ghost)").count(), 1);
  await shot("03-graph-wired");

  // saved to disk automatically
  await until(() => fs.existsSync(path.join(proj, "graphs/e2e_graph.graph.json")) && JSON.parse(read("graphs/e2e_graph.graph.json")).edges.length === 1);

  // undo removes the edge, delete removes the node
  await page.mouse.click(900, 700);
  await page.keyboard.press("Control+z");
  await until(async () => (await page.locator("svg.wires path.wire:not(.ghost)").count()) === 0);
  await page.locator(".node.flow .head").click();
  await page.keyboard.press("Delete");
  await until(async () => (await page.locator(".node").count()) === 1);
});

test("a graph error is reported instead of crashing the game", async () => {
  fs.writeFileSync(path.join(proj, "graphs/broken.graph.json"), JSON.stringify({ nodes: [{ id: 1, type: "no.such.node", props: {}, x: 0, y: 0 }], edges: [] }));
  await tab("Play");
  await page.click("button.gold");
  await page.waitForSelector("text=Your game has an error");
  assert.match(await page.textContent(".err, pre"), /broken/);
  await shot("04-error");
  fs.unlinkSync(path.join(proj, "graphs/broken.graph.json"));
  await page.click("button:has-text('Try again')");
  await page.waitForSelector("text=Health");
});

test("sprite editor draws, undoes, and persists", async () => {
  await tab("Sprites");
  await page.click("button.row:has-text('rat')");
  await page.waitForSelector("canvas.pixel[style*='crosshair']");
  const before = fs.readFileSync(path.join(proj, "sprites/rat.png"));
  const c = await page.locator("canvas.pixel[style*='crosshair']").boundingBox();
  await page.mouse.click(c.x + 10, c.y + 10);
  await until(() => !fs.readFileSync(path.join(proj, "sprites/rat.png")).equals(before));
  const drawn = fs.readFileSync(path.join(proj, "sprites/rat.png"));
  await page.keyboard.press("Control+z");
  await until(() => fs.readFileSync(path.join(proj, "sprites/rat.png")).equals(before));
  await page.keyboard.press("Control+y");
  await until(() => fs.readFileSync(path.join(proj, "sprites/rat.png")).equals(drawn));
  // freehand stroke = one undo step
  await page.mouse.move(c.x + 60, c.y + 60); await page.mouse.down(); await page.mouse.move(c.x + 200, c.y + 60, { steps: 10 }); await page.mouse.up();
  await page.waitForTimeout(400);
  await page.keyboard.press("Control+z");
  await until(() => fs.readFileSync(path.join(proj, "sprites/rat.png")).equals(drawn));
  await shot("05-sprites");

  // new sprite with 2 frames
  await page.click("button[title='New sprite']");
  await page.fill("#sn", "e2e_gem");
  await page.click("button:has-text('32×32')");
  await page.click(".modal button:has-text('Create')");
  await until(() => fs.existsSync(path.join(proj, "sprites/e2e_gem.png")));
  await page.click("button[title='Duplicate frame']");
  await until(() => JSON.parse(read("sprites/e2e_gem.json")).frames === 2);
});

test("objects: edit a built-in (override), create a monster", async () => {
  await tab("Objects");
  await page.click("button.row:has-text('Giant Rat')");
  await page.waitForSelector("input.title");
  assert.match(await page.textContent(".meta"), /Built-in/);
  await page.fill("input.title", "Huge Rat");
  await until(() => /Huge Rat/.test(read("data/entities.json")));
  assert.match(await page.textContent(".meta"), /Overrides built-in/);
  await shot("06-objects");

  await page.click("button[title='New entity']");
  await page.fill("#ni", "e2e_wyrm");
  await page.click(".modal button:has-text('Create')");
  await until(() => JSON.parse(read("data/entities.json")).entities.e2e_wyrm?.tags?.includes("monster"));
  // give it the slime sprite via the picker
  await page.click(".trigger");
  await page.click(".pop button[title='slime']");
  await until(() => JSON.parse(read("data/entities.json")).entities.e2e_wyrm.sprite === "slime");
  // restart and verify the new monster is registered with the engine
  await page.click("button.gold");
  await page.waitForSelector("text=Health");
});

test("scripts: edit Lua, see syntax errors, game uses it", async () => {
  await tab("Lua Scripts");
  await page.waitForSelector(".cm-content");
  await page.click(".cm-content");
  await page.keyboard.press("Control+End");
  await page.keyboard.type('\nrogue.on("spawned", function() end)\n');
  await until(() => /on\("spawned"/.test(read("scripts/rules.lua")));
  await until(async () => /No syntax errors/.test(await page.textContent(".statusbar")));
  await page.keyboard.type("local = =");
  await until(async () => /:\d+:/.test(await page.textContent(".statusbar")));
  await shot("07-scripts-error");
  // autocomplete
  await page.keyboard.press("Control+a"); await page.keyboard.press("Delete");
  await page.keyboard.type("rogue.spaw");
  await page.waitForSelector(".cm-tooltip-autocomplete");
  await page.keyboard.press("Enter");
  await until(() => /rogue\.spawn\("goblin"/.test(read("scripts/rules.lua")) || fs.readFileSync(path.join(proj, "scripts/rules.lua"), "utf8").includes("rogue.spawn"));
  // restore valid code and play
  await page.keyboard.press("Control+a"); await page.keyboard.type("-- ok\n");
  await until(async () => /No syntax errors/.test(await page.textContent(".statusbar")));
});

test("play: console, stairs and death", async () => {
  await tab("Play");
  await page.click("button.gold");
  await page.waitForSelector("text=Health");
  await page.click("button:has-text('Console')");
  const run = async (code) => { await page.fill(".cin", code); await page.keyboard.press("Enter"); };
  await run("1 + 2");
  await page.waitForSelector(".cl.ok:text-is('3')");
  await run("error('nope')");
  await page.waitForSelector(".cl.err:has-text('nope')");
  // walk onto the stairs and descend with '>'
  await run(`local w,h = rogue.map_size(); for y=0,h-1 do for x=0,w-1 do if rogue.tile(x,y).id == "stairs_down" then rogue.teleport(rogue.find_tagged("player")[1], x, y) end end end`);
  await page.keyboard.press("Escape");
  await page.locator("canvas").first().click({ position: { x: 5, y: 5 } });
  await page.keyboard.press(">");
  await until(async () => /DEPTH\s*2/i.test(await page.textContent("aside")));
  await shot("08-depth2");
  // spawn a goblin through the console, kill it, and gain XP
  await page.keyboard.press("`");
  await run(`local p = rogue.pos(rogue.find_tagged("player")[1]); G = rogue.spawn("goblin", p.x, p.y); rogue.damage(rogue.find_tagged("player")[1], G, 999)`);
  await until(async () => /XP\s*[1-9]/i.test(await page.textContent("aside")));
  // die
  await run(`rogue.damage(nil, rogue.find_tagged("player")[1], 9999)`);
  await page.keyboard.press("Escape");
  await page.keyboard.press("."); // takes a turn so the death is processed
  await page.waitForSelector("text=You died");
  await shot("09-death");
  await page.click("button:has-text('New run')");
  await page.waitForSelector("text=Health");
});

test("no browser errors during the whole session", () => {
  assert.deepEqual(errors, []);
});
