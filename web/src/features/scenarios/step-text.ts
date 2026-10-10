// Texty krokov scenara pre obsluhu: na com (linka, tabula, vsetky tabule) a co sa stane, cas v sekundach.
import type { BoardDto, ScenarioStepDto } from "@/api";
import { locale, t } from "@/i18n";
import { stepKind, toSeconds, type StepKind } from "./scenarios";

/** Nazov volby "co sa stane". */
export function kindLabel(kind: StepKind): string {
  return kind === "powerLoss" || kind === "lineOutage" ? t(`scenario.kind.${kind}`) : t(`fault.${kind}`);
}

/** Co sa v kroku stane, s parametrom poruchy. */
export function stepWhat(step: ScenarioStepDto): string {
  const kind = stepKind(step);
  if (kind === "delay") return t("scenario.what.delay", { ms: step.fault?.delayMs ?? 0 });
  if (kind === "status") return t("scenario.what.status", { status: (step.fault?.status ?? 0).toString(16).toUpperCase().padStart(2, "0") });
  return kindLabel(kind);
}

/** Na com sa krok deje. */
export function stepTarget(step: ScenarioStepDto, boards: BoardDto[]): string {
  if (step.action === "lineOutage") return t("scenario.target.line", { line: step.line });
  if (step.address === null) return t("scenario.target.allBoards", { line: step.line });
  const board = boards.find((b) => b.line === step.line && b.address === step.address);
  return board?.name
    ? t("scenario.target.boardNamed", { line: step.line, address: step.address, name: board.name })
    : t("scenario.target.board", { line: step.line, address: step.address });
}

/** Cas v sekundach pre obsluhu ("12,5 s"). */
export const formatSeconds = (ms: number) => `${toSeconds(ms).toLocaleString(locale())} s`;
