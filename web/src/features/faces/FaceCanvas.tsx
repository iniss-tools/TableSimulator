// Platno editora ciel: tabula v strede na neutralnom pozadi, prisposobena miestu alebo v pevnej mierke (Ctrl+koliesko),
// posun platna medzernikom s tahanim, klik mimo prvkov zrusi vyber. Nad tabulou je vrstva s obrysmi prvkov.
import { useCallback, useEffect, useMemo, useRef, useState, type PointerEvent } from "react";
import type { BoardDto, BoardFaceDto, FaceElementDto } from "@/api";
import { BoardView } from "@/features/boards/BoardView";
import type { FaceOverlayLayout } from "@/render/face";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { FaceCanvasEditor } from "./FaceCanvasEditor";
import type { FaceView } from "./view";
import { typing, zoomStep } from "./zoom";

interface Props {
  board: BoardDto;
  /** Predloha v editore (vsetky prvky). */
  face: BoardFaceDto;
  /** Indexy prvkov, ktore sa kreslia (skryte v editore chybaju). */
  shown: number[];
  /** Vybrane prvky. */
  selection: number[];
  readOnly: boolean;
  /** Obrysy, mriezka, prichytavanie a vodiace ciary. */
  view: FaceView;
  /** Pevna mierka; null = tabula sa prisposobi miestu. */
  zoom: number | null;
  onZoom: (zoom: number | null) => void;
  /** Mierka pri prisposobeni miestu (na ukazanie v percentach). */
  onFitScale: (scale: number) => void;
  onSelect: (selection: number[]) => void;
  onChange: (changes: Map<number, FaceElementDto>, transient: boolean) => void;
  onRemove: (indices: number[]) => void;
  /** Rozlozenie prvkov po prekresleni (zarovnanie). */
  onLayout?: (layout: FaceOverlayLayout) => void;
  /** Dvojklik na prvok. */
  onOpen?: (index: number) => void;
}

export function FaceCanvas({ board, face, shown, selection, readOnly, view, zoom, onZoom, onFitScale, onSelect, onChange, onRemove, onLayout, onOpen }: Props) {
  const area = useRef<HTMLDivElement>(null);
  const [space, setSpace] = useState(false);
  const [panning, setPanning] = useState(false);
  const pan = useRef<{ pointer: number; x: number; y: number; left: number; top: number } | null>(null);
  // mys nad platnom - len vtedy patri medzernik platnu (inak aktivuje tlacidlo)
  const hover = useRef(false);
  const [scale, setScale] = useState(1);
  // stabilny objekt - nove celo prekresli tabulu a prepocita polohy prvkov
  const shownKey = shown.join(",");
  const visible = useMemo(
    () => (shown.length === face.elements.length ? face : { ...face, elements: shown.map((i) => face.elements[i]) }),
    // eslint-disable-next-line react-hooks/exhaustive-deps -- shown sa porovnava podla obsahu
    [face, shownKey],
  );

  const reportScale = useCallback(
    (value: number) => {
      setScale(value);
      if (zoom === null) onFitScale(value);
    },
    [zoom, onFitScale],
  );

  // medzernik drzany nad platnom mimo poli formulara = posun platna
  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      if (e.code === "Space" && !e.repeat && hover.current && !typing(e.target)) {
        e.preventDefault();
        setSpace(true);
      }
    };
    const up = (e: KeyboardEvent) => {
      if (e.code === "Space") setSpace(false);
    };
    const blur = () => setSpace(false);
    window.addEventListener("keydown", down);
    window.addEventListener("keyup", up);
    window.addEventListener("blur", blur);
    return () => {
      window.removeEventListener("keydown", down);
      window.removeEventListener("keyup", up);
      window.removeEventListener("blur", blur);
    };
  }, []);

  // Ctrl+koliesko = mierka (pasivne posluchace prehliadac nedovoli zrusit, preto priamo)
  useEffect(() => {
    const el = area.current;
    if (!el) return;
    const wheel = (e: WheelEvent) => {
      if (!e.ctrlKey && !e.metaKey) return;
      e.preventDefault();
      onZoom(zoomStep(zoom ?? scale, e.deltaY < 0 ? 1 : -1));
    };
    el.addEventListener("wheel", wheel, { passive: false });
    return () => el.removeEventListener("wheel", wheel);
  }, [zoom, scale, onZoom]);

  const startPan = (e: PointerEvent<HTMLDivElement>) => {
    if (!space || !area.current) return;
    e.stopPropagation();
    e.preventDefault();
    // ukazovatel, ktory prehliadac nepozna (napr. udalost zo skriptu), sa zachytit neda - tahanie funguje aj bez toho
    try {
      e.currentTarget.setPointerCapture(e.pointerId);
    } catch {
      // bez zachytenia
    }
    pan.current = { pointer: e.pointerId, x: e.clientX, y: e.clientY, left: area.current.scrollLeft, top: area.current.scrollTop };
    setPanning(true);
  };

  const endPan = () => {
    pan.current = null;
    setPanning(false);
  };

  const movePan = (e: PointerEvent<HTMLDivElement>) => {
    const p = pan.current;
    if (!p || p.pointer !== e.pointerId || !area.current) return;
    area.current.scrollLeft = p.left - (e.clientX - p.x);
    area.current.scrollTop = p.top - (e.clientY - p.y);
  };

  return (
    <div
      ref={area}
      aria-label={t("faces.canvas.area")}
      className={cn(
        "relative size-full overflow-auto rounded-xl border bg-muted/50 bg-[radial-gradient(var(--color-border)_1px,transparent_1px)] [background-size:16px_16px]",
        space && (panning ? "cursor-grabbing" : "cursor-grab"),
      )}
      onPointerEnter={() => (hover.current = true)}
      onPointerLeave={() => (hover.current = false)}
      onPointerDownCapture={startPan}
      onPointerMove={movePan}
      onPointerUp={endPan}
      onPointerCancel={endPan}
      onPointerDown={(e) => {
        // klik na prazdne miesto platna (nie na tabulu) zrusi vyber
        if (e.target === e.currentTarget || (e.target as HTMLElement).dataset.canvasBackground !== undefined) onSelect([]);
      }}
    >
      <div data-canvas-background="" className={cn("flex items-center justify-center p-6", zoom === null ? "size-full" : "min-h-full w-max min-w-full")}>
        <BoardView
          board={board}
          face={visible}
          fit="contain"
          className={zoom === null ? "h-full" : undefined}
          scale={zoom ?? undefined}
          onScale={reportScale}
          overlay={(layout) => (
            <FaceCanvasEditor
              layout={layout}
              face={face}
              indices={shown}
              selection={selection}
              readOnly={readOnly || space}
              view={view}
              onSelect={onSelect}
              onChange={onChange}
              onRemove={onRemove}
              onLayout={onLayout}
              onOpen={onOpen}
            />
          )}
        />
      </div>
    </div>
  );
}
