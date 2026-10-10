// Scenare porucha v case: zoznam s casovou osou krokov, spustenie a zastavenie, uprava a odstranenie.
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pencil, Play, Plus, Square, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  deleteFaultScenario,
  errorText,
  getFaultScenariosOptions,
  PERMISSIONS,
  startFaultScenario,
  stopFaultScenario,
  useCan,
  useSimulatorState,
  type BoardDto,
  type FaultScenarioDto,
  type FaultScenarioRequest,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { ScenarioDialog } from "./ScenarioDialog";
import { ScenarioTimeline } from "./ScenarioTimeline";
import { scenarioLength } from "./scenarios";
import { formatSeconds } from "./step-text";

const failed = (e: unknown) => toast.error(errorText(e));

const EMPTY: FaultScenarioRequest = { name: "", loop: false, cycleMs: null, steps: [] };

const toRequest = (s: FaultScenarioDto): FaultScenarioRequest => ({ name: s.name, loop: s.loop, cycleMs: s.cycleMs, steps: s.steps });

interface CardProps {
  scenario: FaultScenarioDto;
  boards: BoardDto[];
  edit: boolean;
  onEdit: () => void;
}

function ScenarioCard({ scenario, boards, edit, onEdit }: CardProps) {
  const run = scenario.run;
  const length = run?.lengthMs ?? scenarioLength(scenario);
  const remove = async () => {
    if (await confirmAction({ title: t("scenario.delete.confirm", { name: scenario.name }), destructive: true }))
      deleteFaultScenario({ path: { id: scenario.id }, throwOnError: true }).catch(failed);
  };
  const toggle = () =>
    (run ? stopFaultScenario({ path: { id: scenario.id }, throwOnError: true }) : startFaultScenario({ path: { id: scenario.id }, throwOnError: true })).catch(
      failed,
    );

  return (
    <Card className={cn("gap-3", run && "ring-2 ring-amber-500/50")}>
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div className="min-w-0">
          <CardTitle className="truncate">{scenario.name}</CardTitle>
          <div className="text-sm text-muted-foreground">
            {t("scenario.meta", { steps: scenario.steps.length, length: formatSeconds(length) })} ·{" "}
            {t(scenario.loop ? "scenario.mode.loop" : "scenario.mode.once")}
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-1">
          {run && <StatusBadge tone="wait">{t("scenario.running", { cycle: run.cycle + 1 })}</StatusBadge>}
          {edit && (
            <>
              <Button size="sm" variant={run ? "outline" : "default"} onClick={toggle}>
                {run ? <Square /> : <Play />}
                {t(run ? "scenario.stop" : "scenario.start")}
              </Button>
              <Button variant="ghost" size="icon-sm" title={t("common.edit")} onClick={onEdit}>
                <Pencil />
              </Button>
              <Button variant="ghost" size="icon-sm" title={t("common.delete")} onClick={() => void remove()}>
                <Trash2 />
              </Button>
            </>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <ScenarioTimeline steps={scenario.steps} lengthMs={length} run={run} boards={boards} />
      </CardContent>
    </Card>
  );
}

export function ScenariosPage() {
  const edit = useCan()(PERMISSIONS.scenarios);
  const live = useSimulatorState();
  const query = useQuery(getFaultScenariosOptions());
  const scenarios = query.data ?? [];
  const [editing, setEditing] = useState<{ id: number | null; scenario: FaultScenarioRequest } | null>(null);

  return (
    <>
      <PageHeader description={t("scenarios.hint")}>
        {edit && (
          <Button onClick={() => setEditing({ id: null, scenario: EMPTY })}>
            <Plus />
            {t("scenario.add")}
          </Button>
        )}
      </PageHeader>
      {query.isSuccess && scenarios.length === 0 && <p className="text-sm text-muted-foreground">{t("scenarios.empty")}</p>}
      <div className="grid gap-4 xl:grid-cols-2">
        {scenarios.map((s) => (
          <ScenarioCard key={s.id} scenario={s} boards={live.boards} edit={edit} onEdit={() => setEditing({ id: s.id, scenario: toRequest(s) })} />
        ))}
      </div>
      {editing && <ScenarioDialog id={editing.id} initial={editing.scenario} onClose={() => setEditing(null)} />}
    </>
  );
}
