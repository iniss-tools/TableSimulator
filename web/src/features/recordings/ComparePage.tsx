// Porovnanie dvoch zaznamov: co INISS ukazoval na tabuliach pred zmenou grafikonu a po nej - postupnosti textu kazdej
// tabule porovnane ako diff (na case nezalezi, len na poradi); hociktory stav sa da ukazat ako tabula.
import { useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeftRight, ChevronDown, ChevronRight, Eye } from "lucide-react";
import {
  compareRecordingsOptions,
  errorText,
  getRecordingBoardOptions,
  getRecordingsOptions,
  type BoardComparisonDto,
  type BoardDifference,
  type BoardStateDto,
  type ComparisonStepDto,
  type RecordingSideDto,
} from "@/api";
import { CheckField } from "@/components/common/form-field";
import { FormDialog } from "@/components/common/form-dialog";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge, type Tone } from "@/components/common/status-badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Spinner } from "@/components/ui/spinner";
import { BoardView } from "@/features/boards/BoardView";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { changedRows, clockTime, elapsed, groupSteps } from "./compare";

const TONES: Record<BoardDifference, Tone> = { same: "ok", different: "bad", onlyA: "wait", onlyB: "wait" };

/** Stav tabule na zobrazenie: zaznam, tabula a cas. */
interface Shown {
  board: string;
  a: { side: RecordingSideDto; state: BoardStateDto } | null;
  b: { side: RecordingSideDto; state: BoardStateDto } | null;
}

/** Tabula zo zaznamu v case - ako ju INISS nechal. */
function Snapshot({ side, state, board }: { side: RecordingSideDto; state: BoardStateDto; board: string }) {
  // tabula po poslednom riadku stavu (INISS posiela riadky po jednom)
  const query = useQuery(getRecordingBoardOptions({ path: { name: side.name, board }, query: { time: state.settled } }));
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <span className="text-sm font-medium">
        {side.name} · {elapsed(state.time)} ({clockTime(side.created, state.time)})
      </span>
      {query.data ? <BoardView board={query.data} /> : query.error ? <p className="text-sm text-destructive">{errorText(query.error)}</p> : <Spinner />}
    </div>
  );
}

function SnapshotDialog({ shown, onClose }: { shown: Shown; onClose: () => void }) {
  return (
    <FormDialog title={t("compare.snapshot", { board: shown.board })} onClose={onClose} onSubmit={onClose} submitLabel={t("common.close")} noCancel size="xl">
      <div className={cn("grid gap-4", shown.a && shown.b && "lg:grid-cols-2")}>
        {shown.a && <Snapshot side={shown.a.side} state={shown.a.state} board={shown.board} />}
        {shown.b && <Snapshot side={shown.b.side} state={shown.b.state} board={shown.board} />}
      </div>
    </FormDialog>
  );
}

/** Text stavu: riadky pod sebou, odlisne riadky zvyraznene; cas a tlacidlo na zobrazenie tabule. */
function StateCell({
  state,
  side,
  changed,
  tone,
  onShow,
}: {
  state: BoardStateDto | null;
  side: RecordingSideDto;
  changed?: Set<number>;
  tone?: "removed" | "added";
  onShow: () => void;
}) {
  if (!state) return <div className="rounded-md border border-dashed p-2 text-xs text-muted-foreground">{t("compare.none")}</div>;
  return (
    <div className="flex min-w-0 flex-col gap-1">
      <div className="flex items-center gap-2 text-xs text-muted-foreground">
        <span title={clockTime(side.created, state.time)}>{elapsed(state.time)}</span>
        <Button variant="ghost" size="icon-xs" title={t("compare.show")} onClick={onShow}>
          <Eye />
        </Button>
      </div>
      <div
        className={cn(
          "overflow-x-auto rounded-md border bg-muted/40 p-2 font-mono text-xs leading-5",
          tone === "removed" && "border-red-500/40 bg-red-500/5",
          tone === "added" && "border-emerald-500/40 bg-emerald-500/5",
        )}
      >
        {state.rows.map((row, i) => (
          <div
            key={i}
            className={cn(
              "min-h-5 whitespace-pre",
              changed?.has(i) && (tone === "added" ? "bg-emerald-500/20" : "bg-red-500/20"),
              row.length === 0 && "text-muted-foreground",
            )}
          >
            {row || "·"}
          </div>
        ))}
      </div>
    </div>
  );
}

function BoardSection({ board, a, b, onShow }: { board: BoardComparisonDto; a: RecordingSideDto; b: RecordingSideDto; onShow: (shown: Shown) => void }) {
  const [open, setOpen] = useState(board.result !== "same");
  const [expanded, setExpanded] = useState<Set<number>>(new Set());
  const groups = useMemo(() => groupSteps(board.steps), [board.steps]);
  const show = (step: ComparisonStepDto) =>
    onShow({ board: board.id, a: step.a ? { side: a, state: step.a } : null, b: step.b ? { side: b, state: step.b } : null });

  return (
    <Card className="gap-3">
      <CardHeader>
        <button type="button" className="flex w-full items-center gap-2 text-left" onClick={() => setOpen(!open)} aria-expanded={open}>
          {open ? <ChevronDown className="size-4 shrink-0" /> : <ChevronRight className="size-4 shrink-0" />}
          <CardTitle className="min-w-0 truncate">{board.name || t("compare.board", { id: board.id })}</CardTitle>
          <span className="text-sm text-muted-foreground">{board.id}</span>
          <StatusBadge tone={TONES[board.result]}>{t(`compare.result.${board.result}`)}</StatusBadge>
          <span className="ml-auto text-xs text-muted-foreground">{t("compare.states", { a: board.statesA, b: board.statesB })}</span>
        </button>
      </CardHeader>
      {open && (
        <CardContent className="flex flex-col gap-3">
          {groups.map((group, g) => {
            if (group.kind === "same") {
              const first = group.steps[0];
              const last = group.steps[group.steps.length - 1];
              const isOpen = expanded.has(g);
              return (
                <div key={g} className="flex flex-col gap-2">
                  <button
                    type="button"
                    className="flex items-center gap-2 text-left text-xs text-muted-foreground hover:text-foreground"
                    onClick={() => setExpanded((s) => new Set(s.has(g) ? [...s].filter((x) => x !== g) : [...s, g]))}
                  >
                    {isOpen ? <ChevronDown className="size-3.5" /> : <ChevronRight className="size-3.5" />}
                    {t("compare.sameRun", {
                      count: group.steps.length,
                      a: `${elapsed(first.a!.time)}–${elapsed(last.a!.time)}`,
                      b: `${elapsed(first.b!.time)}–${elapsed(last.b!.time)}`,
                    })}
                  </button>
                  {isOpen &&
                    group.steps.map((step, i) => (
                      <div key={i} className="grid gap-3 md:grid-cols-2">
                        <StateCell state={step.a} side={a} onShow={() => show(step)} />
                        <StateCell state={step.b} side={b} onShow={() => show(step)} />
                      </div>
                    ))}
                </div>
              );
            }
            const step = group.step;
            const changed = changedRows(step.a?.rows, step.b?.rows);
            return (
              <div key={g} className="grid gap-3 md:grid-cols-2">
                <StateCell state={step.a} side={a} changed={changed} tone="removed" onShow={() => show(step)} />
                <StateCell state={step.b} side={b} changed={changed} tone="added" onShow={() => show(step)} />
              </div>
            );
          })}
        </CardContent>
      )}
    </Card>
  );
}

export function ComparePage() {
  const search = useSearch({ strict: false });
  const navigate = useNavigate();
  const a = typeof search.a === "string" ? search.a : "";
  const b = typeof search.b === "string" ? search.b : "";
  const recordings = useQuery(getRecordingsOptions()).data?.items ?? [];
  const names = [...recordings].sort((x, y) => y.created.localeCompare(x.created)).map((r) => r.name);
  const ready = a !== "" && b !== "" && a !== b;
  const comparison = useQuery({ ...compareRecordingsOptions({ query: { a, b } }), enabled: ready });
  const [onlyDifferent, setOnlyDifferent] = useState(true);
  const [shown, setShown] = useState<Shown | null>(null);

  const choose = (next: { a?: string; b?: string }) => void navigate({ to: "/recordings/compare", search: { a: next.a ?? a, b: next.b ?? b } });
  const data = comparison.data;
  const boards = data?.boards.filter((x) => !onlyDifferent || x.result !== "same") ?? [];
  const counts = (result: BoardDifference) => data?.boards.filter((x) => x.result === result).length ?? 0;
  const options = [{ value: "", label: t("compare.choose") }, ...names.map((n) => ({ value: n, label: n }))];

  return (
    <>
      <PageHeader description={t("compare.hint")} />
      <div className="mb-4 flex flex-wrap items-end gap-2">
        <div className="flex flex-col gap-1">
          <span className="text-sm font-medium">{t("compare.a")}</span>
          <OptionSelect<string> value={a} onValueChange={(value) => choose({ a: value })} options={options} ariaLabel={t("compare.a")} className="min-w-64" />
        </div>
        <Button variant="outline" size="icon" title={t("compare.swap")} onClick={() => choose({ a: b, b: a })} disabled={!a && !b}>
          <ArrowLeftRight />
        </Button>
        <div className="flex flex-col gap-1">
          <span className="text-sm font-medium">{t("compare.b")}</span>
          <OptionSelect<string> value={b} onValueChange={(value) => choose({ b: value })} options={options} ariaLabel={t("compare.b")} className="min-w-64" />
        </div>
      </div>

      {a !== "" && a === b && (
        <Alert className="mb-4">
          <AlertDescription>{t("compare.sameRecording")}</AlertDescription>
        </Alert>
      )}
      {comparison.isFetching && !data && (
        <p className="flex items-center gap-2 text-sm text-muted-foreground">
          <Spinner />
          {t("compare.running")}
        </p>
      )}
      {comparison.error && <p className="text-sm text-destructive">{errorText(comparison.error)}</p>}

      {data && ready && (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <StatusBadge tone="bad">{t("compare.count.different", { count: counts("different") })}</StatusBadge>
            <StatusBadge tone="wait">{t("compare.count.onlyA", { count: counts("onlyA") })}</StatusBadge>
            <StatusBadge tone="wait">{t("compare.count.onlyB", { count: counts("onlyB") })}</StatusBadge>
            <StatusBadge tone="ok">{t("compare.count.same", { count: counts("same") })}</StatusBadge>
            <CheckField className="ml-auto w-auto" label={t("compare.onlyDifferent")} checked={onlyDifferent} onCheckedChange={setOnlyDifferent} />
          </div>
          {boards.length === 0 && <p className="text-sm text-muted-foreground">{t(data.boards.length === 0 ? "compare.noBoards" : "compare.allSame")}</p>}
          <div className="flex flex-col gap-4">
            {boards.map((board) => (
              <BoardSection key={board.id} board={board} a={data.a} b={data.b} onShow={setShown} />
            ))}
          </div>
        </>
      )}
      {shown && <SnapshotDialog shown={shown} onClose={() => setShown(null)} />}
    </>
  );
}
