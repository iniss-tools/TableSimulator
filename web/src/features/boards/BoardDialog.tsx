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
import { Button } from "@/components/ui/button";
import { FieldDescription, FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { boardKind, defaultProfile } from "@/lib/boards";
import { CHAR_WIDTH } from "@/render/chars";
import { t } from "@/i18n";
import { hasCharFonts, usedCharFonts } from "./char-fonts";
import { CharFontsEditor } from "./CharFontsEditor";
import { FlapListsEditor } from "./FlapListsEditor";
import { FaceSection } from "./FaceSection";
import { baseFontHeight, useFontHeight } from "./font-height";

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
  const fontHeight = useFontHeight(p.fontFile);

  // pismo vyssie ako riadok by sa zdola orezalo - vyska riadku sa zvysi na vysku pisma (po nacitani suboru)
  const chooseFont = (fontFile: string | null) => {
    setConfig((c) => ({ ...c, profile: { ...c.profile, fontFile } }));
    if (!fontFile) return;
    void baseFontHeight(fontFile).then((height) => {
      if (!height) return;
      setConfig((c) => (c.profile.fontFile === fontFile && c.profile.rowHeight < height ? { ...c, profile: { ...c.profile, rowHeight: height } } : c));
    });
  };
  const lineManufacturers = (line: number) => manufacturersFor(live, live.lines.find((l) => l.number === line)?.tableClass);
  const manufacturers = lineManufacturers(config.line);
  // vyrobca, ktory linka nepozna (zo starsich dat), ostava vo vybere
  const shownManufacturers = config.manufacturer && !manufacturers.includes(config.manufacturer) ? [config.manufacturer, ...manufacturers] : manufacturers;
  const fontFiles = p.fontFile && !fonts.includes(p.fontFile) ? [p.fontFile, ...fonts] : fonts;
  const kind = boardKind(config.manufacturer);
  // listova tabula: pozicie (moduly) namiesto bodov, cas otacania; pisma, hlavicka a casovanie textov nemaju zmysel
  const flaps = kind === "flaps";
  // spinacia jednotka zosilnovaca: len linka, adresa (aj 0), nazov a vyrobca
  const amplifier = kind === "amplifier";
  const board = !flaps && !amplifier;
  // pisma ELEN, farebnost panelu, pohyblivy text a typy panelov Monitora ma len ELEN; znakova tabula ma sirku v znakoch
  const elen = kind === "elen";
  // LCD1 a ERP dostavaju pri kazdom znaku cislo pisma - jeho vyznam priradi obsluha
  const charFonts = kind === "chars" && hasCharFonts(config.manufacturer);
  const liveBoard = isNew ? undefined : live.boards.find((b) => b.line === config.line && b.address === config.address);
  // vzhlady s kreslenim pre druh tabule (listove pre listove, body pre ostatne); zvoleny ostava vo vybere
  const renderer = flaps ? "flaps" : "led-matrix";
  const shownDesigns = designs.filter((d) => d.renderer === renderer || d.id === config.design);

  // iny vyrobca: novej tabule s nezmenenym profilom a tabuli inej rodiny patri predvoleny profil noveho vyrobcu
  const changeManufacturer = (c: SetBoardRequest, manufacturer: string): SetBoardRequest => {
    if (manufacturer === c.manufacturer) return c;
    const untouched = isNew && JSON.stringify(c.profile) === JSON.stringify(defaultProfile(c.manufacturer));
    const reset = untouched || boardKind(manufacturer) !== boardKind(c.manufacturer);
    return { ...c, manufacturer, profile: reset ? defaultProfile(manufacturer) : c.profile };
  };

  const applyTemplate = (file: string) => {
    setTemplate(file);
    const found = templates.find((x) => x.file === file);
    if (found) setConfig({ ...config, profile: { ...found.profile, autoSize: false }, name: config.name || found.name });
  };

  const title = isNew
    ? t("dialog.board.new")
    : amplifier
      ? t("dialog.amplifier.edit", { name: config.name || t("amplifier.unnamed", { address: config.address }) })
      : t("dialog.board.edit", { name: config.name || t("board.unnamed", { address: config.address }) });
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
          {!amplifier && <TabsTrigger value="profile">{t("board.tab.profile")}</TabsTrigger>}
          {flaps && p.flaps && <TabsTrigger value="lists">{t("board.tab.lists")}</TabsTrigger>}
          {charFonts && <TabsTrigger value="fonts">{t("board.tab.fonts")}</TabsTrigger>}
          {!amplifier && <TabsTrigger value="face">{t("board.tab.face")}</TabsTrigger>}
          {board && <TabsTrigger value="timing">{t("section.timing")}</TabsTrigger>}
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
                    setConfig(changeManufacturer({ ...config, line }, manufacturer));
                  }}
                  options={live.lines.map((l) => ({ value: l.number, label: l.name ? `${l.number} – ${l.name}` : String(l.number) }))}
                  className="w-full"
                />
              </FormField>
              <FormField label={t("field.address")}>
                <Input
                  type="number"
                  min={amplifier ? 0 : 1}
                  max={255}
                  value={config.address}
                  disabled={!isNew}
                  onChange={(e) => setConfig({ ...config, address: num(e.target.value) })}
                />
              </FormField>
              {board && (
                <FormField label={t("field.secondAddress")}>
                  <Input
                    type="number"
                    min={1}
                    max={127}
                    value={config.secondAddress ?? ""}
                    onChange={(e) => setConfig({ ...config, secondAddress: e.target.value ? num(e.target.value) : null })}
                  />
                </FormField>
              )}
            </div>
            {board && <FieldDescription className="-mt-4">{t("field.secondAddress.hint")}</FieldDescription>}
            {amplifier && <FieldDescription className="-mt-4">{t("field.address.amplifier.hint")}</FieldDescription>}
            <FormField label={t("field.name")}>
              <Input value={config.name} onChange={(e) => setConfig({ ...config, name: e.target.value })} />
            </FormField>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("field.manufacturer")}>
                <OptionSelect
                  value={config.manufacturer}
                  onValueChange={(manufacturer) => setConfig(changeManufacturer(config, manufacturer))}
                  options={shownManufacturers.map((m) => ({ value: m, label: m }))}
                  className="w-full"
                />
              </FormField>
              {!amplifier && (
                <FormField label={t("field.design")}>
                  <OptionSelect
                    value={config.design ?? NONE}
                    onValueChange={(design) => setConfig({ ...config, design: design || null })}
                    options={[{ value: NONE, label: t("common.default") }, ...shownDesigns.map((d) => ({ value: d.id, label: d.name }))]}
                    className="w-full"
                  />
                </FormField>
              )}
            </div>
          </FieldGroup>
        </TabsContent>

        <TabsContent value="profile" className="pt-2">
          <FieldGroup>
            <FieldDescription>{t(flaps ? "section.profile.flaps.hint" : "section.profile.hint")}</FieldDescription>
            {flaps && (
              <>
                <div className="grid gap-4 sm:grid-cols-3">
                  <FormField label={t("field.positions")}>
                    <Input
                      type="number"
                      min={1}
                      max={512}
                      value={Math.max(1, Math.round(p.width / 8))}
                      onChange={(e) => profile({ width: Math.max(1, num(e.target.value)) * 8 })}
                    />
                  </FormField>
                  <FormField label={t("field.rows")}>
                    <Input type="number" min={1} max={256} value={p.rows} onChange={(e) => profile({ rows: num(e.target.value) })} />
                  </FormField>
                  <FormField label={t("field.rotationMs")}>
                    <Input type="number" min={0} max={60000} step={100} value={p.rotationMs} onChange={(e) => profile({ rotationMs: num(e.target.value) })} />
                  </FormField>
                </div>
                <FieldDescription className="-mt-2">{t("field.rotationMs.hint")}</FieldDescription>
                <FieldDescription>
                  {p.flaps ? t("board.flaps.lists", { modules: p.flaps.modules.length, lists: Object.keys(p.flaps.lists).length }) : t("board.flaps.none")}
                </FieldDescription>
                <CheckField label={t("field.autoSize")} checked={p.autoSize} onCheckedChange={(autoSize) => profile({ autoSize })} />
              </>
            )}
            {elen && templates.length > 0 && (
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
            {!flaps && (
              <>
                <div className="grid gap-4 sm:grid-cols-3">
                  {elen ? (
                    <FormField label={t("field.width")}>
                      <Input type="number" min={8} max={2048} value={p.width} onChange={(e) => profile({ width: num(e.target.value) })} />
                    </FormField>
                  ) : (
                    <FormField label={t("field.columns")}>
                      <Input
                        type="number"
                        min={1}
                        max={256}
                        value={Math.max(1, Math.round(p.width / CHAR_WIDTH))}
                        onChange={(e) => profile({ width: Math.max(1, num(e.target.value)) * CHAR_WIDTH })}
                      />
                    </FormField>
                  )}
                  <FormField label={t("field.rows")}>
                    <Input type="number" min={1} max={64} value={p.rows} onChange={(e) => profile({ rows: num(e.target.value) })} />
                  </FormField>
                  <FormField label={t("field.rowHeight")}>
                    <Input type="number" min={5} max={64} value={p.rowHeight} onChange={(e) => profile({ rowHeight: num(e.target.value) })} />
                  </FormField>
                </div>
                {elen && (
                  <FormField label={t("field.fontFile")}>
                    <OptionSelect
                      value={p.fontFile ?? NONE}
                      onValueChange={(fontFile) => chooseFont(fontFile || null)}
                      options={[{ value: NONE, label: t("common.none") }, ...fontFiles.map((f) => ({ value: f, label: f }))]}
                      className="w-full"
                    />
                  </FormField>
                )}
                {elen && fontHeight !== null && p.rowHeight < fontHeight && (
                  <div className="-mt-2 flex flex-wrap items-center gap-x-3 gap-y-1">
                    <FieldDescription className="text-amber-700 dark:text-amber-300">
                      {t("board.fontTaller", { font: fontHeight, row: p.rowHeight })}
                    </FieldDescription>
                    <Button type="button" variant="outline" size="sm" onClick={() => profile({ rowHeight: fontHeight })}>
                      {t("board.fontTaller.fix", { font: fontHeight })}
                    </Button>
                  </div>
                )}
                <div className="grid gap-3 sm:grid-cols-2">
                  {elen && (
                    <>
                      <CheckField label={t("field.monochrome")} checked={p.monochrome} onCheckedChange={(monochrome) => profile({ monochrome })} />
                      <CheckField label={t("field.swapRedGreen")} checked={p.swapRedGreen} onCheckedChange={(swapRedGreen) => profile({ swapRedGreen })} />
                    </>
                  )}
                  <CheckField label={t("field.autoSize")} checked={p.autoSize} onCheckedChange={(autoSize) => profile({ autoSize })} />
                </div>
              </>
            )}
          </FieldGroup>
        </TabsContent>

        {flaps && p.flaps && (
          <TabsContent value="lists" className="pt-2">
            <FlapListsEditor flaps={p.flaps} onChange={(lists) => profile({ flaps: lists })} />
          </TabsContent>
        )}

        {charFonts && (
          <TabsContent value="fonts" className="pt-2">
            <CharFontsEditor fonts={p.charFonts} rowHeight={p.rowHeight} used={usedCharFonts(liveBoard)} onChange={(fonts) => profile({ charFonts: fonts })} />
          </TabsContent>
        )}

        {!amplifier && (
          <TabsContent value="face" className="pt-2">
            <FaceSection profile={p} onChange={profile} manufacturer={config.manufacturer} name={config.name} live={liveBoard} />
          </TabsContent>
        )}

        <TabsContent value="timing" className="pt-2">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("field.alternationMs")}>
              <Input type="number" min={100} step={100} value={p.alternationMs} onChange={(e) => profile({ alternationMs: num(e.target.value) })} />
            </FormField>
            {elen && (
              <FormField label={t("field.scrollSpeed")}>
                <Input type="number" min={1} max={500} value={p.scrollSpeed} onChange={(e) => profile({ scrollSpeed: num(e.target.value) })} />
              </FormField>
            )}
            <FormField label={t("field.blinkOnMs")}>
              <Input type="number" min={50} step={50} value={p.blinkOnMs} onChange={(e) => profile({ blinkOnMs: num(e.target.value) })} />
            </FormField>
            <FormField label={t("field.blinkOffMs")}>
              <Input type="number" min={50} step={50} value={p.blinkOffMs} onChange={(e) => profile({ blinkOffMs: num(e.target.value) })} />
            </FormField>
            {elen && (
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
            )}
          </div>
        </TabsContent>
      </Tabs>
    </FormDialog>
  );
}
