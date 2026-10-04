// Ziva tabula: rasterizacia obsahu + LED renderer, prisposobena velkosti miesta.
import { useEffect, useRef, useState } from "react";
import type { Board } from "../api";
import { useLive } from "../store";
import { DEFAULT_DESIGN, defaultDesignId, loadDesign, type LedDesign } from "../render/design";
import { CanvasGlyphSource, CellGlyphSource, ElenGlyphSource, loadFontSet, type ElenFontSet, type GlyphSource } from "../render/fonts";
import { CHAR_WIDTH } from "../render/chars";
import { blinkPhase, boardClock, hasClock, isAnimated, rasterize } from "../render/rasterize";
import { LedRenderer, ledSize } from "../render/led";

interface Props {
  board: Board;
  /** width = na sirku miesta, contain = cela tabula do miesta (kiosk). */
  fit?: "width" | "contain";
}

export function BoardView({ board, fit = "width" }: Props) {
  const { designVersion } = useLive();
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [design, setDesign] = useState<LedDesign>(DEFAULT_DESIGN);
  const [fontSet, setFontSet] = useState<ElenFontSet | null>(null);
  const [space, setSpace] = useState({ width: 0, height: 0 });

  const designId = board.design || defaultDesignId(board.display.family);
  useEffect(() => {
    let live = true;
    loadDesign(designId, designVersion).then((d) => live && setDesign(d));
    return () => {
      live = false;
    };
  }, [designId, designVersion]);

  const fontFile = board.profile.fontFile;
  useEffect(() => {
    let live = true;
    if (!fontFile) setFontSet(null);
    else loadFontSet(fontFile).then((f) => live && setFontSet(f));
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
  const cssScale = space.width > 0 ? (fit === "contain" && space.height > 0 ? Math.min(space.width / natural.width, space.height / natural.height) : space.width / natural.width) : 0;

  const source = fontSet ? new ElenGlyphSource(fontSet, board.profile.fontMap) : null;
  inputs.current = { board, design, source, scale: cssScale * (window.devicePixelRatio || 1) };

  useEffect(() => {
    if (!canvas.current || cssScale <= 0) return;
    renderer.current ??= new LedRenderer(canvas.current);
    let fallback: GlyphSource | null = null;
    let fallbackKey = "";
    let raf = 0;
    let last = 0;

    const draw = (now: number) => {
      const { board: b, design: d, source: s, scale } = inputs.current;
      let glyphs = s;
      if (!glyphs) {
        // znakova tabula ma pevnu bunku na znak, ELEN pismo s vlastnymi sirkami znakov
        const key = `${b.display.family}|${b.profile.rowHeight}`;
        if (!fallback || fallbackKey !== key) {
          fallback = b.display.family === "chars" ? new CellGlyphSource(CHAR_WIDTH, b.profile.rowHeight) : new CanvasGlyphSource(b.profile.rowHeight);
          fallbackKey = key;
        }
        glyphs = fallback;
      }
      const t = Date.now();
      const frame = rasterize(b, glyphs, t);
      const clock = boardClock(b, t);
      renderer.current!.render(
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
      last = now;
    };

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
    return () => cancelAnimationFrame(raf);
  }, [board, design, fontSet, cssScale]);

  return (
    <div ref={box} className={fit === "contain" ? "board-view board-view-contain" : "board-view"}>
      <canvas
        ref={canvas}
        style={cssScale > 0 ? { width: `${natural.width * cssScale}px`, height: `${natural.height * cssScale}px` } : { visibility: "hidden" }}
      />
    </div>
  );
}
