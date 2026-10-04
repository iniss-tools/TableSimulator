import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// vystup ide do wwwroot servera; "bun run dev" posiela /api a /ws na beziaci server
export default defineConfig({
  plugins: [react()],
  build: { outDir: "../TableSimulator/wwwroot", emptyOutDir: true },
  server: {
    proxy: {
      "/api": "http://localhost:5470",
      "/designs": "http://localhost:5470",
      "/ws": { target: "ws://localhost:5470", ws: true },
    },
  },
});
