// Zoznamy listov listovej tabule: texty su z dat stanice (TKatalog a TabTab), farbu listu data nemaju - nastavuje sa tu,
// po jednom liste alebo celemu zoznamu naraz (napr. biele listy kolaji).
import { useState } from "react";
import type { FlapProfileDto } from "@/api";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { FieldDescription } from "@/components/ui/field";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { DEFAULT_FLAPS } from "@/render/design";
import { FLAP_COLOR_KEYS, type FlapColor } from "@/render/flaps";
import { t } from "@/i18n";

/** Bez nastavenej farby - podla textu (* = cerveny). */
const DEFAULT = "";
const COLORS = FLAP_COLOR_KEYS;

export function FlapListsEditor({ flaps, onChange }: { flaps: FlapProfileDto; onChange: (flaps: FlapProfileDto) => void }) {
  const names = Object.keys(flaps.lists).sort((a, b) => a.localeCompare(b));
  const [list, setList] = useState(names[0] ?? "");
  const entries = Object.entries(flaps.lists[list] ?? {});
  const colors = flaps.colors[list] ?? {};
  const colorOptions = [{ value: DEFAULT, label: t("flaps.color.default") }, ...COLORS.map((c) => ({ value: c, label: t(`flaps.color.${c}`) }))];

  const setColors = (next: Record<string, string>) => {
    const all = { ...flaps.colors };
    if (Object.keys(next).length > 0) all[list] = next;
    else delete all[list];
    onChange({ ...flaps, colors: all });
  };
  const setColor = (code: string, color: string) => {
    const next = { ...colors };
    if (color === DEFAULT) delete next[code];
    else next[code] = color;
    setColors(next);
  };

  if (names.length === 0) return <FieldDescription>{t("flaps.empty")}</FieldDescription>;

  return (
    <div className="flex flex-col gap-4">
      <FieldDescription>{t("flaps.lists.hint")}</FieldDescription>
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label={t("flaps.list")}>
          <OptionSelect value={list} onValueChange={setList} options={names.map((n) => ({ value: n, label: n }))} className="w-full" />
        </FormField>
        <FormField label={t("flaps.all")}>
          <OptionSelect
            value={null}
            placeholder={t("flaps.all.pick")}
            onValueChange={(color: string) => setColors(color === DEFAULT ? {} : Object.fromEntries(entries.map(([code]) => [code, color])))}
            options={colorOptions}
            className="w-full"
          />
        </FormField>
      </div>
      <div className="max-h-80 overflow-auto rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-16">{t("flaps.col.code")}</TableHead>
              <TableHead>{t("flaps.col.flap")}</TableHead>
              <TableHead className="w-48">{t("flaps.col.color")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {entries.map(([code, text]) => {
              const set = colors[code] as FlapColor | undefined;
              const shown = set ?? (text.startsWith("*") ? "red" : "black");
              const palette = DEFAULT_FLAPS.colors[shown];
              return (
                <TableRow key={code}>
                  <TableCell className="font-mono">{code === " " ? "␠" : code}</TableCell>
                  <TableCell>
                    <span className="inline-block min-w-24 rounded-sm px-2 py-0.5 font-semibold" style={{ background: palette.bottom, color: palette.text }}>
                      {text.replace(/^\*/, "") || " "}
                    </span>
                  </TableCell>
                  <TableCell>
                    <OptionSelect
                      value={set ?? DEFAULT}
                      onValueChange={(color: string) => setColor(code, color)}
                      options={colorOptions}
                      ariaLabel={t("flaps.colorOf", { code })}
                      className="w-full"
                    />
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
