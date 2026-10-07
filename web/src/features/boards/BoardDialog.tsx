// Nova tabula alebo uprava tabule: identita, profil (hardver vratane hlavicky) a casovanie - po zalozkach.
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  errorText,
  getDesignsOptions,
  getFontsOptions,
  getMonitorOptions,
  manufacturersFor,
  setBoardMutation,
  useSimulatorState,
  type BoardProfileDto,
  type SetBoardRequest,
} from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { CheckField, FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { FieldDescription, FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { defaultProfile } from "@/lib/boards";
import { t } from "@/i18n";
import { HeaderEditor } from "./HeaderEditor";

const num = (value: string) => Number.parseInt(value, 10) || 0;

/** Bez vyberu v zozname (prazdna hodnota = predvolene / ziadne). */
const NONE = "";

export function BoardDialog({ initial, isNew, onClose }: { initial: SetBoardRequest; isNew: boolean; onClose: () => void }) {
  const live = useSimulatorState();
  const [config, setConfig] = useState<SetBoardRequest>(initial);
  const [template, setTemplate] = useState(NONE);
  const fonts = useQuery(getFontsOptions()).data ?? [];
  const designs = useQuery(getDesignsOptions()).data ?? [];
  const templates = useQuery(getMonitorOptions()).data?.panels ?? [];
  const save = useMutation({ ...setBoardMutation(), onSuccess: onClose });

  const p = config.profile;
  const profile = (patch: Partial<BoardProfileDto>) => setConfig({ ...config, profile: { ...p, ...patch } });
  const lineManufacturers = (line: number) => manufacturersFor(live, live.lines.find((l) => l.number === line)?.tableClass);
  const manufacturers = lineManufacturers(config.line);
  // vyrobca, ktory linka nepozna (zo starsich dat), ostava vo vybere
  const shownManufacturers = config.manufacturer && !manufacturers.includes(config.manufacturer) ? [config.manufacturer, ...manufacturers] : manufacturers;
  const fontFiles = p.fontFile && !fonts.includes(p.fontFile) ? [p.fontFile, ...fonts] : fonts;

  const applyTemplate = (file: string) => {
    setTemplate(file);
    const found = templates.find((x) => x.file === file);
    if (found) setConfig({ ...config, profile: { ...found.profile, autoSize: false }, name: config.name || found.name });
  };

  const title = isNew ? t("dialog.board.new") : t("dialog.board.edit", { name: config.name || t("board.unnamed", { address: config.address }) });
  return (
    <FormDialog
      title={title}
      onClose={onClose}
      onSubmit={() => save.mutate({ path: { line: config.line, address: config.address }, body: config })}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="lg"
    >
      <Tabs defaultValue="board">
        <TabsList>
          <TabsTrigger value="board">{t("board.tab.board")}</TabsTrigger>
          <TabsTrigger value="profile">{t("board.tab.profile")}</TabsTrigger>
          <TabsTrigger value="header">{t("section.header")}</TabsTrigger>
          <TabsTrigger value="timing">{t("section.timing")}</TabsTrigger>
        </TabsList>

        <TabsContent value="board" className="pt-2">
          <FieldGroup>
            <div className="grid gap-4 sm:grid-cols-3">
              <FormField label={t("field.line")}>
                <OptionSelect
                  value={config.line}
                  disabled={!isNew}
                  onValueChange={(line) => {
                    const available = lineManufacturers(line);
                    const manufacturer = available.includes(config.manufacturer) ? config.manufacturer : (available[0] ?? config.manufacturer);
                    setConfig({ ...config, line, manufacturer, profile: manufacturer === config.manufacturer ? p : defaultProfile(manufacturer) });
                  }}
                  options={live.lines.map((l) => ({ value: l.number, label: l.name ? `${l.number} – ${l.name}` : String(l.number) }))}
                  className="w-full"
                />
              </FormField>
              <FormField label={t("field.address")}>
                <Input type="number" min={1} max={255} value={config.address} disabled={!isNew} onChange={(e) => setConfig({ ...config, address: num(e.target.value) })} />
              </FormField>
              <FormField label={t("field.secondAddress")}>
                <Input
                  type="number"
                  min={1}
                  max={127}
                  value={config.secondAddress ?? ""}
                  onChange={(e) => setConfig({ ...config, secondAddress: e.target.value ? num(e.target.value) : null })}
                />
              </FormField>
            </div>
            <FieldDescription className="-mt-4">{t("field.secondAddress.hint")}</FieldDescription>
            <FormField label={t("field.name")}>
              <Input value={config.name} onChange={(e) => setConfig({ ...config, name: e.target.value })} />
            </FormField>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("field.manufacturer")}>
                <OptionSelect
                  value={config.manufacturer}
                  onValueChange={(manufacturer) => setConfig({ ...config, manufacturer })}
                  options={shownManufacturers.map((m) => ({ value: m, label: m }))}
                  className="w-full"
                />
              </FormField>
              <FormField label={t("field.design")}>
                <OptionSelect
                  value={config.design ?? NONE}
                  onValueChange={(design) => setConfig({ ...config, design: design || null })}
                  options={[{ value: NONE, label: t("common.default") }, ...designs.map((d) => ({ value: d.id, label: d.name }))]}
                  className="w-full"
                />
              </FormField>
            </div>
          </FieldGroup>
        </TabsContent>

        <TabsContent value="profile" className="pt-2">
          <FieldGroup>
            <FieldDescription>{t("section.profile.hint")}</FieldDescription>
            {templates.length > 0 && (
              <FormField label={t("field.template")} hint={t("field.template.hint")}>
                <OptionSelect
                  value={template}
                  onValueChange={applyTemplate}
                  options={[
                    { value: NONE, label: t("field.template.pick") },
                    ...templates.map((x) => ({ value: x.file, label: `${x.name} (${x.profile.width}×${x.profile.rows * x.profile.rowHeight})` })),
                  ]}
                  className="w-full"
                />
              </FormField>
            )}
            <div className="grid gap-4 sm:grid-cols-3">
              <FormField label={t("field.width")}>
                <Input type="number" min={8} max={2048} value={p.width} onChange={(e) => profile({ width: num(e.target.value) })} />
              </FormField>
              <FormField label={t("field.rows")}>
                <Input type="number" min={1} max={64} value={p.rows} onChange={(e) => profile({ rows: num(e.target.value) })} />
              </FormField>
              <FormField label={t("field.rowHeight")}>
                <Input type="number" min={5} max={64} value={p.rowHeight} onChange={(e) => profile({ rowHeight: num(e.target.value) })} />
              </FormField>
            </div>
            <FormField label={t("field.fontFile")}>
              <OptionSelect
                value={p.fontFile ?? NONE}
                onValueChange={(fontFile) => profile({ fontFile: fontFile || null })}
                options={[{ value: NONE, label: t("common.none") }, ...fontFiles.map((f) => ({ value: f, label: f }))]}
                className="w-full"
              />
            </FormField>
            <div className="grid gap-3 sm:grid-cols-2">
              <CheckField label={t("field.monochrome")} checked={p.monochrome} onCheckedChange={(monochrome) => profile({ monochrome })} />
              <CheckField label={t("field.swapRedGreen")} checked={p.swapRedGreen} onCheckedChange={(swapRedGreen) => profile({ swapRedGreen })} />
              <CheckField label={t("field.autoSize")} checked={p.autoSize} onCheckedChange={(autoSize) => profile({ autoSize })} />
            </div>
          </FieldGroup>
        </TabsContent>

        <TabsContent value="header" className="flex flex-col gap-4 pt-2">
          <FieldDescription>{t("section.header.hint")}</FieldDescription>
          <HeaderEditor header={p.header} width={p.width} onChange={(header) => profile({ header })} />
        </TabsContent>

        <TabsContent value="timing" className="pt-2">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("field.alternationMs")}>
              <Input type="number" min={100} step={100} value={p.alternationMs} onChange={(e) => profile({ alternationMs: num(e.target.value) })} />
            </FormField>
            <FormField label={t("field.scrollSpeed")}>
              <Input type="number" min={1} max={500} value={p.scrollSpeed} onChange={(e) => profile({ scrollSpeed: num(e.target.value) })} />
            </FormField>
            <FormField label={t("field.blinkOnMs")}>
              <Input type="number" min={50} step={50} value={p.blinkOnMs} onChange={(e) => profile({ blinkOnMs: num(e.target.value) })} />
            </FormField>
            <FormField label={t("field.blinkOffMs")}>
              <Input type="number" min={50} step={50} value={p.blinkOffMs} onChange={(e) => profile({ blinkOffMs: num(e.target.value) })} />
            </FormField>
            <FormField label={t("field.movingFont")}>
              <Input
                className="font-mono"
                value={`0x${p.movingFont.toString(16).toUpperCase()}`}
                onChange={(e) => {
                  const v = Number.parseInt(e.target.value.replace(/^0x/i, ""), 16);
                  if (!Number.isNaN(v)) profile({ movingFont: v });
                }}
              />
            </FormField>
          </div>
        </TabsContent>
      </Tabs>
    </FormDialog>
  );
}
