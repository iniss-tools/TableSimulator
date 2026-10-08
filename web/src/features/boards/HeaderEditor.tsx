// Hlavicka tabule (stitok nad maticou): vyska, texty s polohou a hodiny panelu - polohy a vysky v bodoch matice.
import { Plus, Trash2 } from "lucide-react";
import type { BoardHeaderDto, HeaderTextDto } from "@/api";
import { CheckField, FormField } from "@/components/common/form-field";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { t } from "@/i18n";

const num = (value: string) => Number.parseInt(value, 10) || 0;

export function HeaderEditor({ header, width, onChange }: { header: BoardHeaderDto | null; width: number; onChange: (h: BoardHeaderDto | null) => void }) {
  if (!header) {
    return (
      <Button type="button" variant="outline" className="self-start" onClick={() => onChange({ height: 20, texts: [], clock: null })}>
        <Plus />
        {t("header.add")}
      </Button>
    );
  }

  const setText = (i: number, patch: Partial<HeaderTextDto>) => onChange({ ...header, texts: header.texts.map((x, j) => (j === i ? { ...x, ...patch } : x)) });
  const clock = header.clock;

  return (
    <div className="flex flex-col gap-4">
      <div className="grid items-end gap-4 sm:grid-cols-2">
        <FormField label={t("header.height")}>
          <Input type="number" min={1} max={200} value={header.height} onChange={(e) => onChange({ ...header, height: num(e.target.value) })} />
        </FormField>
        <CheckField
          label={t("header.clock")}
          checked={!!clock}
          onCheckedChange={(on) => onChange({ ...header, clock: on ? { x: Math.max(0, width - 45), y: 4, height: 8 } : null })}
          className="pb-2"
        />
      </div>
      {clock && (
        <FormField label={t("header.clockPos")}>
          <div className="flex gap-2">
            <Input
              type="number"
              aria-label="X"
              className="w-24"
              value={clock.x}
              onChange={(e) => onChange({ ...header, clock: { ...clock, x: num(e.target.value) } })}
            />
            <Input
              type="number"
              aria-label="Y"
              className="w-24"
              value={clock.y}
              onChange={(e) => onChange({ ...header, clock: { ...clock, y: num(e.target.value) } })}
            />
            <Input
              type="number"
              aria-label={t("header.textHeight")}
              className="w-24"
              min={1}
              value={clock.height}
              onChange={(e) => onChange({ ...header, clock: { ...clock, height: num(e.target.value) } })}
            />
          </div>
        </FormField>
      )}
      {header.texts.length > 0 && (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-20">X</TableHead>
                <TableHead className="w-20">Y</TableHead>
                <TableHead className="w-24">{t("header.textHeight")}</TableHead>
                <TableHead>{t("header.text")}</TableHead>
                <TableHead className="w-10" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {header.texts.map((text, i) => (
                <TableRow key={i}>
                  <TableCell>
                    <Input type="number" aria-label="X" value={text.x} onChange={(e) => setText(i, { x: num(e.target.value) })} />
                  </TableCell>
                  <TableCell>
                    <Input type="number" aria-label="Y" value={text.y} onChange={(e) => setText(i, { y: num(e.target.value) })} />
                  </TableCell>
                  <TableCell>
                    <Input
                      type="number"
                      aria-label={t("header.textHeight")}
                      min={1}
                      value={text.height}
                      onChange={(e) => setText(i, { height: num(e.target.value) })}
                    />
                  </TableCell>
                  <TableCell>
                    <Input aria-label={t("header.text")} value={text.text} onChange={(e) => setText(i, { text: e.target.value })} />
                  </TableCell>
                  <TableCell>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      title={t("common.delete")}
                      onClick={() => onChange({ ...header, texts: header.texts.filter((_, j) => j !== i) })}
                    >
                      <Trash2 />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
      <div className="flex flex-wrap justify-between gap-2">
        <Button type="button" variant="outline" onClick={() => onChange({ ...header, texts: [...header.texts, { x: 0, y: 1, height: 8, text: "" }] })}>
          <Plus />
          {t("header.addText")}
        </Button>
        <Button type="button" variant="ghost" className="text-destructive" onClick={() => onChange(null)}>
          <Trash2 />
          {t("header.remove")}
        </Button>
      </div>
    </div>
  );
}
