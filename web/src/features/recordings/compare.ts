// Porovnanie zaznamov: casy od zaciatku zaznamu a zoskupenie rovnakych krokov.
import type { ComparisonStepDto } from "@/api";
import { locale } from "@/i18n";

/** Cas od zaciatku zaznamu: 4:05 alebo 1:02:03. */
export function elapsed(seconds: number): string {
  const total = Math.floor(seconds);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = String(total % 60).padStart(2, "0");
  return h > 0 ? `${h}:${String(m).padStart(2, "0")}:${s}` : `${m}:${s}`;
}

/** Skutocny cas (zaciatok zaznamu + sekundy) v jazyku obsluhy. */
export function clockTime(created: string, seconds: number): string {
  return new Date(Date.parse(created) + seconds * 1000).toLocaleTimeString(locale());
}

/** Kroky porovnania: rovnake idu za sebou v jednej skupine (zbalenej), ostatne po jednom. */
export type StepGroup = { kind: "same"; steps: ComparisonStepDto[] } | { kind: "diff"; step: ComparisonStepDto };

export function groupSteps(steps: ComparisonStepDto[]): StepGroup[] {
  const groups: StepGroup[] = [];
  for (const step of steps) {
    const last = groups[groups.length - 1];
    if (step.kind === "same" && last?.kind === "same") last.steps.push(step);
    else if (step.kind === "same") groups.push({ kind: "same", steps: [step] });
    else groups.push({ kind: "diff", step });
  }
  return groups;
}

/** Riadky, ktore sa v dvojici stavov lisia (podla poradia riadku). */
export function changedRows(a: string[] | undefined, b: string[] | undefined): Set<number> {
  const changed = new Set<number>();
  const count = Math.max(a?.length ?? 0, b?.length ?? 0);
  for (let i = 0; i < count; i++) if ((a?.[i] ?? "") !== (b?.[i] ?? "")) changed.add(i);
  return changed;
}
