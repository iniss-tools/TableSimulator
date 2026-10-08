// Ziva tabula: rasterizacia obsahu + LED renderer, prisposobena velkosti miesta; snimka do PNG v pevnej mierke.
import { useEffect, useImperativeHandle, useLayoutEffect, useRef, useState, type Ref } from "react";
import { useConnection, type BoardDto as Board } from "@/api";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { DEFAULT_DESIGN, defaultDesignId, loadDesign, type LedDesign } from "@/render/design";
import { ElenGlyphSource, loadFontSet, type ElenFontSet, type GlyphSource } from "@/render/fonts";
import { fallbackGlyphSource } from "@/render/pixel-fonts";
import { CHAR_WIDTH } from "@/render/chars";
import { blinkPhase, boardClock, hasClock, isAnimated, rasterize } from "@/render/rasterize";
import { LedRenderer, ledSize } from "@/render/led";

// Prve kreslenie tabule trva desiatky ms - stranka s mnohymi tabulami ich kresli po jednej v samostatnych ulohach,
// aby sa medzitym vykreslila (kostry na miestach tabul) a nezamrzla.
const firstDraws: (() => void)[] = [];
let drawing = false;

function nextFirstDraw() {
  firstDraws.shift()?.();
  if (firstDraws.length) setTimeout(nextFirstDraw, 0);
  else drawing = false;
}

/** Zaradi prve kreslenie; vrati zrusenie. */
function queueFirstDraw(job: () => void): () => void {
  let cancelled = false;
  firstDraws.push(() => !cancelled && job());
  if (!drawing) {
    drawing = true;
    setTimeout(nextFirstDraw, 0);
  }
  return () => {
    cancelled = true;
  };
}

/** Mierka snimky: bod tabule = pitch px vzhladu; mala tabula dvojnasobne, aby sa dala citat. */
function snapshotScale(width: number): number {
  return width < 1000 ? 2 : 1;
}

/** Nahradne bodove pismo, ked tabula nema pismo ELEN (znakova ma pevnu bunku, ELEN vlastne sirky znakov). */
const fallbackFor = (b: Board) => fallbackGlyphSource(b.display.family, b.profile.rowHeight, CHAR_WIDTH);

/** Nakresli obsah tabule v case t (ms) - striedanie, blikanie, posun aj hodiny ako v danej chvili. */
function drawBoard(renderer: LedRenderer, b: Board, d: LedDesign, glyphs: GlyphSource, scale: number, t: number) {
  const frame = rasterize(b, glyphs, t);
  const clock = boardClock(b, t);
  renderer.render(
    frame,
    d,
    {
      rowHeight: b.profile.rowHeight,
      brightness: b.display.brightness,
      brightnessGreen: b.display.brightnessGreen ?? b.display.brightness,
      blinkOn: blinkPhase(b.profile, t, b.display),
      monochrome: b.profile.monochrome,
      swapRedGreen: b.profile.swapRedGreen,
      header: b.profile.header,
      clock: `${String(clock.getHours()).padStart(2, "0")}:${String(clock.getMinutes()).padStart(2, "0")}`,
    },
    scale,
  );
}

/** Ovladanie tabule zvonku (karta). */
export interface BoardViewHandle {
  /** Obsah tabule v tejto chvili ako PNG - nezavisle od velkosti miesta. */
  snapshot(): Promise<Blob>;
}

interface Props {
  board: Board;
  /** width = na sirku miesta, contain = cela tabula do miesta (kiosk). */
  fit?: "width" | "contain";
  className?: string;
  /** Vzhlad namiesto vzhladu tabule (nahlad v editore vzhladov). */
  design?: LedDesign;
  ref?: Ref<BoardViewHandle>;
}

export function BoardView({ board, fit = "width", className, design: override, ref }: Props) {
  const { designVersion } = useConnection();
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [loaded, setDesign] = useState<LedDesign>(DEFAULT_DESIGN);
  const design = override ?? loaded;
  const [space, setSpace] = useState({ width: 0, height: 0 });
  // co uz je nacitane - prvy raz sa kresli az so spravnym vzhladom a pismom (bez mihnutia nahradnych)
  const [loadedDesign, setLoadedDesign] = useState<string | null>(null);
  const [font, setFont] = useState<{ file: string; set: ElenFontSet | null } | null>(null);
  const [drawn, setDrawn] = useState(false);
  const firstDone = useRef(false);

  const designId = board.design || defaultDesignId(board.display.family);
  const designKey = `${designId}|${designVersion}`;
  useEffect(() => {
    let live = true;
    void loadDesign(designId, designVersion).then((d) => {
      if (!live) return;
      setDesign(d);
      setLoadedDesign(`${designId}|${designVersion}`);
    });
    return () => {
      live = false;
    };
  }, [designId, designVersion]);

  const fontFile = board.profile.fontFile;
  // pismo patri k suboru z profilu - po zmene suboru sa do nacitania noveho kresli nahradnym
  const fontSet = font?.file === fontFile ? font.set : null;
  useEffect(() => {
    let live = true;
    if (fontFile)
      void loadFontSet(fontFile).then((set) => {
        if (live) setFont({ file: fontFile, set });
      });
    return () => {
      live = false;
    };
  }, [fontFile]);

  useEffect(() => {
    const el = box.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => setSpace({ width: entry.contentRect.width, height: entry.contentRect.height }));
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  // vstupy kreslenia v refe, aby slucka animacie vzdy videla posledne hodnoty
  const inputs = useRef({ board, design, source: null as GlyphSource | null, scale: 1 });
  const renderer = useRef<LedRenderer | null>(null);

  const rowHeight = board.profile.rowHeight;
  const natural = ledSize({ width: board.profile.width, height: board.profile.rows * rowHeight }, design, rowHeight, board.profile.header);
  const cssScale =
    space.width > 0
      ? fit === "contain" && space.height > 0
        ? Math.min(space.width / natural.width, space.height / natural.height)
        : space.width / natural.width
      : 0;

  const source = fontSet ? new ElenGlyphSource(fontSet, board.profile.fontMap) : null;
  const scale = cssScale * (window.devicePixelRatio || 1);
  // pred efektom kreslenia (layout efekt bezi skor)
  useLayoutEffect(() => {
    inputs.current = { board, design, source, scale };
  });

  const ready = (override !== undefined || loadedDesign === designKey) && (!fontFile || font?.file === fontFile);

  useImperativeHandle(
    ref,
    () => ({
      snapshot: () => {
        const { board: b, design: d, source: s } = inputs.current;
        const canvas = document.createElement("canvas");
        const size = ledSize({ width: b.profile.width, height: b.profile.rows * b.profile.rowHeight }, d, b.profile.rowHeight, b.profile.header);
        drawBoard(new LedRenderer(canvas), b, d, s ?? fallbackFor(b), snapshotScale(size.width), Date.now());
        return new Promise<Blob>((resolve, reject) => canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error("PNG"))), "image/png"));
      },
    }),
    [],
  );

  useEffect(() => {
    if (!canvas.current || cssScale <= 0 || (!firstDone.current && !ready)) return;
    renderer.current ??= new LedRenderer(canvas.current);
    let fallback: GlyphSource | null = null;
    let fallbackKey = "";
    let raf = 0;
    let last = 0;

    const draw = (now: number) => {
      const { board: b, design: d, source: s, scale } = inputs.current;
      let glyphs = s;
      if (!glyphs) {
        const key = `${b.display.family}|${b.profile.rowHeight}`;
        if (!fallback || fallbackKey !== key) {
          fallback = fallbackFor(b);
          fallbackKey = key;
        }
        glyphs = fallback;
      }
      drawBoard(renderer.current!, b, d, glyphs, scale, Date.now());
      last = now;
    };

    const start = () => {
      draw(performance.now());
      // animacia: striedanie, blikanie a posun ~30× za sekundu; samotne hodiny staci raz za sekundu
      const animated = isAnimated(board);
      if (animated || hasClock(board)) {
        const interval = animated ? 33 : 1000;
        const tick = (now: number) => {
          if (now - last >= interval) draw(now);
          raf = requestAnimationFrame(tick);
        };
        raf = requestAnimationFrame(tick);
      }
    };
    let cancel: (() => void) | null = null;
    if (firstDone.current) start();
    else
      cancel = queueFirstDraw(() => {
        firstDone.current = true;
        start();
        setDrawn(true);
      });
    return () => {
      cancel?.();
      cancelAnimationFrame(raf);
    };
  }, [board, design, fontSet, cssScale, ready]);

  return (
    <div ref={box} className={cn("w-full leading-0", fit === "contain" && "flex h-full items-center justify-center", className)}>
      <div className="relative max-w-full">
        <canvas
          ref={canvas}
          className={cn("block max-w-full", !drawn && "invisible")}
          style={cssScale > 0 ? { width: `${natural.width * cssScale}px`, height: `${natural.height * cssScale}px` } : { visibility: "hidden" }}
        />
        {!drawn && <Skeleton className="absolute inset-0 rounded-sm" />}
      </div>
    </div>
  );
}
