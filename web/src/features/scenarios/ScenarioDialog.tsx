// Novy scenar porucha alebo uprava: nazov, opakovanie a dlzka cyklu, kroky (od kedy, ako dlho, na com, co sa stane) a nahlad.
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import {
  createFaultScenario,
  errorText,
  getFaultScenariosQueryKey,
  queryClient,
  setFaultScenario,
  useSimulatorState,
  type FaultScenarioRequest,
  type ScenarioStepDto,
} from "@/api";
import { CheckField, FormField } from "@/components/common/form-field";
import { FormDialog } from "@/components/common/form-dialog";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { FieldGroup, FieldLegend, FieldSet } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";
import { ScenarioTimeline } from "./ScenarioTimeline";
import { fromSeconds, newStep, scenarioLength, STEP_KINDS, stepKind, toSeconds, withKind, type StepKind } from "./scenarios";
import { formatSeconds, kindLabel } from "./step-text";

/** Vsetky tabule linky v poli "na com". */
const ALL = "all";

interface Props {
  id: number | null;
  initial: FaultScenarioRequest;
  onClose: () => void;
}

/** Pole so sekundami: text sa drzi pocas pisania, do kroku ide az platne cislo. */
function SecondsInput({ value, onChange, label, min = 0 }: { value: number; onChange: (ms: number) => void; label: string; min?: number }) {
  const [text, setText] = useState(String(toSeconds(value)));
  return (
    <Input
      type="number"
      min={min}
      step={0.1}
      aria-label={label}
      title={label}
      value={text}
      onChange={(e) => {
        setText(e.target.value);
        const ms = fromSeconds(e.target.value);
        if (ms !== null && ms >= 0) onChange(ms);
      }}
    />
  );
}

export function ScenarioDialog({ id, initial, onClose }: Props) {
  const live = useSimulatorState();
  const [scenario, setScenario] = useState(initial);
  // stale kluce riadkov krokov - polia si drzia rozpisany text, po odobrati kroku sa nesmu posunut
  const [keys, setKeys] = useState(() => initial.steps.map((_, i) => i));
  const [nextKey, setNextKey] = useState(initial.steps.length);
  const addStep = () => {
    set({ steps: [...scenario.steps, newStep(scenario.steps.at(-1), live.lines[0]?.number ?? 1)] });
    setKeys([...keys, nextKey]);
    setNextKey(nextKey + 1);
  };
  const removeStep = (index: number) => {
    set({ steps: scenario.steps.filter((_, j) => j !== index) });
    setKeys(keys.filter((_, j) => j !== index));
  };
  const set = (patch: Partial<FaultScenarioRequest>) => setScenario((s) => ({ ...s, ...patch }));
  const setStep = (index: number, patch: Partial<ScenarioStepDto>) =>
    setScenario((s) => ({ ...s, steps: s.steps.map((step, i) => (i === index ? { ...step, ...patch } : step)) }));
  const save = useMutation({
    mutationFn: async (body: FaultScenarioRequest) => {
      if (id === null) await createFaultScenario({ body, throwOnError: true });
      else await setFaultScenario({ path: { id }, body, throwOnError: true });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getFaultScenariosQueryKey() });
      onClose();
    },
  });

  const lineOptions = live.lines.map((l) => ({ value: l.number, label: l.name ? `${l.number} – ${l.name}` : String(l.number) }));
  const targetOptions = (step: ScenarioStepDto) => [
    { value: ALL, label: t("scenario.target.all") },
    ...live.boards
      .filter((b) => b.line === step.line)
      .map((b) => ({ value: String(b.address), label: b.name ? `${b.address} – ${b.name}` : String(b.address) })),
  ];
  const length = scenarioLength(scenario);

  return (
    <FormDialog
      title={id === null ? t("scenario.new") : t("scenario.edit", { name: initial.name })}
      onClose={onClose}
      onSubmit={() => save.mutate(scenario)}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="xl"
    >
      <FieldGroup>
        <div className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto_12rem] sm:items-end">
          <FormField label={t("scenario.field.name")}>
            <Input autoFocus value={scenario.name} onChange={(e) => set({ name: e.target.value })} />
          </FormField>
          <CheckField label={t("scenario.field.loop")} checked={scenario.loop} onCheckedChange={(loop) => set({ loop })} className="pb-2" />
          <FormField label={t("scenario.field.cycle")}>
            <Input
              type="number"
              min={1}
              step={0.1}
              placeholder={t("scenario.field.cycle.auto", { length: formatSeconds(length) })}
              value={scenario.cycleMs === null ? "" : toSeconds(scenario.cycleMs)}
              onChange={(e) => set({ cycleMs: e.target.value === "" ? null : fromSeconds(e.target.value) })}
            />
          </FormField>
        </div>
        <FieldSet>
          <FieldLegend variant="label">{t("scenario.field.steps")}</FieldLegend>
          {scenario.steps.length > 0 && (
            <div className="hidden grid-cols-[6rem_6rem_10rem_minmax(0,1fr)_minmax(0,1fr)_7rem_2rem] gap-2 text-xs text-muted-foreground sm:grid">
              <span>{t("scenario.col.at")}</span>
              <span>{t("scenario.col.duration")}</span>
              <span>{t("scenario.col.line")}</span>
              <span>{t("scenario.col.target")}</span>
              <span>{t("scenario.col.kind")}</span>
              <span>{t("scenario.col.param")}</span>
              <span />
            </div>
          )}
          {scenario.steps.map((step, i) => {
            const kind = stepKind(step);
            return (
              <div
                key={keys[i]}
                className="grid grid-cols-2 gap-2 border-b pb-2 sm:grid-cols-[6rem_6rem_10rem_minmax(0,1fr)_minmax(0,1fr)_7rem_2rem] sm:border-0 sm:pb-0"
              >
                <SecondsInput value={step.atMs} label={t("scenario.col.at")} onChange={(atMs) => setStep(i, { atMs })} />
                <SecondsInput value={step.durationMs} label={t("scenario.col.duration")} min={0.2} onChange={(durationMs) => setStep(i, { durationMs })} />
                <OptionSelect<number>
                  value={step.line}
                  ariaLabel={t("scenario.col.line")}
                  options={lineOptions.some((o) => o.value === step.line) ? lineOptions : [{ value: step.line, label: String(step.line) }, ...lineOptions]}
                  onValueChange={(line) => setStep(i, { line, address: null })}
                  className="w-full"
                />
                {kind === "lineOutage" ? (
                  <span className="self-center text-sm text-muted-foreground">{t("scenario.target.wholeLine")}</span>
                ) : (
                  <OptionSelect<string>
                    value={step.address === null ? ALL : String(step.address)}
                    ariaLabel={t("scenario.col.target")}
                    options={
                      step.address === null || targetOptions(step).some((o) => o.value === String(step.address))
                        ? targetOptions(step)
                        : [...targetOptions(step), { value: String(step.address), label: String(step.address) }]
                    }
                    onValueChange={(value) => setStep(i, { address: value === ALL ? null : Number(value) })}
                    className="w-full"
                  />
                )}
                <OptionSelect<StepKind>
                  value={kind}
                  ariaLabel={t("scenario.col.kind")}
                  options={STEP_KINDS.map((k) => ({ value: k, label: kindLabel(k) }))}
                  onValueChange={(k) => setScenario((s) => ({ ...s, steps: s.steps.map((x, j) => (j === i ? withKind(x, k) : x)) }))}
                  className="w-full"
                />
                {kind === "delay" ? (
                  <Input
                    type="number"
                    min={0}
                    step={100}
                    aria-label={t("fault.delayMs")}
                    title={t("fault.delayMs")}
                    value={step.fault?.delayMs ?? 0}
                    onChange={(e) => setStep(i, { fault: { kind: "delay", status: 0, delayMs: Number.parseInt(e.target.value, 10) || 0 } })}
                  />
                ) : kind === "status" ? (
                  <Input
                    className="font-mono"
                    aria-label={t("fault.statusBits")}
                    title={t("fault.statusBits")}
                    defaultValue={(step.fault?.status ?? 0).toString(16).toUpperCase().padStart(2, "0")}
                    onChange={(e) => {
                      const status = Number.parseInt(e.target.value, 16);
                      if (!Number.isNaN(status)) setStep(i, { fault: { kind: "status", delayMs: 0, status: status & 0xff } });
                    }}
                  />
                ) : (
                  <span />
                )}
                <Button type="button" variant="ghost" size="icon-sm" title={t("scenario.step.remove")} onClick={() => removeStep(i)}>
                  <Trash2 />
                </Button>
              </div>
            );
          })}
          <div>
            <Button type="button" variant="outline" size="sm" onClick={addStep}>
              <Plus />
              {t("scenario.step.add")}
            </Button>
          </div>
        </FieldSet>
        {scenario.steps.length > 0 && (
          <FieldSet>
            <FieldLegend variant="label">{t("scenario.field.preview")}</FieldLegend>
            <ScenarioTimeline steps={scenario.steps} lengthMs={length} run={null} boards={live.boards} />
          </FieldSet>
        )}
      </FieldGroup>
    </FormDialog>
  );
}
