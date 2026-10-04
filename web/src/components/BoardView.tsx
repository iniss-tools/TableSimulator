// Ziva tabula: rasterizacia obsahu + LED renderer, prisposobena velkosti miesta.
import { useEffect, useRef, useState } from "react";
import type { Board } from "../api";
import { useLive } from "../store";
import { DEFAULT_DESIGN, DEFAULT_DESIGN_ID, loadDesign, type LedDesign } from "../render/design";
import { CanvasGlyphSource, ElenGlyphSource, loadFontSet, type ElenFontSet, type GlyphSource } from "../render/fonts";
import { blinkPhase, isAnimated, rasterize } from "../render/rasterize";
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

  const designId = board.design || DEFAULT_DESIGN_ID;
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
  const natural = ledSize({ width: board.profile.width, height: board.profile.rows * rowHeight }, design, rowHeight);
  const cssScale = space.width > 0 ? (fit === "contain" && space.height > 0 ? Math.min(space.width / natural.width, space.height / natural.height) : space.width / natural.width) : 0;

  const source = fontSet ? new ElenGlyphSource(fontSet, board.profile.fontMap) : null;
  inputs.current = { board, design, source, scale: cssScale * (window.devicePixelRatio || 1) };

  useEffect(() => {
    if (!canvas.current || cssScale <= 0) return;
    renderer.current ??= new LedRenderer(canvas.current);
    let fallback: CanvasGlyphSource | null = null;
    let fallbackHeight = 0;
    let raf = 0;
    let last = 0;

    const draw = (now: number) => {
      const { board: b, design: d, source: s, scale } = inputs.current;
      let glyphs = s;
      if (!glyphs) {
        if (!fallback || fallbackHeight !== b.profile.rowHeight) {
          fallback = new CanvasGlyphSource(b.profile.rowHeight);
          fallbackHeight = b.profile.rowHeight;
        }
        glyphs = fallback;
      }
      const t = Date.now();
      const frame = rasterize(b, glyphs, t);
      renderer.current!.render(frame, d, b.profile.rowHeight, b.display.brightness, blinkPhase(b.profile, t), scale);
      last = now;
    };

    draw(performance.now());
    if (isAnimated(board)) {
      const tick = (now: number) => {
        if (now - last >= 33) draw(now);
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
