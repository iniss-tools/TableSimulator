// Prehravanie zaznamu s casovou osou: hustota sprav v zazname, posun kliknutim alebo tahom (aj klavesnicou), pauza,
// rychlost a opakovanie pocas prehravania. Poloha sa medzi spravami servera dopocitava.
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pause, Play, Square } from "lucide-react";
import { toast } from "sonner";
import {
  errorText,
  getRecordingActivityOptions,
  getRecordingsQueryKey,
  queryClient,
  stopReplay,
  updateReplay,
  type RecordingsDto,
  type ReplayStatusDto,
  type UpdateReplayRequest,
} from "@/api";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { useNow } from "@/hooks/use-now";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { duration, isOlderReplay, livePosition, SPEEDS } from "./replay";

/** Pocet usekov hustoty sprav na casovej osi. */
const BUCKETS = 200;

const failed = (e: unknown) => toast.error(errorText(e));

/** Zmena prehravania; odpoved hned zapise do zoznamu zaznamov (WebSocket ju prinesie tiez, novsi stav ostane). */
function update(body: UpdateReplayRequest, done?: () => void) {
  updateReplay({ body, throwOnError: true })
    .then(({ data }) =>
      queryClient.setQueryData<RecordingsDto>(getRecordingsQueryKey(), (r) => r && (isOlderReplay(data, r.replay) ? r : { ...r, replay: data })),
    )
    .catch(failed)
    .finally(done);
}

/** Hustota sprav INISSu v zazname ako stlpce za casovou osou. */
function Activity({ name }: { name: string }) {
  const activity = useQuery({ ...getRecordingActivityOptions({ path: { name }, query: { buckets: BUCKETS } }), staleTime: Infinity }).data;
  if (!activity) return null;
  const max = Math.max(1, ...activity.counts);
  return (
    <div className="pointer-events-none absolute inset-0 flex items-end gap-px">
      {activity.counts.map((count, i) => (
        <div key={i} className="flex-1 rounded-t-[1px] bg-muted-foreground/35" style={{ height: count > 0 ? `${Math.max(8, (count / max) * 100)}%` : 0 }} />
      ))}
    </div>
  );
}

export function ReplayPlayer({ replay, manage }: { replay: ReplayStatusDto; manage: boolean }) {
  const running = replay.state === "running";
  const now = useNow(running ? 200 : 60_000);
  const position = livePosition(replay, now);
  // poloha pocas tahania - posun sa posle az po pusteni
  const [drag, setDrag] = useState<number | null>(null);
  // zvolene miesto, kym server posun nepotvrdi - inak by ukazovatel na chvilu skocil spat
  const [pending, setPending] = useState<number | null>(null);
  const shown = drag ?? pending ?? position;
  const percent = replay.duration > 0 ? Math.min(100, (shown / replay.duration) * 100) : 100;
  const commit = () => {
    if (drag === null) return;
    setPending(drag);
    setDrag(null);
    update({ position: drag, paused: null, speed: null, loop: null }, () => setPending(null));
  };
  const toggle = () => update({ paused: running, position: null, speed: null, loop: null });

  return (
    <Card>
      <CardContent className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <span className="font-medium">{t(`replay.${replay.state}`, { name: replay.name })}</span>
          <span className="flex-1 text-sm text-muted-foreground tabular-nums">
            {duration(shown)} / {duration(replay.duration)}
            {replay.loop && <> · {t("replay.pass", { pass: replay.pass })}</>}
          </span>
          {manage && (
            <>
              <Label className="font-normal">
                {t("rec.speed")}
                <OptionSelect
                  value={replay.speed}
                  onValueChange={(speed) => update({ speed, paused: null, position: null, loop: null })}
                  options={SPEEDS.map((s) => ({ value: s, label: s > 0 ? `${s}×` : t("rec.speed.max") }))}
                  ariaLabel={t("rec.speed")}
                  size="sm"
                />
              </Label>
              <Label className="font-normal">
                <Checkbox checked={replay.loop} onCheckedChange={(loop) => update({ loop: loop === true, paused: null, position: null, speed: null })} />
                {t("rec.loop")}
              </Label>
              <Button variant="outline" size="sm" onClick={toggle}>
                {running ? <Pause /> : <Play />}
                {t(running ? "replay.pause" : replay.state === "paused" ? "replay.resume" : "replay.play")}
              </Button>
              {(running || replay.state === "paused") && (
                <Button variant="outline" size="sm" onClick={() => stopReplay({ throwOnError: true }).catch(failed)}>
                  <Square />
                  {t("replay.stop")}
                </Button>
              )}
            </>
          )}
        </div>
        <div className="relative h-12 overflow-hidden rounded-md border bg-muted/30">
          <Activity name={replay.name} />
          <div className="pointer-events-none absolute inset-y-0 left-0 bg-primary/15" style={{ width: `${percent}%` }} />
          <div className={cn("pointer-events-none absolute inset-y-0 w-0.5 bg-primary", drag !== null && "bg-amber-500")} style={{ left: `${percent}%` }} />
          <input
            type="range"
            min={0}
            max={Math.max(replay.duration, 0.1)}
            step={0.1}
            value={shown}
            disabled={!manage}
            aria-label={t("replay.timeline")}
            aria-valuetext={`${duration(shown)} / ${duration(replay.duration)}`}
            className="absolute inset-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-default"
            onChange={(e) => setDrag(Number(e.target.value))}
            onPointerUp={commit}
            onKeyUp={commit}
            onBlur={commit}
          />
        </div>
      </CardContent>
    </Card>
  );
}
