// Zivy stav: data su v cache TanStack Query, WebSocket /ws ich priamo meni (tabula, linka, zaznam) alebo oznaci
// na nove nacitanie (struktura, zaznamy, virtualne tabule). Navyse len spojenie a verzia vzhladov.
import type { QueryKey } from "@tanstack/react-query";
import { useSyncExternalStore } from "react";
import { socketUrl } from "./auth";
import {
  getDesignsQueryKey,
  getFaultScenariosQueryKey,
  getFontsQueryKey,
  getMeQueryKey,
  getMonitorQueryKey,
  getRecordingsQueryKey,
  getReportMonitorQueryKey,
  getSignageOverviewQueryKey,
  getSignageProgramQueryKey,
  getStateQueryKey,
  getVirtualOverviewQueryKey,
  getVirtualViewQueryKey,
} from "./generated/@tanstack/react-query.gen";
import type { FrameDto, RecordingsDto, ReportMonitorDto, StateDto, VirtualViewDto } from "./generated/types.gen";
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
const programId = getSignageProgramQueryKey({ path: { id: 0 } })[0]._id;
const reportMonitorId = getReportMonitorQueryKey()[0]._id;

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

/** Prehlad informacnych panelov a programy panelov. */
function invalidateSignage() {
  void queryClient.invalidateQueries({ queryKey: getSignageOverviewQueryKey() });
  void queryClient.invalidateQueries({ predicate: (query) => (query.queryKey[0] as { _id?: string } | undefined)?._id === programId });
}

/** Monitor hlaseni: nove zaznamy navrch (najviac tolko, kolko sa nacitalo), iny subor = nacitat znova. */
function updateReportMonitor(message: Extract<LiveMessage, { type: "reports" }>) {
  const isMonitor = (query: { queryKey: QueryKey }) => (query.queryKey[0] as { _id?: string } | undefined)?._id === reportMonitorId;
  if (message.reset) {
    void queryClient.invalidateQueries({ predicate: isMonitor });
    return;
  }
  queryClient.setQueriesData<ReportMonitorDto>({ predicate: isMonitor }, (monitor) => {
    if (!monitor) return monitor;
    // najviac tolko, kolko server da v prvom nacitani (ReportMonitor.MaxEntries) - starsie su v archive
    const entries = [...[...message.added].reverse(), ...monitor.entries].slice(0, 1000);
    return { ...monitor, date: message.date, total: message.total, skipped: message.skipped, entries };
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
      void queryClient.invalidateQueries({ queryKey: stateKey });
      void queryClient.invalidateQueries({ queryKey: getFontsQueryKey() });
      void queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
      void queryClient.invalidateQueries({ queryKey: getMonitorQueryKey() });
      break;
    case "design":
      void queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
      setConnection({ designVersion: connection.designVersion + 1 });
      designListeners.forEach((l) => l(message.id));
      break;
    case "recorder":
      updateRecordings((r) => ({ ...r, recorder: message.status }));
      // dokonceny zaznam pribudol do zoznamu
      if (!message.status.recording) void queryClient.invalidateQueries({ queryKey: recordingsKey });
      break;
    case "replay": {
      // starsia sprava (odoslana pred posunom) nesmie vratit polohu spat
      const current = queryClient.getQueryData<RecordingsDto>(recordingsKey)?.replay;
      if (current && current.name === message.status.name && Date.parse(message.status.at) < Date.parse(current.at)) break;
      updateRecordings((r) => ({ ...r, replay: message.status }));
      // po dohrani mohol pribudnut zaznam s novym casom
      if (message.status.state === "finished" || message.status.state === "stopped") void queryClient.invalidateQueries({ queryKey: recordingsKey });
      break;
    }
    case "recordings":
      void queryClient.invalidateQueries({ queryKey: recordingsKey });
      break;
    case "virtual":
      invalidateVirtual(message.sources, message.definitions);
      break;
    case "signage":
      invalidateSignage();
      break;
    case "reports":
      updateReportMonitor(message);
      break;
    case "scenarios":
      void queryClient.invalidateQueries({ queryKey: getFaultScenariosQueryKey() });
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
      void queryClient.invalidateQueries({ queryKey: stateKey });
      void queryClient.invalidateQueries({ queryKey: recordingsKey });
      invalidateVirtual([], true);
      invalidateSignage();
    };
    socket.onclose = () => {
      if (!withLog) setConnection({ connected: false });
      if (!closed) retry = setTimeout(connect, 1500);
    };
    socket.onmessage = (event) => {
      const message = JSON.parse(event.data as string) as LiveMessage;
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
  void queryClient.invalidateQueries({ queryKey: getMeQueryKey() });
});
