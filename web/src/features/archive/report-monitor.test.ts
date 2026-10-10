import { expect, test } from "bun:test";
import type { ReportArchiveEntryDto } from "@/api";
import { matchesReport } from "./report-monitor";

const entry = (name: string, train = "", text = ""): ReportArchiveEntryDto => ({
  time: "06:00:00",
  kind: "hlásenie",
  name,
  auto: false,
  station: "Dolné Mesto",
  train: train ? { type: "Os", number: train, name: "Rozkvet", date: null } : null,
  arrivalTrack: "",
  departureTrack: "",
  arrivalDelay: null,
  departureDelay: null,
  text,
  result: "OK",
  details: [{ key: "vyl", value: "V12 Výluka Lehota" }],
});

test("vyber podla vlaku a textu bez diakritiky", () => {
  const e = entry("Príchod", "7813", "Osobný vlak prichádza");
  expect(matchesReport(e, "781", "")).toBe(true);
  expect(matchesReport(e, "601", "")).toBe(false);
  expect(matchesReport(e, "", "PRICHADZA")).toBe(true);
  expect(matchesReport(e, "", "vyluka lehota")).toBe(true);
  expect(matchesReport(entry("Info"), "7", "")).toBe(false);
});
