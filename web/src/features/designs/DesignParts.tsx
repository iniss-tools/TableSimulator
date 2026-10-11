// Casti editora vzhladu: sekcia formulara, ciselne pole, plocha a ram, celo tabule so stylmi textov a listy.
import type { ReactNode } from "react";
import { ColorField } from "@/components/common/color-field";
import { CheckField, FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { DEFAULT_FLAPS, type BoardDesign, type TextStyle } from "@/render/design";
import { FLAP_COLOR_KEYS } from "@/render/flaps";
import { t, type TextKey } from "@/i18n";

export function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <Card size="sm">
      <CardHeader className="border-b">
        <CardTitle>{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </CardHeader>
      <CardContent>
        <FieldGroup className="grid gap-4 sm:grid-cols-2">{children}</FieldGroup>
      </CardContent>
    </Card>
  );
}

export function NumberField({
  label,
  hint,
  value,
  step,
  min,
  max,
  onChange,
}: {
  label: string;
  hint?: string;
  value: number;
  step: number;
  min: number;
  max: number;
  onChange: (value: number) => void;
}) {
  return (
    <FormField label={label} hint={hint}>
      <Input type="number" className="max-w-32" step={step} min={min} max={max} value={value} onChange={(e) => onChange(Number(e.target.value))} />
    </FormField>
  );
}

type Change = (patch: Partial<BoardDesign>) => void;

/** Plocha (pozadie, plech, moduly, medzery, svy) a ram. */
export function SurfaceSection({ form, change }: { form: BoardDesign; change: Change }) {
  const dots = form.renderer === "led-matrix";
  const frame = (patch: Partial<BoardDesign["frame"]>) => change({ frame: { ...form.frame, ...patch } });
  const seam = (patch: Partial<BoardDesign["seam"]>) => change({ seam: { ...form.seam, ...patch } });
  return (
    <Section title={t("designs.section.surface")} description={t("designs.section.surface.hint")}>
      <ColorField
        label={t(dots ? "designs.background" : "designs.flaps.plate")}
        value={form.background}
        onChange={(background) => change({ background: background ?? "#000000" })}
      />
      {dots && (
        <ColorField
          label={t("designs.plate")}
          value={form.plate}
          noneLabel={t("designs.plate.none")}
          fallback="#1a1a1a"
          onChange={(plate) => change({ plate })}
        />
      )}
      {dots && (
        <>
          <NumberField
            label={t("designs.padding")}
            hint={t("designs.padding.hint")}
            value={form.padding}
            step={1}
            min={0}
            max={50}
            onChange={(padding) => change({ padding })}
          />
          <NumberField
            label={t("designs.moduleMargin")}
            hint={t("designs.moduleMargin.hint")}
            value={form.moduleMargin}
            step={0.5}
            min={0}
            max={20}
            onChange={(moduleMargin) => change({ moduleMargin })}
          />
          <NumberField
            label={t("designs.rowGap")}
            hint={t("designs.rowGap.hint")}
            value={form.rowGap}
            step={1}
            min={0}
            max={50}
            onChange={(rowGap) => change({ rowGap })}
          />
          <ColorField
            label={t("designs.gapColor")}
            value={form.gapColor}
            noneLabel={t("designs.gapColor.none")}
            fallback="#333333"
            onChange={(gapColor) => change({ gapColor })}
          />
          <ColorField
            label={t("designs.seam")}
            value={form.seam.color}
            noneLabel={t("designs.seam.none")}
            fallback="#0a0a0a"
            onChange={(color) => seam({ color })}
          />
          <NumberField
            label={t("designs.seam.width")}
            hint={t("designs.seam.width.hint")}
            value={form.seam.width}
            step={0.1}
            min={0}
            max={2}
            onChange={(width) => seam({ width })}
          />
        </>
      )}
      <NumberField label={t("designs.frame.width")} value={form.frame.width} step={1} min={0} max={100} onChange={(width) => frame({ width })} />
      <NumberField label={t("designs.frame.radius")} value={form.frame.radius} step={1} min={0} max={100} onChange={(radius) => frame({ radius })} />
      <ColorField label={t("designs.frame.color")} value={form.frame.color} onChange={(color) => frame({ color: color ?? "#000000" })} />
      <FormField label={t("designs.frame.style")}>
        <OptionSelect
          value={form.frame.style}
          onValueChange={(style) => frame({ style })}
          options={(["flat", "metal"] as const).map((s) => ({ value: s, label: t(`designs.frame.style.${s}`) }))}
          className="max-w-48"
        />
      </FormField>
      <FormField label={t("designs.frame.badge")} hint={t("designs.frame.badge.hint")}>
        <Input className="max-w-48" maxLength={20} value={form.frame.badge} onChange={(e) => frame({ badge: e.target.value })} />
      </FormField>
    </Section>
  );
}

/** Pomenovane styly textov cela, ktore pouzivaju predlohy. */
const STYLE_NAMES = ["title", "label", "number"] as const;

/** Pismo a farby cela tabule a pomenovane styly textov. */
export function FaceColorsSection({ form, change }: { form: BoardDesign; change: Change }) {
  const header = (patch: Partial<BoardDesign["header"]>) => change({ header: { ...form.header, ...patch } });
  const style = (name: string, patch: Partial<TextStyle>) => {
    const current = form.header.styles?.[name] ?? { color: null, font: null, bold: null };
    header({ styles: { ...form.header.styles, [name]: { ...current, ...patch } } });
  };
  return (
    <Section title={t("designs.section.header")} description={t("designs.section.header.hint")}>
      <ColorField label={t("designs.header.background")} value={form.header.background} onChange={(v) => header({ background: v ?? "#000000" })} />
      <ColorField
        label={t("designs.header.panel")}
        value={form.header.panel}
        noneLabel={t("designs.header.panel.none")}
        fallback={form.header.background}
        onChange={(panel) => header({ panel })}
      />
      <ColorField label={t("designs.header.color")} value={form.header.color} onChange={(v) => header({ color: v ?? "#ffffff" })} />
      <FormField label={t("designs.header.font")} hint={t("designs.font.hint")}>
        <Input value={form.header.font} onChange={(e) => header({ font: e.target.value })} />
      </FormField>
      <CheckField label={t("designs.header.bold")} checked={form.header.bold} onCheckedChange={(bold) => header({ bold })} />
      <div className="hidden sm:block" />
      <ColorField label={t("designs.header.clockColor")} value={form.header.clockColor} onChange={(v) => header({ clockColor: v ?? "#ffffff" })} />
      <ColorField
        label={t("designs.header.clockBackground")}
        value={form.header.clockBackground}
        onChange={(v) => header({ clockBackground: v ?? "#000000" })}
      />
      <FormField label={t("designs.header.clockFont")} hint={t("designs.font.hint")}>
        <Input value={form.header.clockFont} onChange={(e) => header({ clockFont: e.target.value })} />
      </FormField>
      <div className="hidden sm:block" />
      {STYLE_NAMES.map((name) => {
        const current = form.header.styles?.[name];
        return (
          <div key={name} className="grid gap-3 rounded-lg border p-3 sm:col-span-2 sm:grid-cols-3">
            <p className="text-sm font-medium sm:col-span-3">{t(`designs.style.${name}` as TextKey)}</p>
            <ColorField
              label={t("designs.style.color")}
              value={current?.color ?? null}
              noneLabel={t("designs.style.inherit")}
              fallback={form.header.color}
              onChange={(color) => style(name, { color })}
            />
            <FormField label={t("designs.style.font")}>
              <Input placeholder={t("designs.style.inherit")} value={current?.font ?? ""} onChange={(e) => style(name, { font: e.target.value || null })} />
            </FormField>
            <FormField label={t("designs.style.bold")}>
              <OptionSelect
                value={current?.bold === true ? "yes" : current?.bold === false ? "no" : "inherit"}
                onValueChange={(v) => style(name, { bold: v === "inherit" ? null : v === "yes" })}
                options={[
                  { value: "inherit", label: t("designs.style.inherit") },
                  { value: "yes", label: t("face.bold.yes") },
                  { value: "no", label: t("face.bold.no") },
                ]}
                className="w-full"
              />
            </FormField>
          </div>
        );
      })}
    </Section>
  );
}

/** Material listovej tabule: farby listov, riadok, deliaca ciara, neznamy list a pismo. */
export function FlapsSection({ form, change }: { form: BoardDesign; change: Change }) {
  const flaps = form.flaps ?? DEFAULT_FLAPS;
  const set = (patch: Partial<typeof flaps>) => change({ flaps: { ...flaps, ...patch } });
  return (
    <Section title={t("designs.section.flaps")} description={t("designs.section.flaps.hint")}>
      {FLAP_COLOR_KEYS.map((key) => {
        const color = flaps.colors[key] ?? DEFAULT_FLAPS.colors[key];
        const setColor = (patch: Partial<typeof color>) => set({ colors: { ...flaps.colors, [key]: { ...color, ...patch } } });
        return (
          <div key={key} className="grid gap-3 rounded-lg border p-3 sm:col-span-2 sm:grid-cols-3">
            <p className="text-sm font-medium sm:col-span-3">{t(`designs.flaps.color.${key}`)}</p>
            <ColorField label={t("designs.flaps.top")} value={color.top} onChange={(top) => setColor({ top: top ?? color.top })} />
            <ColorField label={t("designs.flaps.bottom")} value={color.bottom} onChange={(bottom) => setColor({ bottom: bottom ?? color.bottom })} />
            <ColorField label={t("designs.flaps.text")} value={color.text} onChange={(text) => setColor({ text: text ?? color.text })} />
          </div>
        );
      })}
      <ColorField label={t("designs.flaps.row")} value={flaps.row} onChange={(row) => set({ row: row ?? flaps.row })} />
      <ColorField label={t("designs.flaps.split")} value={flaps.split} onChange={(split) => set({ split: split ?? flaps.split })} />
      <ColorField label={t("designs.flaps.unknown")} value={flaps.unknown} onChange={(unknown) => set({ unknown: unknown ?? flaps.unknown })} />
      <FormField label={t("designs.flaps.font")} hint={t("designs.font.hint")}>
        <Input value={flaps.font} onChange={(e) => set({ font: e.target.value })} />
      </FormField>
    </Section>
  );
}
