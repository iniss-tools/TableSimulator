// Casova os scenara: kazdy krok ako pruh v cykle, platne kroky zvyraznene, pri behu ukazovatel aktualneho casu.
import type { BoardDto, ScenarioRunDto, ScenarioStepDto } from "@/api";
import { useNow } from "@/hooks/use-now";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { runPosition } from "./scenarios";
import { formatSeconds, stepTarget, stepWhat } from "./step-text";

/** Farba pruhu podla druhu kroku: porucha tabule, vypadok napajania, vypadok linky. */
const COLORS = {
  fault: { idle: "bg-amber-500/45", active: "bg-amber-500" },
  powerLoss: { idle: "bg-red-500/45", active: "bg-red-500" },
  lineOutage: { idle: "bg-violet-500/45", active: "bg-violet-500" },
} as const;

const percent = (value: number, length: number) => `${Math.min(100, Math.max(0, (value / length) * 100))}%`;

function Cursor({ run, length }: { run: ScenarioRunDto; length: number }) {
  const now = useNow(200);
  return <div className="pointer-events-none absolute inset-y-0 w-0.5 bg-foreground" style={{ left: percent(runPosition(run, now), length) }} />;
}

interface Props {
  steps: ScenarioStepDto[];
  lengthMs: number;
  run: ScenarioRunDto | null;
  boards: BoardDto[];
}

export function ScenarioTimeline({ steps, lengthMs, run, boards }: Props) {
  if (steps.length === 0) return <p className="text-sm text-muted-foreground">{t("scenario.noSteps")}</p>;
  return (
    <div className="flex flex-col gap-1 text-xs">
      {steps.map((step, i) => {
        const active = run?.activeSteps.includes(i) ?? false;
        const color = COLORS[step.action][active ? "active" : "idle"];
        const label = `${stepTarget(step, boards)} – ${stepWhat(step)}`;
        return (
          <div key={i} className="grid grid-cols-[minmax(0,15rem)_minmax(0,1fr)] items-center gap-2">
            <span className={cn("truncate", active ? "font-medium" : "text-muted-foreground")} title={label}>
              {label}
            </span>
            <div className="relative h-4 overflow-hidden rounded-sm bg-muted">
              <div
                className={cn("absolute inset-y-0 rounded-sm", color, active && "ring-2 ring-foreground/40 ring-inset")}
                style={{ left: percent(step.atMs, lengthMs), width: percent(step.durationMs, lengthMs) }}
                title={t("scenario.stepTime", { at: formatSeconds(step.atMs), duration: formatSeconds(step.durationMs) })}
              />
              {run && <Cursor run={run} length={lengthMs} />}
            </div>
          </div>
        );
      })}
      <div className="grid grid-cols-[minmax(0,15rem)_minmax(0,1fr)] gap-2 text-muted-foreground">
        <span />
        <div className="flex justify-between">
          <span>0 s</span>
          <span>{formatSeconds(lengthMs)}</span>
        </div>
      </div>
    </div>
  );
}
