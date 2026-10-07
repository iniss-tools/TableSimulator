import path from "node:path";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// vystup ide do wwwroot servera; "bun run dev" posiela /api a /ws na beziaci server
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: { alias: { "@": path.resolve(import.meta.dirname, "./src") } },
  build: {
    outDir: "../TableSimulator/wwwroot",
    emptyOutDir: true,
    // kniznice osobitne - menia sa zriedka, prehliadac ich drzi v cache aj po novej verzii rozhrania
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            { name: "react", test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
            { name: "tanstack", test: /node_modules[\\/]@tanstack[\\/]/ },
          ],
        },
      },
    },
  },
  server: {
    proxy: {
      "/api": "http://localhost:5470",
      "/designs": "http://localhost:5470",
      "/ws": { target: "ws://localhost:5470", ws: true },
    },
  },
});
