import path from "node:path";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig, type Plugin } from "vite";

// Aplikacia MCP Apps (zobrazenie tabule v klientovi s MCP Apps): jeden subor HTML so vsetkym vlozenym - ramec hostitela
// nesmie nic stahovat. Server ho vyda ako zdroje ui://tablesimulator/board.html a virtual.html (TableSimulator/Mcp/AppPages.cs).
// Spusta sa po hlavnej zostave webu (bun run build), ktora wwwroot vyprazdni.

/** Vlozi skripty a styly zostavy do HTML a ostatne subory zahodi. */
function inlineBundle(): Plugin {
  return {
    name: "inline-bundle",
    enforce: "post",
    generateBundle(_, bundle) {
      const html = Object.values(bundle).find((f) => f.type === "asset" && f.fileName.endsWith(".html"));
      if (!html || html.type !== "asset") return;
      let source = String(html.source);
      for (const [name, file] of Object.entries(bundle)) {
        if (file === html) continue;
        // </script> v kode by ukoncil vlozeny skript
        if (file.type === "chunk")
          source = source.replace(
            new RegExp(`<script[^>]*src="[^"]*${file.fileName}"[^>]*></script>`),
            () => `<script type="module">${file.code.replace(/<\/script/gi, "<\\/script")}</script>`,
          );
        else if (name.endsWith(".css"))
          source = source.replace(new RegExp(`<link[^>]*href="[^"]*${file.fileName}"[^>]*>`), () => `<style>${String(file.source)}</style>`);
        delete bundle[name];
      }
      html.source = source;
      html.fileName = "app.html";
    },
  };
}

export default defineConfig({
  plugins: [react(), tailwindcss(), inlineBundle()],
  resolve: { alias: { "@": path.resolve(import.meta.dirname, "./src") } },
  publicDir: false,
  build: {
    outDir: "../TableSimulator/wwwroot/mcp-app",
    emptyOutDir: true,
    // vsetko v jednom subore - obrazky a pisma ako data URL
    assetsInlineLimit: Number.MAX_SAFE_INTEGER,
    cssCodeSplit: false,
    modulePreload: false,
    rolldownOptions: { input: "mcp-app.html", output: { codeSplitting: false } },
  },
});
