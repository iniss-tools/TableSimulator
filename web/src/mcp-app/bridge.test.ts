import { describe, expect, test } from "bun:test";
import { dataOf, McpAppBridge, PROTOCOL_VERSION } from "./bridge";

/** Most s hostitelom, ktory si pamata odoslane spravy. */
function bridge() {
  const sent: { jsonrpc?: string; id?: number | string; method?: string; params?: unknown; result?: unknown }[] = [];
  return { sent, bridge: new McpAppBridge((m) => sent.push(m)) };
}

describe("most MCP Apps", () => {
  test("uvodne podanie ruky a jazyk hostitela", async () => {
    const { sent, bridge: b } = bridge();
    const locale = b.initialize("TableSimulator");
    expect(sent[0]).toMatchObject({ id: 1, method: "ui/initialize", params: { protocolVersion: PROTOCOL_VERSION } });
    b.receive({ jsonrpc: "2.0", id: 1, result: { hostContext: { locale: "cs-CZ" } } });
    expect(await locale).toBe("cs-CZ");
    expect(sent[1]).toMatchObject({ method: "ui/notifications/initialized" });
  });

  test("pripravenost aj pri chybe podania ruky", async () => {
    const { sent, bridge: b } = bridge();
    const locale = b.initialize("TableSimulator");
    b.receive({ jsonrpc: "2.0", id: 1, error: { code: -32601, message: "nie" } });
    expect(await locale.catch((e: Error) => e.message)).toBe("nie");
    expect(sent.some((m) => m.method === "ui/notifications/initialized")).toBe(true);
  });

  test("nastroj servera - udaje zo strukturovaneho obsahu aj z textu, chyba nastroja", async () => {
    const { bridge: b } = bridge();
    const structured = b.callTool<{ a: number }>("x", {});
    b.receive({ jsonrpc: "2.0", id: 1, result: { structuredContent: { a: 1 } } });
    expect(await structured).toEqual({ a: 1 });
    // VS Code posiela vysledok len ako text
    const text = b.callTool<{ a: number }>("x", {});
    b.receive(JSON.stringify({ jsonrpc: "2.0", id: 2, result: { content: [{ type: "text", text: '{"a":2}' }] } }));
    expect(await text).toEqual({ a: 2 });
    const failed = b.callTool("x", {});
    b.receive({ jsonrpc: "2.0", id: 3, result: { isError: true, content: [{ type: "text", text: "board.notFound: Board 9-9" }] } });
    expect(await failed.catch((e: Error) => e.message)).toContain("board.notFound");
  });

  test("oznamenia hostitela a odpoved na jeho poziadavku", () => {
    const { sent, bridge: b } = bridge();
    const inputs: unknown[] = [];
    const off = b.on("ui/notifications/tool-input", (p) => inputs.push(p));
    b.receive({ jsonrpc: "2.0", method: "ui/notifications/tool-input", params: { arguments: { id: "1-1" } } });
    off();
    b.receive({ jsonrpc: "2.0", method: "ui/notifications/tool-input", params: {} });
    b.receive({ jsonrpc: "2.0", id: "t1", method: "ui/resource-teardown", params: {} });
    b.receive({ data: "cudzia sprava" });
    expect(inputs).toEqual([{ arguments: { id: "1-1" } }]);
    expect(sent).toEqual([{ jsonrpc: "2.0", id: "t1", result: {} }]);
  });

  test("udaje bez vysledku alebo s neplatnym textom", () => {
    expect(dataOf(null)).toBeNull();
    expect(dataOf({ content: [{ type: "text", text: "nie je JSON" }] })).toBeNull();
  });
});
