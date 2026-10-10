import { expect, test } from "bun:test";
import type { ScenarioStepDto } from "@/api";
import { fromSeconds, newStep, runPosition, scenarioLength, stepKind, toSeconds, withKind } from "./scenarios";

const step: ScenarioStepDto = { atMs: 1000, durationMs: 2500, line: 2, address: 5, action: "fault", fault: { kind: "nak", delayMs: 0, status: 0 } };

test("dlzka cyklu ako v jadre", () => {
  expect(scenarioLength({ cycleMs: null, steps: [step, { ...step, atMs: 4000, durationMs: 500 }] })).toBe(4500);
  expect(scenarioLength({ cycleMs: 60_000, steps: [step] })).toBe(60_000);
  expect(scenarioLength({ cycleMs: null, steps: [] })).toBe(1000);
});

test("volba co sa stane", () => {
  expect(stepKind(step)).toBe("nak");
  const delay = withKind(step, "delay");
  expect(delay.fault).toEqual({ kind: "delay", delayMs: 500, status: 0 });
  const power = withKind(step, "powerLoss");
  expect(power.action).toBe("powerLoss");
  expect(power.fault).toBeNull();
  expect(stepKind(power)).toBe("powerLoss");
});

test("novy krok za predchadzajucim", () => {
  expect(newStep(step, 1)).toMatchObject({ atMs: 3500, line: 2, address: 5 });
  expect(newStep(undefined, 3)).toMatchObject({ atMs: 0, line: 3, address: null });
});

test("poloha behu a sekundy", () => {
  const run = { startedAt: new Date(10_000).toISOString(), lengthMs: 4000, cycle: 0, activeSteps: [] };
  expect(runPosition(run, 19_500)).toBe(1500);
  expect(toSeconds(1550)).toBe(1.6);
  expect(fromSeconds("2,5")).toBe(2500);
  expect(fromSeconds("")).toBeNull();
});
