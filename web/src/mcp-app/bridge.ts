// Most aplikacie MCP Apps s hostitelom (VS Code, aplikacia Claude): JSON-RPC 2.0 cez postMessage - ui/initialize,
// argumenty a vysledok nastroja, volanie nastrojov servera cez hostitela. Overene vo VS Code 1.141: vysledok nastroja
// chodi len ako text (bez structuredContent), preto sa udaje citaju aj z JSON v texte.

/** Vysledok nastroja MCP (CallToolResult). */
export interface ToolResult {
  content?: { type: string; text?: string }[];
  structuredContent?: unknown;
  isError?: boolean;
}

interface Message {
  jsonrpc: "2.0";
  id?: number | string;
  method?: string;
  params?: unknown;
  result?: unknown;
  error?: { code: number; message: string };
}

/** Verzia protokolu MCP Apps (SEP-1865), ktoru aplikacia pozna. */
export const PROTOCOL_VERSION = "2026-01-26";

function isMessage(value: unknown): value is Message {
  return typeof value === "object" && value !== null && (value as Message).jsonrpc === "2.0";
}

function parse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

/** Text vysledku nastroja (chyba nastroja, nahradne udaje). */
export function textOf(result: ToolResult): string {
  return (result.content ?? []).find((c) => c.type === "text")?.text ?? "";
}

/** Strukturovany obsah vysledku, inak JSON z jeho textu; null, ak udaje nie su. */
export function dataOf<T>(result: ToolResult | null | undefined): T | null {
  if (!result) return null;
  if (result.structuredContent !== undefined && result.structuredContent !== null) return result.structuredContent as T;
  return parse(textOf(result)) as T | null;
}

export class McpAppBridge {
  private nextId = 1;
  private readonly pending = new Map<number | string, { resolve: (value: unknown) => void; reject: (error: Error) => void }>();
  private readonly handlers = new Map<string, Set<(params: unknown) => void>>();

  /** <paramref name="post" /> posle spravu hostitelovi (window.parent.postMessage). */
  constructor(private readonly post: (message: Message) => void) {}

  /** Sprava od hostitela (event.data okna). */
  receive(data: unknown) {
    const message = typeof data === "string" ? parse(data) : data;
    if (!isMessage(message)) return;
    if (message.method === undefined) {
      const waiting = message.id === undefined ? undefined : this.pending.get(message.id);
      if (!waiting || message.id === undefined) return;
      this.pending.delete(message.id);
      if (message.error) waiting.reject(new Error(message.error.message));
      else waiting.resolve(message.result);
      return;
    }
    this.handlers.get(message.method)?.forEach((handler) => handler(message.params));
    // poziadavka hostitela (napr. ukoncenie zobrazenia) - aplikacia nic nedrzi, staci prazdna odpoved
    if (message.id !== undefined) this.post({ jsonrpc: "2.0", id: message.id, result: {} });
  }

  request<T>(method: string, params: unknown): Promise<T> {
    const id = this.nextId++;
    this.post({ jsonrpc: "2.0", id, method, params });
    return new Promise<T>((resolve, reject) => this.pending.set(id, { resolve: (value) => resolve(value as T), reject }));
  }

  notify(method: string, params: unknown) {
    this.post({ jsonrpc: "2.0", method, params });
  }

  /** Odber oznamenia hostitela; vrati zrusenie. */
  on(method: string, handler: (params: unknown) => void): () => void {
    let set = this.handlers.get(method);
    if (!set) this.handlers.set(method, (set = new Set()));
    set.add(handler);
    return () => set.delete(handler);
  }

  /** Uvodny handshake; aj pri chybe ohlasi pripravenost - hostitel posle argumenty az potom. Vrati jazyk hostitela. */
  async initialize(name: string): Promise<string | null> {
    try {
      const result = await this.request<{ hostContext?: { locale?: string } }>("ui/initialize", {
        appInfo: { name, version: "1.0.0" },
        appCapabilities: {},
        protocolVersion: PROTOCOL_VERSION,
      });
      return result.hostContext?.locale ?? null;
    } finally {
      this.notify("ui/notifications/initialized", {});
    }
  }

  /** Nastroj servera cez hostitela; chyba nastroja = vynimka s jeho textom. */
  async callTool<T>(name: string, args: Record<string, unknown>): Promise<T | null> {
    const result = await this.request<ToolResult>("tools/call", { name, arguments: args });
    if (result.isError) throw new Error(textOf(result));
    return dataOf<T>(result);
  }

  /** Nova vyska obsahu - hostitel podla nej zmeni ramec. */
  resize(height: number) {
    this.notify("ui/notifications/size-changed", { height });
  }
}
