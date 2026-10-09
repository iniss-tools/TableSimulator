// Spinacia jednotka zosilnovaca ELSVO: kontrolka zopnutia a bity stavu, prisposobene velkosti miesta.
import { useEffect, useImperativeHandle, useLayoutEffect, useRef, useState, type Ref } from "react";
import type { AmplifierDto, BoardDto as Board } from "@/api";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { AMPLIFIER_HEIGHT, AMPLIFIER_WIDTH, drawAmplifier, type AmplifierTexts } from "@/render/amplifier";
import type { BoardViewHandle } from "./BoardView";

/** Prikaz uvolnenia po hlaseni (inak vypnutie). */
const RELEASE = 0xc0;

const OFF: AmplifierDto = { band: 0, state: 0, onSince: null, switches: 0, lastCommand: null };

function texts(amplifier: AmplifierDto): AmplifierTexts {
  const title =
    amplifier.band > 0 ? t("amplifier.on", { band: amplifier.band }) : amplifier.lastCommand === RELEASE ? t("amplifier.released") : t("amplifier.off");
  const state = amplifier.state.toString(16).toUpperCase().padStart(2, "0");
  return { title, detail: t("amplifier.detail", { state, switches: amplifier.switches }) };
}

interface Props {
  board: Board;
  fit?: "width" | "contain";
  className?: string;
  ref?: Ref<BoardViewHandle>;
}

export function AmplifierView({ board, fit = "width", className, ref }: Props) {
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [space, setSpace] = useState({ width: 0, height: 0 });
  const amplifier = board.display.amplifier ?? OFF;
  const latest = useRef(amplifier);
  useLayoutEffect(() => {
    latest.current = amplifier;
  });

  useEffect(() => {
    const el = box.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => setSpace({ width: entry.contentRect.width, height: entry.contentRect.height }));
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  // na sirku miesta, no najviac dvojnasobok - jednotka nema byt cez celu siroku kartu
  const cssScale =
    space.width > 0
      ? fit === "contain" && space.height > 0
        ? Math.min(space.width / AMPLIFIER_WIDTH, space.height / AMPLIFIER_HEIGHT)
        : Math.min(2, space.width / AMPLIFIER_WIDTH)
      : 0;
  const scale = cssScale * (window.devicePixelRatio || 1);

  useImperativeHandle(
    ref,
    () => ({
      snapshot: () => {
        const target = document.createElement("canvas");
        target.width = AMPLIFIER_WIDTH * 2;
        target.height = AMPLIFIER_HEIGHT * 2;
        drawAmplifier(target.getContext("2d")!, latest.current, texts(latest.current), 2);
        return new Promise<Blob>((resolve, reject) => target.toBlob((blob) => (blob ? resolve(blob) : reject(new Error("PNG"))), "image/png"));
      },
    }),
    [],
  );

  useEffect(() => {
    const el = canvas.current;
    if (!el || cssScale <= 0) return;
    el.width = Math.max(1, Math.round(AMPLIFIER_WIDTH * scale));
    el.height = Math.max(1, Math.round(AMPLIFIER_HEIGHT * scale));
    const ctx = el.getContext("2d");
    if (ctx) drawAmplifier(ctx, amplifier, texts(amplifier), scale);
  }, [amplifier, scale, cssScale]);

  return (
    <div ref={box} className={cn("flex w-full justify-center leading-0", fit === "contain" && "h-full items-center", className)}>
      <canvas
        ref={canvas}
        className="block max-w-full rounded-lg"
        style={cssScale > 0 ? { width: `${AMPLIFIER_WIDTH * cssScale}px`, height: `${AMPLIFIER_HEIGHT * cssScale}px` } : { visibility: "hidden" }}
      />
    </div>
  );
}
