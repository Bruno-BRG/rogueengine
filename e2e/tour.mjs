import { withPage } from "./shot.mjs";
const SP = "/tmp/claude-0/-home-user-rogueengine/61abf173-585d-56c8-abb9-1a26d9c3f664/scratchpad";
const errs = await withPage(async (page) => {
  await page.goto("http://127.0.0.1:1430/");
  await page.fill("#n", "Tour");
  await page.fill("#l", "/tmp/claude-0/tourprojects");
  await page.click("button:has-text('Create & play')");
  await page.waitForSelector("canvas");
  await page.waitForTimeout(800);
  for (const [tab, file] of [["Visual Script","graph"],["Sprites","sprites"],["Objects","objects"],["Lua Scripts","scripts"]]) {
    await page.click(`nav button:has-text('${tab}')`);
    await page.waitForTimeout(900);
    await page.screenshot({ path: `${SP}/tour-${file}.png` });
  }
});
console.log(errs);
