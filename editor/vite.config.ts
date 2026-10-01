import { defineConfig } from "vite";
import { fileURLToPath } from "node:url";
import { svelte } from "@sveltejs/vite-plugin-svelte";

export default defineConfig({
  root: fileURLToPath(new URL(".", import.meta.url)),
  plugins: [svelte()],
  clearScreen: false,
  // In the browser, the editor talks to `rogue-server` (cargo run -p rogue-studio --bin rogue-server).
  server: { port: 1420, strictPort: true, proxy: { "/api": "http://127.0.0.1:1430" } },
  build: { outDir: "dist", emptyOutDir: true, target: "es2022" },
});
