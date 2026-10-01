import { withPage } from "./shot.mjs";
const SP = "/tmp/claude-0/-home-user-rogueengine/61abf173-585d-56c8-abb9-1a26d9c3f664/scratchpad";
const errs = await withPage(async (page) => {
  await page.goto("http://127.0.0.1:1430/");
  await page.screenshot({ path: SP + "/welcome.png" });
  await page.fill("#n", "Shot Game");
  await page.fill("#l", "/tmp/claude-0/shotprojects");
  await page.click("button:has-text('Create & play')");
  await page.waitForSelector("canvas");
  await page.waitForTimeout(1200);
  await page.screenshot({ path: SP + "/play1.png" });
  for (const k of ["ArrowRight","ArrowRight","ArrowDown","ArrowDown","ArrowRight","ArrowRight"]) { await page.keyboard.press(k); await page.waitForTimeout(150); }
  await page.waitForTimeout(500);
  await page.screenshot({ path: SP + "/play2.png" });
});
console.log(errs);
