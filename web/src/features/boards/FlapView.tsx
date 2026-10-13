// Listova tabula (ERS, FERS): moduly s textami listov v okne cela, prisposobena velkosti miesta; zmenene listy sa
// chvilu otacaju.
import { useEffect, useImperativeHandle, useLayoutEffect, useMemo, useRef, useState, type ReactNode, type Ref } from "react";
import { useQuery } from "@tanstack/react-query";
import { getFacesOptions, useConnection, type BoardFaceDto, type BoardDto as Board } from "@/api";
import { cn } from "@/lib/utils";
import { DEFAULT_FLAP_DESIGN, defaultDesignId, designForFamily, faceStyle, loadDesign, type BoardDesign } from "@/render/design";
import { faceBoxes, faceValues, hasFaceClock, measureContext, resolveFace, type FaceOverlayLayout } from "@/render/face";
import { drawFlaps, layoutFlaps, type FlapFace } from "@/render/flaps";
import { loadFaceImages } from "@/render/images";
import { boardClock } from "@/render/rasterize";
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
  /** Celo namiesto cela tabule (nahlad); null = bez cela. */
  face?: BoardFaceDto | null;
  /** Vzhlad namiesto vzhladu tabule (nahlad v editore vzhladov). */
  design?: BoardDesign;
  /** Vrstva nad tabulou s polohami prvkov cela (editor ciel). */
  overlay?: (layout: FaceOverlayLayout) => ReactNode;
  /** Pevna mierka namiesto prisposobenia miestu - lupa editora. */
  scale?: number;
  /** Mierka, v ktorej sa tabula kresli. */
  onScale?: (scale: number) => void;
  ref?: Ref<BoardViewHandle>;
}

/** Celo s hodnotami a casom hodin tabule v case t. */
function flapFace(face: BoardFaceDto | null, board: Board, t: number): FlapFace | null {
  return face ? { face, values: faceValues(board), time: boardClock(board, t) } : null;
}

export function FlapView({ board, fit = "width", className, face: faceOverride, design: designOverride, overlay, scale: fixedScale, onScale, ref }: Props) {
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [space, setSpace] = useState({ width: 0, height: 0 });
  const faces = useQuery({ ...getFacesOptions(), enabled: faceOverride === undefined }).data;
  const { face: faceId, header } = board.profile;
  const resolved = useMemo(() => resolveFace(faceId, header, faces), [faceId, header, faces]);
  const face = faceOverride !== undefined ? faceOverride : resolved;

  // vzhlad listovej tabule; vzhlad bodov na nej neplati (predvoleny listovy)
  const { designVersion } = useConnection();
  const [loaded, setLoaded] = useState<BoardDesign>(DEFAULT_FLAP_DESIGN);
  const designId = board.design || defaultDesignId("flaps");
  useEffect(() => {
    if (designOverride !== undefined) return;
    let live = true;
    void loadDesign(designId, designVersion).then((d) => live && setLoaded(designForFamily(d, "flaps")));
    return () => {
      live = false;
    };
  }, [designId, designVersion, designOverride]);
  const design = designOverride ?? loaded;

  const layout = useMemo(() => layoutFlaps(board, face, design.frame.width), [board, face, design]);
  // obrazky cela (logo) - po nacitani sa tabula prekresli
  const [images, setImages] = useState(0);
  useEffect(() => {
    let live = true;
    void loadFaceImages(face).then((loaded) => live && loaded && setImages((n) => n + 1));
    return () => {
      live = false;
    };
  }, [face]);
  const latest = useRef({ board, face, design });
  useLayoutEffect(() => {
    latest.current = { board, face, design };
  });

  useEffect(() => {
    const el = box.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => setSpace({ width: entry.contentRect.width, height: entry.contentRect.height }));
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const cssScale =
    fixedScale ??
    (space.width > 0
      ? fit === "contain" && space.height > 0
        ? Math.min(space.width / layout.width, space.height / layout.height)
        : space.width / layout.width
      : 0);
  useEffect(() => {
    if (cssScale > 0) onScale?.(cssScale);
  }, [cssScale, onScale]);
  const scale = cssScale * (window.devicePixelRatio || 1);

  // polohy prvkov cela pre editor; geometria listov uz zacina v rohu platna, hodiny su vzdy pismom
  const editing = overlay !== undefined;
  const overlayLayout = useMemo((): FaceOverlayLayout | null => {
    if (!editing || !face || cssScale <= 0) return null;
    const g = layout.geometry;
    return { scale: cssScale, geometry: g, boxes: faceBoxes(measureContext(), face, g, faceStyle(design), faceValues(board), new Date(), false) };
  }, [editing, face, cssScale, layout, design, board]);

  useImperativeHandle(
    ref,
    () => ({
      snapshot: () => {
        const { board: b, face: f, design: d } = latest.current;
        const snapshot = layoutFlaps(b, f, d.frame.width);
        const target = document.createElement("canvas");
        target.width = snapshot.width * 2;
        target.height = snapshot.height * 2;
        drawFlaps(target.getContext("2d")!, snapshot, 2, d, undefined, flapFace(f, b, Date.now()));
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
      raf = 0;
      const now = performance.now();
      let active = false;
      drawFlaps(
        ctx,
        layout,
        scale,
        latest.current.design,
        (key) => {
          const begin = flips.current.get(key);
          if (begin === undefined) return null;
          const age = now - begin;
          if (age >= rotation) {
            flips.current.delete(key);
            return null;
          }
          active = true;
          return Math.abs(Math.cos((age / FLIP_PERIOD) * Math.PI));
        },
        flapFace(latest.current.face, latest.current.board, Date.now()),
      );
      if (active) raf = requestAnimationFrame(draw);
    };
    draw();
    // hodiny cela bez otacania listov - 4× za sekundu, aby sekundy neskakali
    const clock = hasFaceClock(face) ? setInterval(() => raf === 0 && draw(), 250) : undefined;
    return () => {
      cancelAnimationFrame(raf);
      clearInterval(clock);
    };
  }, [layout, scale, cssScale, rotation, face, design, images]);

  return (
    <div
      ref={box}
      className={cn(fixedScale ? "w-max" : "w-full", "leading-0", fit === "contain" && !fixedScale && "flex h-full items-center justify-center", className)}
    >
      <div className={cn("relative", !fixedScale && "max-w-full")}>
        <canvas
          ref={canvas}
          className={cn("block rounded-sm", !fixedScale && "max-w-full")}
          style={cssScale > 0 ? { width: `${layout.width * cssScale}px`, height: `${layout.height * cssScale}px` } : { visibility: "hidden" }}
        />
        {overlayLayout && overlay && <div className="absolute inset-0">{overlay(overlayLayout)}</div>}
      </div>
    </div>
  );
}
