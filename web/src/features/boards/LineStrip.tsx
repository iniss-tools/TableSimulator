// Prehlad liniek nad tabulami: stav spojenia s INISSom kazdej linky; klik vyberie tabule linky (znova = vsetky).
import type { LineDto } from "@/api";
import type { Tone } from "@/components/common/status-badge";
import { ALL_LINES, isOutage, lineLabel, lineStatusTime, STATUS_TONES } from "@/features/lines/line-status";
import { useNow } from "@/hooks/use-now";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";

const DOTS: Record<Tone, string> = { ok: "bg-emerald-500", wait: "bg-amber-500", bad: "bg-red-500", off: "bg-muted-foreground/50" };

const CHIP = "flex min-w-0 items-center gap-2 rounded-lg border px-3 py-1.5 text-left text-sm transition-colors hover:bg-accent";

interface LineStripProps {
  lines: LineDto[];
  filter: number;
  onFilter: (line: number) => void;
}

export function LineStrip({ lines, filter, onFilter }: LineStripProps) {
  const now = useNow(1000);
  return (
    <div role="group" aria-label={t("boards.filter.line")} className="flex flex-wrap gap-2">
      {lines.length > 1 && (
        <button
          type="button"
          aria-pressed={filter === ALL_LINES}
          className={cn(CHIP, filter === ALL_LINES && "border-primary bg-accent")}
          onClick={() => onFilter(ALL_LINES)}
        >
          {t("boards.filter.all")}
        </button>
      )}
      {lines.map((line) => {
        const selected = filter === line.number;
        const time = lineStatusTime(line, now);
        return (
          <button
            key={line.number}
            type="button"
            aria-pressed={selected}
            title={line.statusDetail ?? undefined}
            className={cn(CHIP, "max-w-80", isOutage(line.status) && "border-red-500/50 bg-red-500/5", selected && "border-primary bg-accent")}
            onClick={() => onFilter(selected ? ALL_LINES : line.number)}
          >
            <span className={cn("size-2.5 shrink-0 rounded-full", DOTS[STATUS_TONES[line.status]])} />
            <span className="grid min-w-0 leading-tight">
              <span className="truncate font-medium">{lineLabel(line)}</span>
              <span className="truncate text-xs text-muted-foreground">
                {t(`line.status.${line.status}`)}
                {time && ` · ${time}`}
              </span>
            </span>
          </button>
        );
      })}
    </div>
  );
}
