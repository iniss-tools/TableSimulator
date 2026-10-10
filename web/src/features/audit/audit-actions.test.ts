// Kazdy prikaz servera ide do auditu pod svojim menom - stranka Audit ho ukazuje textom audit.action.<meno>. Test najde
// prikazy v zdrojoch Application (record XyzCommand) a overi, ze maju text (cestina ma rovnake kluce, strazi to typ).
import { expect, test } from "bun:test";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { sk } from "@/i18n/sk";

const APPLICATION = join(import.meta.dir, "..", "..", "..", "..", "TableSimulator.Application");

function commands(): string[] {
  const names = new Set<string>();
  for (const file of readdirSync(APPLICATION, { recursive: true, encoding: "utf8" })) {
    if (!file.endsWith(".cs") || /(^|[\\/])(bin|obj)[\\/]/.test(file)) continue;
    for (const match of readFileSync(join(APPLICATION, file), "utf8").matchAll(/\brecord\s+(\w+)Command\b/g)) names.add(match[1]);
  }
  return [...names].sort();
}

test("kazdy prikaz ma text v audite", () => {
  const found = commands();
  expect(found.length).toBeGreaterThan(20);
  expect(found.filter((name) => !(`audit.action.${name}` in sk))).toEqual([]);
});
