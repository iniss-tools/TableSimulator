// Zivy stav simulatora: prve nacitanie cez /api/state, potom zmeny cez WebSocket /ws.
import { useSyncExternalStore } from "react";
import { api, onUnauthorized, type Board, type Frame, type Line, type Me, type RecorderStatus, type ReplayStatus, type State } from "./api";

export interface LiveState {
  /** Prihlaseny pouzivatel; null = este sa nezistilo. */
  me: Me | null;
  loaded: boolean;
  connected: boolean;
  lines: Line[];
  boards: Board[];
  supportedClasses: number[];
  /** Vyrobcovia tabul podla triedy linky. */
  manufacturers: Record<string, string[]>;
  settings: State["settings"] | null;
  /** Zvysi sa pri zmene vzhladu v priecinku vzhladov - renderer si ho nacita znova. */
  designVersion: number;
  /** Prebiehajuci zaznam komunikacie. */
  recorder: RecorderStatus | null;
  /** Posledne prehravanie zaznamu. */
  replay: ReplayStatus | null;
  /** Zvysi sa, ked pribudne alebo ubudne zaznam. */
  recordingsVersion: number;
}

let state: LiveState = {
  me: null,
  loaded: false,
  connected: false,
  lines: [],
  boards: [],
  supportedClasses: [],
  manufacturers: {},
  settings: null,
  designVersion: 0,
  recorder: null,
  replay: null,
  recordingsVersion: 0,
};

const listeners = new Set<() => void>();
const designListeners = new Set<(id: string) => void>();
const virtualListeners = new Set<(sources: string[], definitions: boolean) => void>();

function set(patch: Partial<LiveState>) {
  state = { ...state, ...patch };
  listeners.forEach((l) => l());
}

export async function reload() {
  const s = await api.state();
  set({
    loaded: true,
    lines: s.lines,
    boards: s.boards,
    supportedClasses: s.supportedClasses,
    manufacturers: s.manufacturers,
    settings: s.settings,
  });
  const r = await api.recordings();
  set({ recorder: r.recorder, replay: r.replay });
}

function upsert<T>(items: T[], item: T, key: (x: T) => string | number): T[] {
  const k = key(item);
  const i = items.findIndex((x) => key(x) === k);
  if (i < 0) return [...items, item];
  const copy = items.slice();
  copy[i] = item;
  return copy;
}

/** Otvori WebSocket; pri vypadku sa znova pripaja. */
export function openSocket(onFrames?: (frames: Frame[]) => void, withLog = false): () => void {
  let socket: WebSocket | null = null;
  let closed = false;
  let retry: ReturnType<typeof setTimeout> | undefined;

  const connect = () => {
    const proto = location.protocol === "https:" ? "wss" : "ws";
    socket = new WebSocket(`${proto}://${location.host}/ws${withLog ? "?log=1" : ""}`);
    socket.onopen = () => {
      if (!withLog) {
        set({ connected: true });
        reload().catch(() => undefined);
        // po vypadku spojenia si virtualne tabule nacitaju obsah znova
        virtualListeners.forEach((l) => l([], true));
      }
    };
    socket.onclose = () => {
      if (!withLog) set({ connected: false });
      if (!closed) retry = setTimeout(connect, 1500);
    };
    socket.onmessage = (event) => {
      const msg = JSON.parse(event.data);
      switch (msg.type) {
        case "board":
          if (!withLog) set({ boards: upsert(state.boards, msg.board as Board, (b) => b.id) });
          break;
        case "line":
          if (!withLog) set({ lines: upsert(state.lines, msg.line as Line, (l) => l.number) });
          break;
        case "structure":
          if (!withLog) reload().catch(() => undefined);
          break;
        case "design":
          if (!withLog) {
            set({ designVersion: state.designVersion + 1 });
            designListeners.forEach((l) => l(msg.id));
          }
          break;
        case "frames":
          onFrames?.(msg.frames as Frame[]);
          break;
        case "recorder":
          if (!withLog) set({ recorder: msg.status as RecorderStatus });
          break;
        case "replay":
          if (!withLog) {
            const replay = msg.status as ReplayStatus;
            set({ replay, recordingsVersion: replay.state === "running" ? state.recordingsVersion : state.recordingsVersion + 1 });
          }
          break;
        case "recordings":
          if (!withLog) set({ recordingsVersion: state.recordingsVersion + 1 });
          break;
        case "virtual":
          if (!withLog) virtualListeners.forEach((l) => l(msg.sources as string[], msg.definitions as boolean));
          break;
      }
    };
  };

  connect();
  return () => {
    closed = true;
    clearTimeout(retry);
    socket?.close();
  };
}

export function useLive(): LiveState {
  return useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => state,
  );
}

export function onDesignChanged(listener: (id: string) => void): () => void {
  designListeners.add(listener);
  return () => designListeners.delete(listener);
}

/** Zmena XML tabul (mena suborov) alebo definicii virtualnych tabul. */
export function onVirtualChanged(listener: (sources: string[], definitions: boolean) => void): () => void {
  virtualListeners.add(listener);
  return () => virtualListeners.delete(listener);
}

/** Vyrobcovia tabul na linke danej triedy. */
export function manufacturersFor(live: LiveState, tableClass: number | undefined): string[] {
  return (tableClass !== undefined && live.manufacturers[String(tableClass)]) || [];
}

/** Zisti prihlaseneho pouzivatela (pri starte, po prihlaseni a po odpovedi 401). */
export async function loadMe() {
  try {
    set({ me: await api.me() });
  } catch {
    // server nebezi - skusi sa znova pri dalsom nacitani
  }
}

onUnauthorized(() => {
  loadMe().catch(() => undefined);
});

/** Ci prihlaseny pouzivatel ma opravnenie. */
export function can(me: Me | null, permission: string): boolean {
  return me?.permissions.includes(permission) ?? false;
}

/** Opravnenia prihlaseneho pouzivatela pre komponenty. */
export function useCan(): (permission: string) => boolean {
  const { me } = useLive();
  return (permission) => can(me, permission);
}
