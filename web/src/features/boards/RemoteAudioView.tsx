// Rozhlas vzdialeneho INISSu: fronty hlaseni s kontrolkou a priebehom, prisposobene velkosti miesta.
import { useEffect, useImperativeHandle, useLayoutEffect, useRef, useState, type Ref } from "react";
import type { BoardDto as Board } from "@/api";
import { progress } from "@/features/aksignal/aksignal";
import { queueLabel } from "@/features/aksignal/queue-label";
import { announcementText, queueState } from "@/features/aksignal/remote-audio";
import { useNow } from "@/hooks/use-now";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { drawRemoteAudio, REMOTE_AUDIO_WIDTH, remoteAudioHeight, type RemoteAudioPanel } from "@/render/remoteaudio";
import type { BoardViewHandle } from "./BoardView";

function panelOf(board: Board, now: number): RemoteAudioPanel {
  const queues = board.display.remoteAudio?.queues ?? [];
  const playing = queues.filter((q) => q.playing).length;
  return {
    title: playing > 0 ? t("remoteaudio.title.playing", { count: playing }) : queues.length > 0 ? t("remoteaudio.title.idle") : t("remoteaudio.title.none"),
    rows: queues.map((q) => {
      const state = queueState(q);
      return {
        state,
        name: queueLabel(q.name),
        text: q.playing ? announcementText(q.playing) : t(`remoteaudio.queue.${state}`),
        progress: q.playing ? progress(q.playing, now) : null,
      };
    }),
  };
}

interface Props {
  board: Board;
  fit?: "width" | "contain";
  className?: string;
  ref?: Ref<BoardViewHandle>;
}

export function RemoteAudioView({ board, fit = "width", className, ref }: Props) {
  const box = useRef<HTMLDivElement>(null);
  const canvas = useRef<HTMLCanvasElement>(null);
  const [space, setSpace] = useState({ width: 0, height: 0 });
  // priebeh hlasenia sa posuva aj bez zmeny tabule
  const playing = board.display.remoteAudio?.queues.some((q) => q.playing) ?? false;
  const now = useNow(playing ? 250 : 60_000);
  const panel = panelOf(board, now);
  const height = remoteAudioHeight(panel.rows.length);
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

  // na sirku miesta, no najviac dvojnasobok - rozhlas nema byt cez celu siroku kartu
  const cssScale =
    space.width > 0
      ? fit === "contain" && space.height > 0
        ? Math.min(space.width / REMOTE_AUDIO_WIDTH, space.height / height)
        : Math.min(2, space.width / REMOTE_AUDIO_WIDTH)
      : 0;
  const scale = cssScale * (window.devicePixelRatio || 1);

  useImperativeHandle(
    ref,
    () => ({
      snapshot: () => {
        const shot = panelOf(latest.current, Date.now());
        const target = document.createElement("canvas");
        target.width = REMOTE_AUDIO_WIDTH * 2;
        target.height = remoteAudioHeight(shot.rows.length) * 2;
        drawRemoteAudio(target.getContext("2d")!, shot, 2);
        return new Promise<Blob>((resolve, reject) => target.toBlob((blob) => (blob ? resolve(blob) : reject(new Error("PNG"))), "image/png"));
      },
    }),
    [],
  );

  useEffect(() => {
    const el = canvas.current;
    if (!el || cssScale <= 0) return;
    el.width = Math.max(1, Math.round(REMOTE_AUDIO_WIDTH * scale));
    el.height = Math.max(1, Math.round(height * scale));
    const ctx = el.getContext("2d");
    if (ctx) drawRemoteAudio(ctx, panel, scale);
  }, [panel, height, scale, cssScale]);

  return (
    <div ref={box} className={cn("flex w-full justify-center leading-0", fit === "contain" && "h-full items-center", className)}>
      <canvas
        ref={canvas}
        className="block max-w-full rounded-lg"
        style={cssScale > 0 ? { width: `${REMOTE_AUDIO_WIDTH * cssScale}px`, height: `${height * cssScale}px` } : { visibility: "hidden" }}
      />
    </div>
  );
}
