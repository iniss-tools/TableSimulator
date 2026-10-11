// Jeden prvok cela v editore: druh, ukotvenie a poloha, podla druhu rozmer a vypln plochy, text alebo hodiny.
import type { ReactNode } from "react";
import { ArrowDown, ArrowUp, Clock, Square, Trash2, Type } from "lucide-react";
import type { FaceAlign, FaceAnchor, FaceClockFormat, FaceElementDto, FaceVAlign } from "@/api";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { t, type TextKey } from "@/i18n";
import { FACE_COLOR_TOKENS } from "./faces";

const ANCHORS: FaceAnchor[] = ["face", "matrix", "row", "top", "bottom", "left", "right"];
const ALIGNS: FaceAlign[] = ["left", "center", "right"];
const VALIGNS: FaceVAlign[] = ["top", "middle", "bottom"];
const FORMATS: FaceClockFormat[] = ["H:mm", "HH:mm", "HH:mm:ss"];
/** Pomenovane styly textov zo vzhladu (prazdny = ostatne texty cela). */
const STYLES = ["", "title", "label", "number"] as const;
const HEX = /^#[0-9a-fA-F]{6}$/;
/** Vlastna farba #rrggbb vo vybere farieb. */
const CUSTOM = "#";

function NumberInput({
  label,
  value,
  onChange,
  disabled,
  step = 0.5,
  min,
  hint,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
  disabled: boolean;
  step?: number;
  min?: number;
  hint?: ReactNode;
}) {
  return (
    <FormField label={label} hint={hint}>
      <Input
        type="number"
        step={step}
        min={min}
        value={value}
        disabled={disabled}
        onChange={(e) => {
          const parsed = Number.parseFloat(e.target.value);
          onChange(Number.isFinite(parsed) ? parsed : 0);
        }}
      />
    </FormField>
  );
}

/** Farba: predvolena (prazdna), nazov farby vzhladu alebo vlastna #rrggbb. */
function ColorSelect({
  label,
  value,
  emptyLabel,
  onChange,
  disabled,
}: {
  label: string;
  value: string;
  emptyLabel: string;
  onChange: (value: string) => void;
  disabled: boolean;
}) {
  const custom = value.startsWith("#");
  return (
    <FormField label={label}>
      <div className="flex items-center gap-2">
        <OptionSelect
          value={custom ? CUSTOM : value}
          disabled={disabled}
          onValueChange={(v) => onChange(v === CUSTOM ? "#ffffff" : v)}
          options={[
            { value: "", label: emptyLabel },
            ...FACE_COLOR_TOKENS.map((token) => ({ value: token, label: t(`face.color.${token}` as TextKey) })),
            { value: CUSTOM, label: t("face.color.custom") },
          ]}
          className="min-w-0 flex-1"
        />
        {custom && (
          <input
            type="color"
            aria-label={label}
            className="h-8 w-10 shrink-0 cursor-pointer rounded-md border bg-transparent p-0.5"
            value={HEX.test(value) ? value : "#ffffff"}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value)}
          />
        )}
      </div>
    </FormField>
  );
}

const KIND_ICONS = { rect: Square, text: Type, clock: Clock };

interface Props {
  element: FaceElementDto;
  index: number;
  count: number;
  readOnly: boolean;
  onChange: (element: FaceElementDto) => void;
  onMove: (to: number) => void;
  onRemove: () => void;
}

export function FaceElementCard({ element, index, count, readOnly, onChange, onMove, onRemove }: Props) {
  const set = (patch: Partial<FaceElementDto>) => onChange({ ...element, ...patch });
  const Icon = KIND_ICONS[element.kind];
  const summary = element.kind === "text" ? element.text.split("\n")[0] : t(`face.anchor.${element.anchor}`);

  return (
    <Card size="sm">
      <CardHeader className="flex flex-row items-center gap-2 border-b">
        <Icon className="size-4 shrink-0 text-muted-foreground" />
        <CardTitle className="min-w-0 flex-1 truncate">
          {t(`face.kind.${element.kind}`)}
          <span className="ml-2 font-normal text-muted-foreground">{summary}</span>
        </CardTitle>
        {!readOnly && (
          <div className="flex shrink-0 gap-1">
            <Button type="button" variant="ghost" size="icon-sm" title={t("faces.element.up")} disabled={index === 0} onClick={() => onMove(index - 1)}>
              <ArrowUp />
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              title={t("faces.element.down")}
              disabled={index === count - 1}
              onClick={() => onMove(index + 1)}
            >
              <ArrowDown />
            </Button>
            <Button type="button" variant="ghost" size="icon-sm" title={t("common.delete")} onClick={onRemove}>
              <Trash2 />
            </Button>
          </div>
        )}
      </CardHeader>
      <CardContent className="grid gap-3 sm:grid-cols-4">
        {element.kind === "text" && (
          <FormField label={t("face.text")} hint={t("face.text.hint")} className="sm:col-span-4">
            <Textarea rows={2} value={element.text} disabled={readOnly} onChange={(e) => set({ text: e.target.value })} />
          </FormField>
        )}
        <FormField label={t("face.anchor")} className="sm:col-span-2">
          <OptionSelect
            value={element.anchor}
            disabled={readOnly}
            onValueChange={(anchor) => set({ anchor })}
            options={ANCHORS.map((a) => ({ value: a, label: t(`face.anchor.${a}`) }))}
            className="w-full"
          />
        </FormField>
        {element.anchor === "row" ? (
          <NumberInput
            label={t("face.row")}
            value={element.row}
            step={1}
            min={1}
            disabled={readOnly}
            onChange={(row) => set({ row: Math.max(1, Math.round(row)) })}
          />
        ) : (
          <div className="hidden sm:block" />
        )}
        <div className="hidden sm:block" />
        <FormField label={t("face.align")}>
          <OptionSelect
            value={element.align}
            disabled={readOnly}
            onValueChange={(align) => set({ align })}
            options={ALIGNS.map((a) => ({ value: a, label: t(`face.align.${a}`) }))}
            className="w-full"
          />
        </FormField>
        <FormField label={t("face.valign")}>
          <OptionSelect
            value={element.vAlign}
            disabled={readOnly}
            onValueChange={(vAlign) => set({ vAlign })}
            options={VALIGNS.map((a) => ({ value: a, label: t(`face.valign.${a}`) }))}
            className="w-full"
          />
        </FormField>
        <NumberInput label="X" value={element.x} disabled={readOnly} onChange={(x) => set({ x })} />
        <NumberInput label="Y" value={element.y} disabled={readOnly} onChange={(y) => set({ y })} />

        {element.kind === "rect" && (
          <>
            <NumberInput label={t("face.width")} value={element.width} disabled={readOnly} onChange={(width) => set({ width })} />
            <NumberInput label={t("face.height")} value={element.height} disabled={readOnly} onChange={(height) => set({ height })} />
            <NumberInput label={t("face.border")} value={element.border} min={0} disabled={readOnly} onChange={(border) => set({ border })} />
            <NumberInput label={t("face.radius")} value={element.radius} min={0} disabled={readOnly} onChange={(radius) => set({ radius })} />
            <div className="sm:col-span-2">
              <ColorSelect
                label={t("face.fill")}
                value={element.fill}
                emptyLabel={t("face.color.none")}
                disabled={readOnly}
                onChange={(fill) => set({ fill })}
              />
            </div>
            <div className="sm:col-span-2">
              <ColorSelect
                label={t("face.borderColor")}
                value={element.color}
                emptyLabel={t("face.color.default")}
                disabled={readOnly}
                onChange={(color) => set({ color })}
              />
            </div>
            <p className="text-sm text-muted-foreground sm:col-span-4">{t("face.size.hint")}</p>
          </>
        )}

        {element.kind === "text" && (
          <>
            <NumberInput label={t("face.size")} value={element.size} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
            <NumberInput label={t("face.size2")} value={element.size2} min={0} disabled={readOnly} onChange={(size2) => set({ size2 })} />
            <NumberInput label={t("face.maxWidth")} value={element.maxWidth} min={0} disabled={readOnly} onChange={(maxWidth) => set({ maxWidth })} />
            <NumberInput label={t("face.spacing")} value={element.spacing} disabled={readOnly} onChange={(spacing) => set({ spacing })} />
            <FormField label={t("face.style")} className="sm:col-span-2">
              <OptionSelect
                value={element.style}
                disabled={readOnly}
                onValueChange={(style) => set({ style })}
                options={STYLES.map((style) => ({ value: style, label: t(`face.style.${style || "default"}`) }))}
                className="w-full"
              />
            </FormField>
            <FormField label={t("face.bold")} className="sm:col-span-2">
              <OptionSelect
                value={element.bold === null ? "default" : element.bold ? "yes" : "no"}
                disabled={readOnly}
                onValueChange={(v) => set({ bold: v === "default" ? null : v === "yes" })}
                options={[
                  { value: "default", label: t("face.bold.default") },
                  { value: "yes", label: t("face.bold.yes") },
                  { value: "no", label: t("face.bold.no") },
                ]}
                className="w-full"
              />
            </FormField>
            <div className="sm:col-span-2">
              <ColorSelect
                label={t("face.color")}
                value={element.color}
                emptyLabel={t("face.color.default")}
                disabled={readOnly}
                onChange={(color) => set({ color })}
              />
            </div>
          </>
        )}

        {element.kind === "clock" && (
          <>
            <NumberInput label={t("face.size")} value={element.size} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
            <FormField label={t("face.format")}>
              <OptionSelect
                value={element.format}
                disabled={readOnly}
                onValueChange={(format) => set({ format })}
                options={FORMATS.map((f) => ({ value: f, label: f }))}
                className="w-full"
              />
            </FormField>
            <FormField label={t("face.render")} className="sm:col-span-2">
              <OptionSelect
                value={element.render}
                disabled={readOnly}
                onValueChange={(render) => set({ render })}
                options={(["text", "display"] as const).map((r) => ({ value: r, label: t(`face.render.${r}`) }))}
                className="w-full"
              />
            </FormField>
            <div className="sm:col-span-2">
              <ColorSelect
                label={t("face.clockColor")}
                value={element.color}
                emptyLabel={t("face.color.default")}
                disabled={readOnly}
                onChange={(color) => set({ color })}
              />
            </div>
            <div className="sm:col-span-2">
              <ColorSelect
                label={t("face.clockFill")}
                value={element.fill}
                emptyLabel={t("face.color.default")}
                disabled={readOnly}
                onChange={(fill) => set({ fill })}
              />
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
