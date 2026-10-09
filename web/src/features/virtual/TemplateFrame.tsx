// Stranka virtualnej tabule zo sablony XSL INISSView v ramci: dva ramy - nova stranka sa nacita do skryteho a ukaze
// sa az nacitana (obnova bez bliknutia). Skript stranky (JScript.js z INISSView) bezi v ramci ako v INISSView;
// relativne adresy stranky smeruju do priecinka INISSView (base /inissview/app/).
import { useLayoutEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";

/** Rozmer, na ktory su sablony INISSView spravidla robene - nahlad ho zmensi do miesta. */
const DESIGN = { width: 1920, height: 1080 };

interface TemplateFrameProps {
  html: string;
  title: string;
  /** Nahlad - stranka v rozmere 1920 × 1080 zmensena do miesta; inak na cele miesto ako v prehliadaci. */
  preview?: boolean;
  className?: string;
}

export function TemplateFrame({ html, title, preview, className }: TemplateFrameProps) {
  // obsah oboch ramov a ktory je viditelny
  const [frames, setFrames] = useState<[string, string]>([html, ""]);
  const [front, setFront] = useState(0);
  const back = 1 - front;
  const box = useRef<HTMLDivElement>(null);
  const [scale, setScale] = useState(1);

  // nova stranka do skryteho ramu; ukaze sa po nacitani
  if (frames[front] !== html && frames[back] !== html) {
    const next: [string, string] = [frames[0], frames[1]];
    next[back] = html;
    setFrames(next);
  }

  useLayoutEffect(() => {
    const el = box.current;
    if (!preview || !el) return;
    const observer = new ResizeObserver(([entry]) => setScale(entry.contentRect.width / DESIGN.width));
    observer.observe(el);
    return () => observer.disconnect();
  }, [preview]);

  const frame = (index: number) => (
    <iframe
      key={index}
      title={title}
      srcDoc={frames[index]}
      // skript stranky zapisuje cookie - potrebuje vlastny povod ako stranka INISSView
      sandbox="allow-scripts allow-same-origin"
      onLoad={() => index === back && frames[index] === html && setFront(index)}
      className={cn("absolute top-0 left-0 border-0 bg-white", index === front ? "visible" : "invisible")}
      style={preview ? { width: DESIGN.width, height: DESIGN.height, transform: `scale(${scale})`, transformOrigin: "0 0" } : { width: "100%", height: "100%" }}
    />
  );

  return (
    <div
      ref={box}
      className={cn("relative size-full overflow-hidden", className)}
      style={preview ? { aspectRatio: `${DESIGN.width} / ${DESIGN.height}` } : undefined}
    >
      {frame(0)}
      {frame(1)}
    </div>
  );
}
