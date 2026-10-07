// Editor rozlozenia virtualnej tabule s nahladom na udajoch vybranej tabule (alebo ukazkovych udajoch).
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import {
  errorText,
  getVirtualViewOptions,
  setVirtualLayoutMutation,
  type VirtualAlign,
  type VirtualBoardDto,
  type VirtualClock,
  type VirtualColorsDto,
  type VirtualColumnDto,
  type VirtualLayoutDto,
  type VirtualViewDto,
} from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { CheckField, FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { FieldDescription, FieldGroup, FieldLegend, FieldSet } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { t, type TextKey } from "@/i18n";
import { CELL_NAMES, defaultColumn, layoutColumns } from "@/render/virtual";
import { layoutName } from "./layouts";
import { useNow, VirtualScreen } from "./VirtualScreen";

const COLORS: (keyof VirtualColorsDto)[] = ["background", "text", "headBackground", "headText", "accent", "muted", "delay", "rowLine"];
const CLOCKS: VirtualClock[] = ["none", "minutes", "seconds"];
const ALIGNS: VirtualAlign[] = ["left", "center", "right"];
/** Nahlad na ukazkovych udajoch. */
const SAMPLE = 0;

/** Ukazkove udaje, ak ziadna virtualna tabula nie je. */
function sample(layout: VirtualLayoutDto): VirtualViewDto {
  const row = (time: string, type: string, number: string, destination: string, track: string, delay = "") => ({
    cells: [
      { name: "type", text: type },
      { name: "number", text: number },
      { name: "time", text: time },
      { name: "delay", text: delay },
      { name: "track", text: track },
      { name: "destination", text: destination },
    ],
  });
  return {
    board: { id: 0, name: "Odchody", description: "", source: "", rowCount: null, rowStart: 1, separator: "", template: "", parameter: "", layout: layout.id },
    source: { name: "", path: "", modified: "", station: "Dolné Mesto", key: null, written: null, validTo: null, rows: 4, stale: false, error: null },
    rows: [
      row("11:35", "Os", "7813", "Horné Mesto", "2", "5"),
      row("11:52", "R", "601", "Veľká Ves#Bratislava", "1"),
      row("12:10", "REX", "1771", "Nové Zámky", "3#"),
      row("12:35", "Os", "7815", "Horné Mesto", "2"),
    ],
    layout,
  };
}

interface Props {
  initial: VirtualLayoutDto;
  isNew: boolean;
  boards: VirtualBoardDto[];
  onClose: () => void;
}

export function VirtualLayoutDialog({ initial, isNew, boards, onClose }: Props) {
  const [layout, setLayout] = useState(initial);
  const [boardId, setBoardId] = useState(boards.find((b) => b.layout === initial.id)?.id ?? boards[0]?.id ?? SAMPLE);
  // nahlad na datach vybranej tabule
  const view = useQuery({ ...getVirtualViewOptions({ path: { id: boardId } }), enabled: boardId !== SAMPLE }).data;
  const now = useNow(250);
  const save = useMutation({ ...setVirtualLayoutMutation(), onSuccess: onClose });

  const set = (patch: Partial<VirtualLayoutDto>) => setLayout({ ...layout, ...patch });
  const setColor = (key: keyof VirtualColorsDto, value: string) => set({ colors: { ...layout.colors, [key]: value } });
  const setColumn = (index: number, patch: Partial<VirtualColumnDto>) => set({ columns: layout.columns.map((c, i) => (i === index ? { ...c, ...patch } : c)) });
  const move = (index: number, by: number) => {
    const columns = layout.columns.slice();
    const [column] = columns.splice(index, 1);
    columns.splice(index + by, 0, column);
    set({ columns });
  };
  const preview = (boardId !== SAMPLE && view) || sample(layout);
  const number = (value: string, fallback: number) => Number.parseInt(value, 10) || fallback;

  return (
    <FormDialog
      title={isNew ? t("dialog.layout.new") : t("dialog.layout.edit", { name: layoutName(layout) })}
      onClose={onClose}
      onSubmit={() => save.mutate({ path: { id: layout.id }, body: layout })}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="xl"
    >
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <FieldGroup>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("layout.field.id")} hint={isNew ? t("layout.field.id.hint") : undefined}>
              <Input className="font-mono" value={layout.id} disabled={!isNew} onChange={(e) => set({ id: e.target.value.trim().toLowerCase() })} />
            </FormField>
            <FormField label={t("layout.field.name")}>
              <Input value={layout.name} onChange={(e) => set({ name: e.target.value })} />
            </FormField>
          </div>
          <FormField label={t("layout.field.title")} hint={t("layout.field.title.hint")}>
            <Input value={layout.title} onChange={(e) => set({ title: e.target.value })} />
          </FormField>
          <div className="grid gap-3 sm:grid-cols-3">
            <CheckField label={t("layout.field.showStation")} checked={layout.showStation} onCheckedChange={(showStation) => set({ showStation })} />
            <CheckField label={t("layout.field.showDate")} checked={layout.showDate} onCheckedChange={(showDate) => set({ showDate })} />
            <CheckField label={t("layout.field.columnHeaders")} checked={layout.columnHeaders} onCheckedChange={(columnHeaders) => set({ columnHeaders })} />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("layout.field.clock")}>
              <OptionSelect<VirtualClock>
                value={layout.clock}
                onValueChange={(clock) => set({ clock })}
                options={CLOCKS.map((c) => ({ value: c, label: t(`layout.clock.${c}` as TextKey) }))}
                className="w-full"
              />
            </FormField>
            <FormField label={t("layout.field.rows")} hint={t("layout.field.rows.hint")}>
              <Input type="number" min={1} max={40} value={layout.rows} onChange={(e) => set({ rows: number(e.target.value, 1) })} />
            </FormField>
          </div>
          <FormField label={t("layout.field.font")} hint={t("layout.field.font.hint")}>
            <Input value={layout.font} placeholder="Segoe UI, Arial, sans-serif" onChange={(e) => set({ font: e.target.value })} />
          </FormField>

          <FieldSet>
            <FieldLegend>{t("layout.colors")}</FieldLegend>
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              {COLORS.map((key) => (
                <label key={key} className="flex items-center gap-2 text-sm">
                  <input type="color" className="size-7 cursor-pointer rounded border bg-transparent" value={layout.colors[key]} onChange={(e) => setColor(key, e.target.value)} />
                  {t(`layout.color.${key}` as TextKey)}
                </label>
              ))}
              <label className="flex items-center gap-2 text-sm">
                <Checkbox
                  checked={layout.colors.rowAlternate !== ""}
                  onCheckedChange={(on) => setColor("rowAlternate", on ? layout.colors.rowLine : "")}
                  aria-label={t("layout.color.rowAlternate")}
                />
                {layout.colors.rowAlternate !== "" && (
                  <input
                    type="color"
                    className="size-7 cursor-pointer rounded border bg-transparent"
                    value={layout.colors.rowAlternate}
                    onChange={(e) => setColor("rowAlternate", e.target.value)}
                  />
                )}
                {t("layout.color.rowAlternate")}
              </label>
            </div>
          </FieldSet>

          <FieldSet>
            <FieldLegend>{t("layout.timing")}</FieldLegend>
            <div className="grid gap-4 sm:grid-cols-3">
              <FormField label={t("layout.field.alternationMs")}>
                <Input type="number" min={300} max={20000} step={100} value={layout.alternationMs} onChange={(e) => set({ alternationMs: number(e.target.value, 1500) })} />
              </FormField>
              <FormField label={t("layout.field.blinkMs")}>
                <Input type="number" min={100} max={5000} step={50} value={layout.blinkMs} onChange={(e) => set({ blinkMs: number(e.target.value, 600) })} />
              </FormField>
              <FormField label={t("layout.field.blinkPhases")} hint={t("layout.field.blinkPhases.hint")}>
                <Input type="number" min={2} max={10} value={layout.blinkPhases} onChange={(e) => set({ blinkPhases: number(e.target.value, 3) })} />
              </FormField>
            </div>
          </FieldSet>
        </FieldGroup>

        <div className="flex flex-col gap-3 lg:sticky lg:top-0 lg:self-start">
          <FormField label={t("layout.preview")}>
            <OptionSelect
              value={boardId}
              onValueChange={setBoardId}
              options={[{ value: SAMPLE, label: t("layout.preview.sample") }, ...boards.map((b) => ({ value: b.id, label: `${b.id} – ${b.name}` }))]}
              className="w-full"
            />
          </FormField>
          <VirtualScreen view={preview} layout={layout} now={now} preview />
        </div>
      </div>

      <FieldSet>
        <FieldLegend>{t("layout.columns")}</FieldLegend>
        <CheckField
          label={t("layout.columns.auto")}
          checked={layout.columns.length === 0}
          onCheckedChange={(auto) =>
            set({
              columns: auto ? [] : layoutColumns({ ...layout, columns: [] }, preview.rows).map(({ key: _, ...column }) => column),
            })
          }
        />
        {layout.columns.length > 0 && (
          <>
            <FieldDescription>{t("layout.column.hint")}</FieldDescription>
            <datalist id="layout-cells">
              {CELL_NAMES.map((n) => (
                <option key={n} value={n} />
              ))}
            </datalist>
            <datalist id="layout-widths">
              {["fill", "8%", "12%", "20%", "6em"].map((w) => (
                <option key={w} value={w} />
              ))}
            </datalist>
            <datalist id="layout-colors">
              {["accent", "muted", "delay"].map((c) => (
                <option key={c} value={c} />
              ))}
            </datalist>
            <div className="rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t("layout.column.cells")}</TableHead>
                    <TableHead>{t("layout.column.label")}</TableHead>
                    <TableHead className="w-24">{t("layout.column.width")}</TableHead>
                    <TableHead className="w-32">{t("layout.column.align")}</TableHead>
                    <TableHead className="w-24">{t("layout.column.color")}</TableHead>
                    <TableHead className="w-14">{t("layout.column.bold")}</TableHead>
                    <TableHead className="w-24">{t("layout.column.size")}</TableHead>
                    <TableHead className="w-20">{t("layout.column.separator")}</TableHead>
                    <TableHead className="w-28" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {layout.columns.map((c, i) => (
                    <TableRow key={i}>
                      <TableCell>
                        <Input
                          list="layout-cells"
                          aria-label={t("layout.column.cells")}
                          className="min-w-36 font-mono"
                          value={c.cells.join(" ")}
                          onChange={(e) => setColumn(i, { cells: e.target.value.split(/[\s,+]+/).filter((n) => n !== "") })}
                        />
                      </TableCell>
                      <TableCell>
                        <Input aria-label={t("layout.column.label")} className="min-w-28" value={c.label} onChange={(e) => setColumn(i, { label: e.target.value })} />
                      </TableCell>
                      <TableCell>
                        <Input list="layout-widths" aria-label={t("layout.column.width")} value={c.width} onChange={(e) => setColumn(i, { width: e.target.value.trim() })} />
                      </TableCell>
                      <TableCell>
                        <OptionSelect<VirtualAlign>
                          value={c.align}
                          onValueChange={(align) => setColumn(i, { align })}
                          ariaLabel={t("layout.column.align")}
                          options={ALIGNS.map((a) => ({ value: a, label: t(`layout.align.${a}` as TextKey) }))}
                          className="w-full"
                        />
                      </TableCell>
                      <TableCell>
                        <Input list="layout-colors" aria-label={t("layout.column.color")} value={c.color} onChange={(e) => setColumn(i, { color: e.target.value.trim() })} />
                      </TableCell>
                      <TableCell>
                        <Checkbox checked={c.bold} aria-label={t("layout.column.bold")} onCheckedChange={(bold) => setColumn(i, { bold: bold === true })} />
                      </TableCell>
                      <TableCell>
                        <Input
                          type="number"
                          min={30}
                          max={300}
                          step={10}
                          aria-label={t("layout.column.size")}
                          value={c.size}
                          onChange={(e) => setColumn(i, { size: number(e.target.value, 100) })}
                        />
                      </TableCell>
                      <TableCell>
                        <Input aria-label={t("layout.column.separator")} value={c.separator} onChange={(e) => setColumn(i, { separator: e.target.value })} />
                      </TableCell>
                      <TableCell>
                        <div className="flex">
                          <Button type="button" variant="ghost" size="icon-sm" title={t("common.up")} disabled={i === 0} onClick={() => move(i, -1)}>
                            <ArrowUp />
                          </Button>
                          <Button type="button" variant="ghost" size="icon-sm" title={t("common.down")} disabled={i === layout.columns.length - 1} onClick={() => move(i, 1)}>
                            <ArrowDown />
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon-sm"
                            title={t("common.delete")}
                            onClick={() => set({ columns: layout.columns.filter((_, j) => j !== i) })}
                          >
                            <Trash2 />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
            <Button type="button" variant="outline" className="self-start" onClick={() => set({ columns: [...layout.columns, defaultColumn(["time"])] })}>
              <Plus />
              {t("layout.column.add")}
            </Button>
          </>
        )}
      </FieldSet>
    </FormDialog>
  );
}
