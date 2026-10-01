// Usage: node e2e/shot.mjs <out.png> <script.js-body>  (helper for visual checks)
import { chromium } from "playwright-core";
import fs from "node:fs";

export async function withPage(fn, { width = 1440, height = 900 } = {}) {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM ?? "/opt/pw-browsers/chromium-1194/chrome-linux/chrome", args: ["--no-sandbox"] });
  const page = await browser.newPage({ viewport: { width, height } });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => m.type() === "error" && errors.push("console: " + m.text()));
  try { await fn(page, errors); } finally { await browser.close(); }
  return errors;
}
