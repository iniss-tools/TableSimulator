// Listova tabula (ERS, FERS): moduly s textami listov, prisposobena velkosti miesta; zmenene listy sa chvilu otacaju.
import { useEffect, useImperativeHandle, useLayoutEffect, useMemo, useRef, useState, type Ref } from "react";
import type { BoardDto as Board } from "@/api";
import { cn } from "@/lib/utils";
import { drawFlaps, layoutFlaps } from "@/render/flaps";
import type { BoardViewHandle } from "./BoardView";

/** Najkratsie a najdlhsie otacanie zmeneneho listu na obrazovke [ms] (inak podla profilu tabule). */
const FLIP_MIN = 300;
const FLIP_MAX = 4000;
/** Jedno preklopenie listu [ms]. */
const FLIP_PERIOD = 240;

interface Props {
  board: Board;
  fit?: "width" | "contain";
  className?: string;
  ref?: Ref<BoardViewHandle>;
}

export function FlapView({ board, fit = "width", className, ref }: Props) {
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [space, setSpace] = useState({ width: 0, height: 0 });
  const layout = useMemo(() => layoutFlaps(board), [board]);
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

  const cssScale =
    space.width > 0
      ? fit === "contain" && space.height > 0
        ? Math.min(space.width / layout.width, space.height / layout.height)
        : space.width / layout.width
      : 0;
  const scale = cssScale * (window.devicePixelRatio || 1);

  useImperativeHandle(
    ref,
    () => ({
      snapshot: () => {
        const snapshot = layoutFlaps(latest.current);
        const target = document.createElement("canvas");
        target.width = snapshot.width * 2;
        target.height = snapshot.height * 2;
        drawFlaps(target.getContext("2d")!, snapshot, 2);
        return new Promise<Blob>((resolve, reject) => target.toBlob((blob) => (blob ? resolve(blob) : reject(new Error("PNG"))), "image/png"));
      },
    }),
    [],
  );

  // texty listov z predchadzajuceho kreslenia a zaciatky otacania zmenenych listov
  const previous = useRef(new Map<string, string>());
  const flips = useRef(new Map<string, number>());
  const rotation = Math.min(FLIP_MAX, Math.max(FLIP_MIN, board.profile.rotationMs));

  useEffect(() => {
    const el = canvas.current;
    if (!el || cssScale <= 0) return;
    el.width = Math.max(1, Math.round(layout.width * scale));
    el.height = Math.max(1, Math.round(layout.height * scale));
    const ctx = el.getContext("2d");
    if (!ctx) return;

    const start = performance.now();
    for (const row of layout.rows)
      for (const cell of row.cells) {
        const old = previous.current.get(cell.key);
        if (old !== undefined && old !== cell.label) flips.current.set(cell.key, start);
        previous.current.set(cell.key, cell.label);
      }

    let raf = 0;
    const draw = () => {
      const now = performance.now();
      let active = false;
      drawFlaps(ctx, layout, scale, (key) => {
        const begin = flips.current.get(key);
        if (begin === undefined) return null;
        const age = now - begin;
        if (age >= rotation) {
          flips.current.delete(key);
          return null;
        }
        active = true;
        return Math.abs(Math.cos((age / FLIP_PERIOD) * Math.PI));
      });
      if (active) raf = requestAnimationFrame(draw);
    };
    draw();
    return () => cancelAnimationFrame(raf);
  }, [layout, scale, cssScale, rotation]);

  return (
    <div ref={box} className={cn("w-full leading-0", fit === "contain" && "flex h-full items-center justify-center", className)}>
      <canvas
        ref={canvas}
        className="block max-w-full rounded-sm"
        style={cssScale > 0 ? { width: `${layout.width * cssScale}px`, height: `${layout.height * cssScale}px` } : { visibility: "hidden" }}
      />
    </div>
  );
}
