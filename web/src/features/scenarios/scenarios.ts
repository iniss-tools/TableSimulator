// Scenare porucha: dlzka cyklu (ako FaultScenario.LengthMs v jadre), "co sa stane" v kroku, novy krok a poloha behu.
import type { AkSignalConditionsDto, BoardFaultDto, FaultKind, FaultScenarioRequest, ScenarioDeviceDto, ScenarioRunDto, ScenarioStepDto } from "@/api";

/** Najkratsi cyklus [ms] - zodpoveda FaultScenario.MinCycleMs. */
export const MIN_CYCLE_MS = 1000;

/** Predvolene trvanie noveho kroku [ms]. */
const DEFAULT_DURATION_MS = 10_000;

/** Co sa v kroku stane: porucha tabule, vypadok napajania ci linky alebo stav ustredne AK Signal - jedna volba v dialogu. */
export type StepKind = Exclude<FaultKind, "none"> | "powerLoss" | "lineOutage" | "device";

export const STEP_KINDS: StepKind[] = ["noReply", "nak", "delay", "garbage", "status", "powerLoss", "lineOutage", "device"];

/** Stavy ustredne, ktore krok scenara moze pridat - v poradi ako na stranke svetiel. */
export const CONDITION_KEYS = [
  "lightsFault",
  "highPriority",
  "outputPowerError",
  "ampSupplyMissing",
  "ampCommError",
  "bankNotSynced",
] as const satisfies readonly (keyof AkSignalConditionsDto)[];

export const NO_CONDITIONS: AkSignalConditionsDto = {
  lightsFault: false,
  highPriority: false,
  outputPowerError: false,
  ampSupplyMissing: false,
  ampCommError: false,
  bankNotSynced: false,
};

/** Predvoleny krok ustredne: porucha svetiel, snimac podla nastavenia. */
const DEFAULT_DEVICE: ScenarioDeviceDto = { conditions: { ...NO_CONDITIONS, lightsFault: true }, sensor: null };

/** Krok ustredne nieco nastavuje - aspon jeden stav alebo rezim snimaca (inak ho server odmietne). */
export const deviceSetsSomething = (device: ScenarioDeviceDto) => device.sensor !== null || CONDITION_KEYS.some((k) => device.conditions[k]);

/** Dlzka jedneho cyklu [ms]: zadana, inak koniec posledneho kroku, najmenej 1 s. */
export function scenarioLength(scenario: Pick<FaultScenarioRequest, "cycleMs" | "steps">): number {
  const end = scenario.steps.reduce((max, s) => Math.max(max, s.atMs + s.durationMs), 0);
  return Math.max(MIN_CYCLE_MS, scenario.cycleMs ?? end);
}

export function stepKind(step: ScenarioStepDto): StepKind {
  if (step.action !== "fault") return step.action;
  const kind = step.fault?.kind ?? "noReply";
  return kind === "none" ? "noReply" : kind;
}

/** Krok s inou volbou "co sa stane" - porucha si necha parametre, ak ich ma. */
export function withKind(step: ScenarioStepDto, kind: StepKind): ScenarioStepDto {
  if (kind === "powerLoss" || kind === "lineOutage") return { ...step, action: kind, fault: null, device: null };
  if (kind === "device") return { ...step, action: "device", fault: null, device: step.device ?? DEFAULT_DEVICE };
  const fault: BoardFaultDto = {
    kind,
    delayMs: kind === "delay" ? step.fault?.delayMs || 500 : 0,
    status: kind === "status" ? step.fault?.status || 0x01 : 0,
  };
  return { ...step, action: "fault", fault, device: null };
}

/** Novy krok: za koncom predchadzajuceho, na tej istej linke (inak prvej), neodpoveda 10 s. */
export function newStep(previous: ScenarioStepDto | undefined, line: number): ScenarioStepDto {
  return {
    atMs: previous ? previous.atMs + previous.durationMs : 0,
    durationMs: DEFAULT_DURATION_MS,
    line: previous?.line ?? line,
    address: previous?.address ?? null,
    action: "fault",
    fault: { kind: "noReply", delayMs: 0, status: 0 },
    device: null,
  };
}

/** Poloha beziaceho scenara v cykle [ms] v case now. */
export function runPosition(run: ScenarioRunDto, now: number): number {
  const elapsed = Math.max(0, now - Date.parse(run.startedAt));
  return run.lengthMs > 0 ? elapsed % run.lengthMs : 0;
}

/** Sekundy z milisekund na upravu (najviac jedno desatinne miesto). */
export const toSeconds = (ms: number) => Math.round(ms / 100) / 10;

/** Milisekundy zo sekund zadanych v poli; prazdne alebo neplatne = null. */
export function fromSeconds(text: string): number | null {
  const value = Number.parseFloat(text.replace(",", "."));
  return Number.isFinite(value) ? Math.round(value * 1000) : null;
}
