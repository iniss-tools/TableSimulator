// Vstup aplikacie MCP Apps (vite.mcp.config.ts - jeden subor HTML, ktory server vyda ako ui://tablesimulator/board.html).
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BoardApp } from "./BoardApp";
import { McpAppBridge } from "./bridge";
import "./app.css";

const bridge = new McpAppBridge((message) => window.parent.postMessage(message, "*"));
window.addEventListener("message", (event) => bridge.receive(event.data));

// ramec nesmie na siet - dotazy API su vypnute, vsetko prichadza z nastroja board_view
const queries = new QueryClient({ defaultOptions: { queries: { enabled: false, retry: false } } });

createRoot(document.getElementById("root")!).render(
  <QueryClientProvider client={queries}>
    <BoardApp bridge={bridge} />
  </QueryClientProvider>,
);
