// Uprava cela priamo v nahlade tabule: vrstva nad platnom s obrysmi prvkov. Klik vyberie prvok, Ctrl alebo Shift+klik
// ho prida k vyberu alebo odoberie, tahanie po prazdnom mieste vyberie prvky obdlznikom. Tahanie posunie vsetky vybrane
// prvky, uchyt na volnom okraji (pri jednom prvku) meni rozmer plochy alebo velkost pisma; sipky posuvaju vyber,
// Delete ho odstrani. Zamknuty prvok sa na platne neda chytit (vybrat sa da vo vrstvach), skryty sa nekresli. Pri
// tahani sa vyber prichytava k mriezke a k okrajom a stredom inych prvkov a struktury tabule (Alt prichytenie vypne)
// a vodiace ciary ukazu zarovnanie a vzdialenost k susedovi v bodoch.
import { useEffect, useRef, useState, type CSSProperties, type KeyboardEvent, type PointerEvent } from "react";
import type { BoardFaceDto, FaceElementDto } from "@/api";
import { cn } from "@/lib/utils";
import { anchorBox, type Box, type FaceOverlayLayout } from "@/render/face";
import { t } from "@/i18n";
import { intersecting, union } from "./align";
import { COARSE, FINE, freeEdges, moveElement, referencePoint, resizeElement, SNAP, type DragMode } from "./edit";
import { elementLabel } from "./faces";
import { snapEdge, snapMove, targetsOf, type Gap, type Guide, type SnapOptions, type SnapTargets } from "./snap";
import type { FaceView } from "./view";

/** Najmensi obrys v CSS px, aby sa dal chytit aj tenky prvok. */
const MIN_HIT = 8;
/** Posun mysi v CSS px, od ktoreho sa klik meni na tahanie. */
const DRAG_START = 3;
/** Dosah prichytenia v CSS px. */
const SNAP_REACH = 6;
/** Najhustejsia viditelna mriezka v CSS px - hustejsia sa nekresli (prichytavanie plati dalej). */
const GRID_MIN_PX = 4;

interface Drag {
  /** Prvok, za ktory sa taha. */
  index: number;
  mode: DragMode;
  pointer: number;
  x: number;
  y: number;
  /** Povodne prvky, ktore sa hybu (vyber bez zamknutych). */
  elements: Map<number, FaceElementDto>;
  /** Povodna plocha tahanych prvkov v bodoch (pri zmene rozmeru plocha prvku). */
  box: Box;
  started: boolean;
  /** Prva zmena tahania uz je krok historie. */
  changed: boolean;
  /** Ciele prichytenia v bodoch (ostatne prvky, body, riadky, plocha cela). */
  targets: SnapTargets;
  /** Klik na uz vybrany prvok bez posunu ho vyberie samotny. */
  narrow: boolean;
}

/** Vyber obdlznikom: zaciatok a aktualny roh v CSS px vrstvy. */
interface Marquee {
  pointer: number;
  x0: number;
  y0: number;
  x1: number;
  y1: number;
  /** Ctrl/Shift - pridat k doterajsiemu vyberu. */
  add: boolean;
}

interface Props {
  layout: FaceOverlayLayout;
  face: BoardFaceDto;
  /** Index prvku cela ku kazdej ploche rozlozenia (skryte prvky sa nekreslia, ich plochy chybaju). */
  indices: number[];
  /** Vybrane prvky (indexy v predlohe). */
  selection: number[];
  readOnly: boolean;
  view: FaceView;
  onSelect: (selection: number[]) => void;
  /** Zmena prvkov podla indexu; transient = pokracovanie tahania (nie novy krok historie). */
  onChange: (changes: Map<number, FaceElementDto>, transient: boolean) => void;
  onRemove: (indices: number[]) => void;
  /** Rozlozenie po kazdom prekresleni (zarovnanie v paneli vlastnosti potrebuje plochy prvkov). */
  onLayout?: (layout: FaceOverlayLayout) => void;
}

const number = (value: number) => String(Math.round(value * 100) / 100);

/** Co ukazat pri tahani: poloha, rozmer alebo pismo. */
function dragText(element: FaceElementDto, mode: DragMode): string {
  if (mode === "move") return `X ${number(element.x)} · Y ${number(element.y)}`;
  if (element.kind === "rect" || element.kind === "image") return `${t("face.width")} ${number(element.width)} · ${t("face.height")} ${number(element.height)}`;
  return `${t("face.size")} ${number(element.size)}`;
}

export function FaceCanvasEditor({ layout, face, indices, selection, readOnly, view, onSelect, onChange, onRemove, onLayout }: Props) {
  const { scale, geometry: g, boxes } = layout;
  /** Plocha prvku podla indexu v celej predlohe; null = skryty alebo bez ukotvenia. */
  const boxOf = (index: number) => {
    const at = indices.indexOf(index);
    return at < 0 ? null : (boxes[at] ?? null);
  };
  const root = useRef<HTMLDivElement>(null);
  const drag = useRef<Drag | null>(null);
  const [marquee, setMarquee] = useState<Marquee | null>(null);
  const [tip, setTip] = useState<{ text: string; index: number } | null>(null);
  const [lines, setLines] = useState<{ guides: Guide[]; gaps: Gap[] } | null>(null);
  const points = (b: Box): Box => ({ x: b.x / g.unit, y: b.y / g.unit, w: b.w / g.unit, h: b.h / g.unit });

  useEffect(() => {
    onLayout?.(layout);
  }, [layout, onLayout]);

  const css = (b: Box): CSSProperties => ({ left: b.x * scale, top: b.y * scale, width: b.w * scale, height: b.h * scale });
  // obrys na chytenie: aspon MIN_HIT px okolo stredu prvku
  const hit = (b: Box): CSSProperties => {
    const w = Math.max(MIN_HIT, b.w * scale);
    const h = Math.max(MIN_HIT, b.h * scale);
    return { left: (b.x + b.w / 2) * scale - w / 2, top: (b.y + b.h / 2) * scale - h / 2, width: w, height: h };
  };
  const capture = (e: PointerEvent<HTMLElement>) => {
    // ukazovatel, ktory prehliadac nepozna (napr. udalost zo skriptu), sa zachytit neda - tahanie funguje aj bez toho
    try {
      e.currentTarget.setPointerCapture(e.pointerId);
    } catch {
      // bez zachytenia
    }
  };

  const begin = (e: PointerEvent<HTMLElement>, index: number, mode: DragMode) => {
    if (e.button !== 0) return;
    e.stopPropagation();
    e.preventDefault();
    root.current?.focus({ preventScroll: true });
    // Ctrl/Shift+klik prida prvok k vyberu alebo ho odoberie - bez tahania
    if (mode === "move" && (e.ctrlKey || e.metaKey || e.shiftKey)) {
      onSelect(selection.includes(index) ? selection.filter((i) => i !== index) : [...selection, index]);
      return;
    }
    const keep = mode !== "move" || selection.includes(index);
    const chosen = keep ? selection : [index];
    if (!keep) onSelect(chosen);
    const moving = new Map(chosen.flatMap((i) => (face.elements[i] && !face.elements[i].locked && boxOf(i) ? [[i, face.elements[i]] as const] : [])));
    const own = boxOf(index);
    if (readOnly || !own || face.elements[index]?.locked || moving.size === 0) return;
    capture(e);
    drag.current = {
      index,
      mode,
      pointer: e.pointerId,
      x: e.clientX,
      y: e.clientY,
      elements: moving,
      box: mode === "move" ? union([...moving.keys()].map((i) => points(boxOf(i)!))) : points(own),
      started: false,
      changed: false,
      targets: targetsOf([g.face, g.matrix, ...g.rows, ...boxes.filter((b, i) => b && !moving.has(indices[i])).map((b) => b!)].map(points)),
      narrow: keep && mode === "move" && selection.length > 1,
    };
  };

  const move = (e: PointerEvent<HTMLElement>) => {
    const m = marquee;
    if (m && m.pointer === e.pointerId && root.current) {
      const rect = root.current.getBoundingClientRect();
      setMarquee({ ...m, x1: e.clientX - rect.left, y1: e.clientY - rect.top });
      return;
    }
    const d = drag.current;
    if (!d || d.pointer !== e.pointerId) return;
    const dxCss = e.clientX - d.x;
    const dyCss = e.clientY - d.y;
    if (!d.started && Math.hypot(dxCss, dyCss) < DRAG_START) return;
    d.started = true;
    let dx = dxCss / (scale * g.unit);
    let dy = dyCss / (scale * g.unit);
    // Alt = bez prichytenia, jemny krok; inak prichytenie k prvkom a mriezke, bez nich pol bodu
    const options: SnapOptions = {
      threshold: SNAP_REACH / (scale * g.unit),
      grid: view.snapGrid && !e.altKey ? { step: view.gridStep, originX: g.matrix.x / g.unit, originY: g.matrix.y / g.unit } : null,
      objects: view.guides && !e.altKey,
    };
    const snapping = options.objects || options.grid !== null;
    const step = e.altKey ? FINE : snapping ? 0.01 : SNAP;
    const element = d.elements.get(d.index)!;
    let guides: Guide[] = [];
    let gaps: Gap[] = [];
    if (snapping && d.mode === "move") {
      // skupina sa prichytava ako celok (mriezka podla okraja prvku, za ktory sa taha)
      const snapped = snapMove(d.box, dx, dy, d.targets, d.elements.size > 1 ? "left" : element.align, d.elements.size > 1 ? "top" : element.vAlign, options);
      ({ dx, dy, guides, gaps } = snapped);
    } else if (snapping && (element.kind === "rect" || element.kind === "image")) {
      // volny okraj plochy (pri zarovnani na stred sa meni symetricky - bez prichytenia)
      const free = freeEdges(element);
      if (d.mode !== "y" && element.align !== "center") {
        const r = snapEdge("x", free.left ? d.box.x : d.box.x + d.box.w, dx, d.box, d.targets, options);
        dx = r.delta;
        if (r.guide) guides.push(r.guide);
      }
      if (d.mode !== "x" && element.vAlign !== "middle") {
        const r = snapEdge("y", free.top ? d.box.y : d.box.y + d.box.h, dy, d.box, d.targets, options);
        dy = r.delta;
        if (r.guide) guides.push(r.guide);
      }
    }
    setLines(guides.length || gaps.length ? { guides, gaps } : null);
    const changes = new Map<number, FaceElementDto>();
    if (d.mode === "move") for (const [i, original] of d.elements) changes.set(i, moveElement(original, dx, dy, step));
    else changes.set(d.index, resizeElement(element, d.mode, dx, dy, d.box, step));
    setTip({ text: dragText(changes.get(d.index)!, d.mode), index: d.index });
    if ([...changes].every(([i, changed]) => JSON.stringify(changed) === JSON.stringify(face.elements[i]))) return;
    onChange(changes, d.changed);
    d.changed = true;
  };

  const end = (e: PointerEvent<HTMLElement>) => {
    const m = marquee;
    if (m && m.pointer === e.pointerId) {
      setMarquee(null);
      const rect = { x: Math.min(m.x0, m.x1) / scale, y: Math.min(m.y0, m.y1) / scale, w: Math.abs(m.x1 - m.x0) / scale, h: Math.abs(m.y1 - m.y0) / scale };
      // len klik na prazdne miesto = zrusit vyber
      if (Math.hypot(m.x1 - m.x0, m.y1 - m.y0) < DRAG_START) {
        if (!m.add) onSelect([]);
        return;
      }
      const hits = intersecting(boxes, rect)
        .map((i) => indices[i])
        .filter((i) => !face.elements[i]?.locked);
      onSelect(m.add ? [...selection, ...hits.filter((i) => !selection.includes(i))] : hits);
      return;
    }
    const d = drag.current;
    if (d?.pointer !== e.pointerId) return;
    if (!d.started && d.narrow) onSelect([d.index]);
    drag.current = null;
    setTip(null);
    setLines(null);
  };

  const key = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.key === "Escape") {
      onSelect([]);
      return;
    }
    if (selection.length === 0 || readOnly || drag.current) return;
    const unlocked = selection.filter((i) => face.elements[i] && !face.elements[i].locked);
    if (unlocked.length === 0) return;
    if (e.key === "Delete" || e.key === "Backspace") {
      e.preventDefault();
      onRemove(unlocked);
      return;
    }
    const delta = e.shiftKey ? COARSE : e.altKey ? FINE : SNAP;
    const arrows: Record<string, [number, number]> = { ArrowLeft: [-delta, 0], ArrowRight: [delta, 0], ArrowUp: [0, -delta], ArrowDown: [0, delta] };
    const arrow = arrows[e.key];
    if (!arrow) return;
    e.preventDefault();
    onChange(new Map(unlocked.map((i) => [i, moveElement(face.elements[i], arrow[0], arrow[1], e.altKey ? FINE : SNAP)])), false);
  };

  // jeden vybrany prvok: ukotvenie, bod merania a uchyty; viac prvkov: spolocna plocha
  const single = selection.length === 1 ? selection[0] : null;
  const current = single !== null ? face.elements[single] : undefined;
  const currentBox = single !== null ? boxOf(single) : null;
  const anchor = current ? anchorBox(g, current) : null;
  const reference = current && anchor ? referencePoint(current, anchor) : null;
  const selectedBoxes = selection.flatMap((i) => {
    const b = boxOf(i);
    return b ? [b] : [];
  });
  const group = selection.length > 1 && selectedBoxes.length > 0 ? union(selectedBoxes) : null;

  // uchyty na volnych okrajoch vybraneho prvku: plocha a obrazok sirka, vyska a roh, text, hodiny a symbol len roh (velkost)
  const handles: { index: number; mode: Exclude<DragMode, "move">; x: number; y: number; cursor: string }[] = [];
  if (single !== null && current && currentBox && !readOnly && !current.locked) {
    const edges = freeEdges(current);
    const x = edges.left ? currentBox.x : currentBox.x + currentBox.w;
    const y = edges.top ? currentBox.y : currentBox.y + currentBox.h;
    const corner = edges.left === edges.top ? "nwse-resize" : "nesw-resize";
    if (current.kind === "rect" || current.kind === "image") {
      handles.push({ index: single, mode: "x", x, y: currentBox.y + currentBox.h / 2, cursor: "ew-resize" });
      handles.push({ index: single, mode: "y", x: currentBox.x + currentBox.w / 2, y, cursor: "ns-resize" });
    }
    handles.push({ index: single, mode: "xy", x, y, cursor: corner });
  }

  return (
    <div
      ref={root}
      tabIndex={0}
      role="group"
      aria-label={t("faces.canvas")}
      className="absolute inset-0 touch-none outline-none select-none focus-visible:ring-2 focus-visible:ring-ring"
      onPointerDown={(e) => {
        // tahanie po prazdnom mieste = vyber obdlznikom (bez posunu zrusi vyber)
        if (e.button !== 0) return;
        if (e.target !== e.currentTarget && !(e.target instanceof HTMLElement && e.target.dataset.faceClip !== undefined)) return;
        root.current?.focus({ preventScroll: true });
        const rect = e.currentTarget.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        capture(e);
        setMarquee({ pointer: e.pointerId, x0: x, y0: y, x1: x, y1: y, add: e.ctrlKey || e.metaKey || e.shiftKey });
      }}
      onPointerMove={move}
      onPointerUp={end}
      onPointerCancel={end}
      onKeyDown={key}
    >
      {/* prvky mimo plochy tabule sa nekreslia - ani ich obrysy */}
      <div data-face-clip="" className="absolute inset-0 overflow-hidden">
        {view.grid && view.gridStep * g.unit * scale >= GRID_MIN_PX && (
          <div
            aria-hidden
            className="pointer-events-none absolute"
            style={{
              ...css(g.face),
              // ciary mriezky idu cez roh bodov tabule
              backgroundImage:
                "linear-gradient(to right, var(--grid-line) 1px, transparent 1px), linear-gradient(to bottom, var(--grid-line) 1px, transparent 1px)",
              backgroundSize: `${view.gridStep * g.unit * scale}px ${view.gridStep * g.unit * scale}px`,
              backgroundPosition: `${(g.matrix.x - g.face.x) * scale}px ${(g.matrix.y - g.face.y) * scale}px`,
              ["--grid-line" as string]: "rgb(56 189 248 / 0.35)",
            }}
          />
        )}
        {anchor && <div className="pointer-events-none absolute border border-dashed border-amber-400/80 bg-amber-400/5" style={css(anchor)} aria-hidden />}
        {reference && (
          <div
            className="pointer-events-none absolute size-2 -translate-x-1/2 -translate-y-1/2 rounded-full border border-amber-200 bg-amber-500"
            style={{ left: reference.x * scale, top: reference.y * scale }}
            aria-hidden
          />
        )}
        {boxes.map((box, i) => {
          const index = indices[i];
          const element = face.elements[index];
          const chosen = selection.includes(index);
          return box && element ? (
            <div
              key={index}
              role="button"
              aria-label={elementLabel(element)}
              aria-pressed={chosen}
              data-face-element={index}
              className={cn(
                "absolute border",
                element.locked ? "pointer-events-none" : readOnly ? "cursor-pointer" : "cursor-move",
                chosen
                  ? "border-sky-500 bg-sky-500/10 ring-1 ring-sky-300/60"
                  : cn("border-dashed hover:border-sky-400 hover:bg-sky-400/5", view.outlines ? "border-sky-400/45" : "border-transparent"),
              )}
              style={hit(box)}
              onPointerDown={(e) => begin(e, index, "move")}
            />
          ) : null;
        })}
        {group && <div aria-hidden className="pointer-events-none absolute border border-sky-500" style={css(group)} />}
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
      {marquee && (
        <div
          aria-hidden
          className="pointer-events-none absolute z-20 border border-sky-500 bg-sky-500/10"
          style={{
            left: Math.min(marquee.x0, marquee.x1),
            top: Math.min(marquee.y0, marquee.y1),
            width: Math.abs(marquee.x1 - marquee.x0),
            height: Math.abs(marquee.y1 - marquee.y0),
          }}
        />
      )}
      {lines?.guides.map((l, i) => (
        <div
          key={`g${i}`}
          aria-hidden
          className="pointer-events-none absolute z-10 bg-pink-500"
          style={
            l.axis === "x"
              ? { left: l.at * g.unit * scale, top: l.from * g.unit * scale, width: 1, height: (l.to - l.from) * g.unit * scale }
              : { left: l.from * g.unit * scale, top: l.at * g.unit * scale, width: (l.to - l.from) * g.unit * scale, height: 1 }
          }
        />
      ))}
      {lines?.gaps.map((gap, i) => {
        const start = gap.from * g.unit * scale;
        const length = (gap.to - gap.from) * g.unit * scale;
        const at = gap.at * g.unit * scale;
        return (
          <div
            key={`d${i}`}
            aria-hidden
            className={cn(
              "pointer-events-none absolute z-10 flex items-center justify-center border-pink-500",
              gap.axis === "y" ? "border-l border-dashed" : "border-t border-dashed",
            )}
            style={gap.axis === "y" ? { left: at, top: start, height: length, width: 0 } : { left: start, top: at, width: length, height: 0 }}
          >
            <span className="rounded bg-pink-500 px-1 text-[10px] leading-4 font-medium whitespace-nowrap text-white">{number(gap.to - gap.from)}</span>
          </div>
        );
      })}
      {tip && boxOf(tip.index) && (
        <div
          className="pointer-events-none absolute z-20 -translate-y-full rounded bg-popover px-1.5 py-0.5 text-xs leading-normal whitespace-nowrap text-popover-foreground shadow"
          style={{ left: boxOf(tip.index)!.x * scale, top: boxOf(tip.index)!.y * scale - 4 }}
        >
          {tip.text}
        </div>
      )}
    </div>
  );
}
