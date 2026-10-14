// Vstup aplikacie MCP Apps (vite.mcp.config.ts - jeden subor HTML, ktory server vyda ako ui://tablesimulator/board.html
// aj ui://tablesimulator/virtual.html; ktora aplikacia sa spusti, urci data-app korena z TableSimulator/Mcp/AppPages.cs).
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BoardApp } from "./BoardApp";
import { McpAppBridge } from "./bridge";
import { VirtualApp } from "./VirtualApp";
import "./app.css";

const bridge = new McpAppBridge((message) => window.parent.postMessage(message, "*"));
window.addEventListener("message", (event) => bridge.receive(event.data));

// ramec nesmie na siet - dotazy API su vypnute, vsetko prichadza z nastrojov board_view a virtual_view
const queries = new QueryClient({ defaultOptions: { queries: { enabled: false, retry: false } } });

const root = document.getElementById("root")!;

createRoot(root).render(
  <QueryClientProvider client={queries}>{root.dataset.app === "virtual" ? <VirtualApp bridge={bridge} /> : <BoardApp bridge={bridge} />}</QueryClientProvider>,
);
