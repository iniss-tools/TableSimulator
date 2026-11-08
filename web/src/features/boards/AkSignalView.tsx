// Ustredna AK Signal: lampa svetla, rezim a kontrolky stavu, prisposobene velkosti miesta.
import { useEffect, useImperativeHandle, useLayoutEffect, useRef, useState, type Ref } from "react";
import type { BoardDto as Board } from "@/api";
import { lightMode, playing } from "@/features/aksignal/aksignal";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { AKSIGNAL_HEIGHT, AKSIGNAL_WIDTH, drawAkSignal, type AkSignalPanel, type AkSignalTexts } from "@/render/aksignal";
import type { BoardViewHandle } from "./BoardView";

const IDLE: AkSignalPanel = { lightOn: false, auto: false, manual: false, dark: false, fault: false, audio: false, silent: false };

function panelOf(board: Board): AkSignalPanel {
  const state = board.display.akSignal;
  if (!state) return IDLE;
  return {
    lightOn: state.lightOn,
    auto: state.autoMode,
    manual: state.manual,
    dark: state.dark,
    fault: state.conditions.lightsFault,
    audio: playing(state).length > 0,
    silent: state.serverSilent,
  };
}

function texts(board: Board): AkSignalTexts {
  const state = board.display.akSignal;
  const mode = t(`aksignal.mode.${state ? lightMode(state) : "off"}`);
  return {
    title: t(state?.lightOn ? "aksignal.title.on" : "aksignal.title.off", { mode }),
    detail: t("aksignal.detail", { sky: t(state?.dark ? "aksignal.dark" : "aksignal.day"), state: state?.lightState || "–" }),
    labels: [
      t("aksignal.lamp.auto"),
      t("aksignal.lamp.manual"),
      t("aksignal.lamp.dark"),
      t("aksignal.lamp.fault"),
      t("aksignal.lamp.audio"),
      t("aksignal.lamp.silent"),
    ],
  };
}

interface Props {
  board: Board;
  fit?: "width" | "contain";
  className?: string;
  ref?: Ref<BoardViewHandle>;
}

export function AkSignalView({ board, fit = "width", className, ref }: Props) {
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [space, setSpace] = useState({ width: 0, height: 0 });
  const latest = useRef(board);
  useLayoutEffect(() => {
    latest.current = board;
  });

  useEffect(() => {
    const el = box.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => setSpace({ width: entry.contentRect.width, height: entry.contentRect.height }));
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  // na sirku miesta, no najviac dvojnasobok - ustredna nema byt cez celu siroku kartu
  const cssScale =
    space.width > 0
      ? fit === "contain" && space.height > 0
        ? Math.min(space.width / AKSIGNAL_WIDTH, space.height / AKSIGNAL_HEIGHT)
        : Math.min(2, space.width / AKSIGNAL_WIDTH)
      : 0;
  const scale = cssScale * (window.devicePixelRatio || 1);

  useImperativeHandle(
    ref,
    () => ({
      snapshot: () => {
        const target = document.createElement("canvas");
        target.width = AKSIGNAL_WIDTH * 2;
        target.height = AKSIGNAL_HEIGHT * 2;
        drawAkSignal(target.getContext("2d")!, panelOf(latest.current), texts(latest.current), 2);
        return new Promise<Blob>((resolve, reject) => target.toBlob((blob) => (blob ? resolve(blob) : reject(new Error("PNG"))), "image/png"));
      },
    }),
    [],
  );

  useEffect(() => {
    const el = canvas.current;
    if (!el || cssScale <= 0) return;
    el.width = Math.max(1, Math.round(AKSIGNAL_WIDTH * scale));
    el.height = Math.max(1, Math.round(AKSIGNAL_HEIGHT * scale));
    const ctx = el.getContext("2d");
    if (ctx) drawAkSignal(ctx, panelOf(board), texts(board), scale);
  }, [board, scale, cssScale]);

  return (
    <div ref={box} className={cn("flex w-full justify-center leading-0", fit === "contain" && "h-full items-center", className)}>
      <canvas
        ref={canvas}
        className="block max-w-full rounded-lg"
        style={cssScale > 0 ? { width: `${AKSIGNAL_WIDTH * cssScale}px`, height: `${AKSIGNAL_HEIGHT * cssScale}px` } : { visibility: "hidden" }}
      />
    </div>
  );
}
