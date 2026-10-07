// Zivy stav: data su v cache TanStack Query, WebSocket /ws ich priamo meni (tabula, linka, zaznam) alebo oznaci
// na nove nacitanie (struktura, zaznamy, virtualne tabule). Navyse len spojenie a verzia vzhladov.
import type { QueryKey } from "@tanstack/react-query";
import { useSyncExternalStore } from "react";
import { socketUrl } from "./auth";
import {
  getDesignsQueryKey,
  getFontsQueryKey,
  getMeQueryKey,
  getMonitorQueryKey,
  getRecordingsQueryKey,
  getStateQueryKey,
  getVirtualOverviewQueryKey,
  getVirtualViewQueryKey,
} from "./generated/@tanstack/react-query.gen";
import type { FrameDto, RecordingsDto, StateDto, VirtualViewDto } from "./generated/types.gen";
import { onUnauthorized } from "./client";
import type { LiveMessage } from "./messages";
import { queryClient } from "./query";

interface Connection {
  /** WebSocket je pripojeny. */
  connected: boolean;
  /** Zvysi sa pri zmene vzhladu v priecinku vzhladov - renderer si ho nacita znova. */
  designVersion: number;
}

let connection: Connection = { connected: false, designVersion: 0 };
const listeners = new Set<() => void>();
const designListeners = new Set<(id: string) => void>();

function setConnection(patch: Partial<Connection>) {
  connection = { ...connection, ...patch };
  listeners.forEach((l) => l());
}

/** Stav spojenia so serverom a verzia vzhladov. */
export function useConnection(): Connection {
  return useSyncExternalStore(
    (l) => {
      listeners.add(l);
      return () => listeners.delete(l);
    },
    () => connection,
  );
}

/** Zmena suboru vzhladu (id) - nahlady si ho nacitaju znova. */
export function onDesignChanged(listener: (id: string) => void): () => void {
  designListeners.add(listener);
  return () => designListeners.delete(listener);
}

function upsert<T>(items: T[], item: T, key: (x: T) => string | number): T[] {
  const k = key(item);
  const i = items.findIndex((x) => key(x) === k);
  if (i < 0) return [...items, item];
  const copy = items.slice();
  copy[i] = item;
  return copy;
}

const stateKey: QueryKey = getStateQueryKey();
const recordingsKey: QueryKey = getRecordingsQueryKey();
const virtualViewId = getVirtualViewQueryKey({ path: { id: 0 } })[0]._id;

function updateState(update: (state: StateDto) => StateDto) {
  queryClient.setQueryData<StateDto>(stateKey, (state) => state && update(state));
}

function updateRecordings(update: (recordings: RecordingsDto) => RecordingsDto) {
  queryClient.setQueryData<RecordingsDto>(recordingsKey, (recordings) => recordings && update(recordings));
}

/** Zobrazenia virtualnych tabul, ktorych sa zmena tyka (ich XML alebo definicie). */
function invalidateVirtual(sources: string[], definitions: boolean) {
  const changed = new Set(sources.map((s) => s.toLowerCase()));
  void queryClient.invalidateQueries({ queryKey: getVirtualOverviewQueryKey() });
  void queryClient.invalidateQueries({
    predicate: (query) => {
      const key = query.queryKey[0] as { _id?: string } | undefined;
      if (key?._id !== virtualViewId) return false;
      const view = query.state.data as VirtualViewDto | undefined;
      return definitions || !view || changed.has(view.board.source.toLowerCase());
    },
  });
}

function apply(message: LiveMessage) {
  switch (message.type) {
    case "board":
      updateState((s) => ({ ...s, boards: upsert(s.boards, message.board, (b) => b.id) }));
      break;
    case "line":
      updateState((s) => ({ ...s, lines: upsert(s.lines, message.line, (l) => l.number) }));
      break;
    case "structure":
      // aj zmena nastavenia - priecinky pisiem, vzhladov a Monitora
      queryClient.invalidateQueries({ queryKey: stateKey });
      queryClient.invalidateQueries({ queryKey: getFontsQueryKey() });
      queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
      queryClient.invalidateQueries({ queryKey: getMonitorQueryKey() });
      break;
    case "design":
      queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
      setConnection({ designVersion: connection.designVersion + 1 });
      designListeners.forEach((l) => l(message.id));
      break;
    case "recorder":
      updateRecordings((r) => ({ ...r, recorder: message.status }));
      // dokonceny zaznam pribudol do zoznamu
      if (!message.status.recording) queryClient.invalidateQueries({ queryKey: recordingsKey });
      break;
    case "replay":
      updateRecordings((r) => ({ ...r, replay: message.status }));
      // po dohrani mohol pribudnut zaznam s novym casom
      if (message.status.state !== "running") queryClient.invalidateQueries({ queryKey: recordingsKey });
      break;
    case "recordings":
      queryClient.invalidateQueries({ queryKey: recordingsKey });
      break;
    case "virtual":
      invalidateVirtual(message.sources, message.definitions);
      break;
  }
}

/**
 * Otvori WebSocket; pri vypadku sa znova pripaja. Hlavne spojenie (bez <paramref name="onFrames" />) meni cache
 * a po kazdom pripojeni nacita stav znova; spojenie logu (<c>withLog</c>) len odovzdava zaznamy komunikacie.
 */
export function openSocket(onFrames?: (frames: FrameDto[]) => void, withLog = false): () => void {
  let socket: WebSocket | null = null;
  let closed = false;
  let retry: ReturnType<typeof setTimeout> | undefined;

  const connect = () => {
    socket = new WebSocket(socketUrl(withLog));
    socket.onopen = () => {
      if (withLog) return;
      setConnection({ connected: true });
      // co sa zmenilo pocas vypadku
      queryClient.invalidateQueries({ queryKey: stateKey });
      queryClient.invalidateQueries({ queryKey: recordingsKey });
      invalidateVirtual([], true);
    };
    socket.onclose = () => {
      if (!withLog) setConnection({ connected: false });
      if (!closed) retry = setTimeout(connect, 1500);
    };
    socket.onmessage = (event) => {
      const message = JSON.parse(event.data) as LiveMessage;
      if (message.type === "frames") onFrames?.(message.frames);
      else if (!withLog) apply(message);
    };
  };

  connect();
  return () => {
    closed = true;
    clearTimeout(retry);
    socket?.close();
  };
}

// prihlasenie vyprsalo alebo pouzivatela vypli - zisti sa znova, kto je prihlaseny
onUnauthorized(() => {
  queryClient.invalidateQueries({ queryKey: getMeQueryKey() });
});
