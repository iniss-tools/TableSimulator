// Uprava cela priamo v nahlade tabule: vrstva nad platnom s obrysmi prvkov. Klik vyberie prvok, tahanie ho posunie,
// uchyt na volnom okraji meni rozmer plochy alebo velkost pisma; sipky posuvaju vybrany prvok, Delete ho odstrani.
import { useRef, useState, type CSSProperties, type KeyboardEvent, type PointerEvent } from "react";
import type { BoardFaceDto, FaceElementDto } from "@/api";
import { cn } from "@/lib/utils";
import { anchorBox, type Box, type FaceOverlayLayout } from "@/render/face";
import { t } from "@/i18n";
import { COARSE, FINE, freeEdges, moveElement, referencePoint, resizeElement, SNAP, type DragMode } from "./edit";
import { elementLabel } from "./faces";

/** Najmensi obrys v CSS px, aby sa dal chytit aj tenky prvok. */
const MIN_HIT = 8;
/** Posun mysi v CSS px, od ktoreho sa klik meni na tahanie. */
const DRAG_START = 3;

interface Drag {
  index: number;
  mode: DragMode;
  pointer: number;
  x: number;
  y: number;
  element: FaceElementDto;
  /** Povodna plocha prvku v bodoch. */
  box: Box;
  started: boolean;
  /** Prva zmena tahania uz je krok historie. */
  changed: boolean;
}

interface Props {
  layout: FaceOverlayLayout;
  face: BoardFaceDto;
  selected: number | null;
  readOnly: boolean;
  outlines: boolean;
  onSelect: (index: number | null) => void;
  /** Zmena prvku; transient = pokracovanie tahania (nie novy krok historie). */
  onChange: (index: number, element: FaceElementDto, transient: boolean) => void;
  onRemove: (index: number) => void;
}

const number = (value: number) => String(Math.round(value * 100) / 100);

/** Co ukazat pri tahani: poloha, rozmer alebo pismo. */
function dragText(element: FaceElementDto, mode: DragMode): string {
  if (mode === "move") return `X ${number(element.x)} · Y ${number(element.y)}`;
  if (element.kind === "rect") return `${t("face.width")} ${number(element.width)} · ${t("face.height")} ${number(element.height)}`;
  return `${t("face.size")} ${number(element.size)}`;
}

export function FaceCanvasEditor({ layout, face, selected, readOnly, outlines, onSelect, onChange, onRemove }: Props) {
  const { scale, geometry: g, boxes } = layout;
  const root = useRef<HTMLDivElement>(null);
  const drag = useRef<Drag | null>(null);
  const [tip, setTip] = useState<{ text: string; index: number } | null>(null);

  const css = (b: Box): CSSProperties => ({ left: b.x * scale, top: b.y * scale, width: b.w * scale, height: b.h * scale });
  // obrys na chytenie: aspon MIN_HIT px okolo stredu prvku
  const hit = (b: Box): CSSProperties => {
    const w = Math.max(MIN_HIT, b.w * scale);
    const h = Math.max(MIN_HIT, b.h * scale);
    return { left: (b.x + b.w / 2) * scale - w / 2, top: (b.y + b.h / 2) * scale - h / 2, width: w, height: h };
  };

  const begin = (e: PointerEvent<HTMLElement>, index: number, mode: DragMode) => {
    if (e.button !== 0) return;
    e.stopPropagation();
    e.preventDefault();
    root.current?.focus({ preventScroll: true });
    onSelect(index);
    const box = boxes[index];
    if (readOnly || !box) return;
    e.currentTarget.setPointerCapture(e.pointerId);
    drag.current = {
      index,
      mode,
      pointer: e.pointerId,
      x: e.clientX,
      y: e.clientY,
      element: face.elements[index],
      box: { x: box.x / g.unit, y: box.y / g.unit, w: box.w / g.unit, h: box.h / g.unit },
      started: false,
      changed: false,
    };
  };

  const move = (e: PointerEvent<HTMLElement>) => {
    const d = drag.current;
    if (!d || d.pointer !== e.pointerId) return;
    const dxCss = e.clientX - d.x;
    const dyCss = e.clientY - d.y;
    if (!d.started && Math.hypot(dxCss, dyCss) < DRAG_START) return;
    d.started = true;
    const step = e.altKey ? FINE : SNAP;
    const dx = dxCss / (scale * g.unit);
    const dy = dyCss / (scale * g.unit);
    const next = d.mode === "move" ? moveElement(d.element, dx, dy, step) : resizeElement(d.element, d.mode, dx, dy, d.box, step);
    setTip({ text: dragText(next, d.mode), index: d.index });
    if (JSON.stringify(next) === JSON.stringify(face.elements[d.index])) return;
    onChange(d.index, next, d.changed);
    d.changed = true;
  };

  const end = (e: PointerEvent<HTMLElement>) => {
    if (drag.current?.pointer !== e.pointerId) return;
    drag.current = null;
    setTip(null);
  };

  const key = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.key === "Escape") {
      onSelect(null);
      return;
    }
    if (selected === null || readOnly || drag.current) return;
    const element = face.elements[selected];
    if (!element) return;
    if (e.key === "Delete" || e.key === "Backspace") {
      e.preventDefault();
      onRemove(selected);
      return;
    }
    const delta = e.shiftKey ? COARSE : e.altKey ? FINE : SNAP;
    const arrows: Record<string, [number, number]> = { ArrowLeft: [-delta, 0], ArrowRight: [delta, 0], ArrowUp: [0, -delta], ArrowDown: [0, delta] };
    const arrow = arrows[e.key];
    if (!arrow) return;
    e.preventDefault();
    onChange(selected, moveElement(element, arrow[0], arrow[1], e.altKey ? FINE : SNAP), false);
  };

  const current = selected !== null ? face.elements[selected] : undefined;
  const currentBox = selected !== null ? boxes[selected] : null;
  const anchor = current ? anchorBox(g, current) : null;
  const reference = current && anchor ? referencePoint(current, anchor) : null;

  // uchyty na volnych okrajoch vybraneho prvku: plocha sirka, vyska a roh, text a hodiny len roh (pismo)
  const handles: { index: number; mode: Exclude<DragMode, "move">; x: number; y: number; cursor: string }[] = [];
  if (selected !== null && current && currentBox && !readOnly) {
    const edges = freeEdges(current);
    const x = edges.left ? currentBox.x : currentBox.x + currentBox.w;
    const y = edges.top ? currentBox.y : currentBox.y + currentBox.h;
    const corner = edges.left === edges.top ? "nwse-resize" : "nesw-resize";
    if (current.kind === "rect") {
      handles.push({ index: selected, mode: "x", x, y: currentBox.y + currentBox.h / 2, cursor: "ew-resize" });
      handles.push({ index: selected, mode: "y", x: currentBox.x + currentBox.w / 2, y, cursor: "ns-resize" });
    }
    handles.push({ index: selected, mode: "xy", x, y, cursor: corner });
  }

  return (
    <div
      ref={root}
      tabIndex={0}
      role="group"
      aria-label={t("faces.canvas")}
      className="absolute inset-0 touch-none outline-none select-none focus-visible:ring-2 focus-visible:ring-ring"
      onPointerDown={(e) => {
        // klik mimo prvkov zrusi vyber
        if (e.target !== e.currentTarget && !(e.target instanceof HTMLElement && e.target.dataset.faceClip !== undefined)) return;
        root.current?.focus({ preventScroll: true });
        onSelect(null);
      }}
      onPointerMove={move}
      onPointerUp={end}
      onPointerCancel={end}
      onKeyDown={key}
    >
      {/* prvky mimo plochy tabule sa nekreslia - ani ich obrysy */}
      <div data-face-clip="" className="absolute inset-0 overflow-hidden">
        {anchor && <div className="pointer-events-none absolute border border-dashed border-amber-400/80 bg-amber-400/5" style={css(anchor)} aria-hidden />}
        {reference && (
          <div
            className="pointer-events-none absolute size-2 -translate-x-1/2 -translate-y-1/2 rounded-full border border-amber-200 bg-amber-500"
            style={{ left: reference.x * scale, top: reference.y * scale }}
            aria-hidden
          />
        )}
        {boxes.map((box, i) =>
          box ? (
            <div
              key={i}
              role="button"
              aria-label={elementLabel(face.elements[i])}
              aria-pressed={selected === i}
              data-face-element={i}
              className={cn(
                "absolute border",
                readOnly ? "cursor-pointer" : "cursor-move",
                selected === i
                  ? "border-sky-500 bg-sky-500/10 ring-1 ring-sky-300/60"
                  : cn("border-dashed hover:border-sky-400 hover:bg-sky-400/5", outlines ? "border-sky-400/45" : "border-transparent"),
              )}
              style={hit(box)}
              onPointerDown={(e) => begin(e, i, "move")}
            />
          ) : null,
        )}
      </div>
      {handles.map((h) => (
        <div
          key={h.mode}
          data-face-handle={h.mode}
          aria-hidden
          className="absolute z-10 size-2.5 -translate-x-1/2 -translate-y-1/2 rounded-[2px] border border-sky-600 bg-white shadow-sm"
          style={{ left: h.x * scale, top: h.y * scale, cursor: h.cursor }}
          onPointerDown={(e) => begin(e, h.index, h.mode)}
        />
      ))}
      {tip && boxes[tip.index] && (
        <div
          className="pointer-events-none absolute z-20 -translate-y-full rounded bg-popover px-1.5 py-0.5 text-xs leading-normal whitespace-nowrap text-popover-foreground shadow"
          style={{ left: boxes[tip.index]!.x * scale, top: boxes[tip.index]!.y * scale - 4 }}
        >
          {tip.text}
        </div>
      )}
    </div>
  );
}
