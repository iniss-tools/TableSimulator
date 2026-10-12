// Panel vlastnosti vybranych prvkov cela (ako vo Figme): zarovnanie, ukotvenie a poloha, podla druhu rozmer a vypln
// plochy, text, hodiny, symbol alebo obrazok; zamok, poradie kreslenia a odstranenie. Pri viacerych prvkoch pole s roznymi
// hodnotami ukaze "Rozne" a zmena plati pre vsetky; casti podla druhu len ked su vsetky prvky rovnakeho druhu.
import { Fragment, useRef, type ReactNode } from "react";
import {
  AlignCenterHorizontal,
  AlignCenterVertical,
  AlignEndHorizontal,
  AlignEndVertical,
  AlignHorizontalSpaceBetween,
  AlignStartHorizontal,
  AlignStartVertical,
  AlignVerticalSpaceBetween,
  ArrowDownToLine,
  ArrowUpToLine,
  ImageUp,
  Layers,
  Lock,
  LockOpen,
  Trash2,
  X,
  type LucideIcon,
} from "lucide-react";
import { toast } from "sonner";
import type { FaceAlign, FaceAnchor, FaceClockFormat, FaceElementDto, FaceVAlign } from "@/api";
import { FormField } from "@/components/common/form-field";
import { OptionSelect, type Option } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { FACE_SYMBOLS } from "@/render/face";
import { t, type TextKey } from "@/i18n";
import { ALIGN_OPS, canAlign, type AlignOp } from "./align";
import { elementSummary, IMAGE_TYPES, KIND_ICONS, readImage } from "./faces";
import { ColorField, NumberField, PanelSection } from "./fields";

const ANCHORS: FaceAnchor[] = ["face", "matrix", "row", "top", "bottom", "left", "right"];
const ALIGNS: FaceAlign[] = ["left", "center", "right"];
const VALIGNS: FaceVAlign[] = ["top", "middle", "bottom"];
const FORMATS: FaceClockFormat[] = ["H:mm", "HH:mm", "HH:mm:ss"];
/** Pomenovane styly textov zo vzhladu (prazdny = ostatne texty cela). */
const STYLES = ["", "title", "label", "number"] as const;

const ALIGN_ICONS: Record<AlignOp, LucideIcon> = {
  left: AlignStartVertical,
  hcenter: AlignCenterVertical,
  right: AlignEndVertical,
  top: AlignStartHorizontal,
  vcenter: AlignCenterHorizontal,
  bottom: AlignEndHorizontal,
  hdistribute: AlignHorizontalSpaceBetween,
  vdistribute: AlignVerticalSpaceBetween,
};

/** Spolocna hodnota vybranych prvkov; undefined = rozne. */
function common<K extends keyof FaceElementDto>(elements: FaceElementDto[], key: K): FaceElementDto[K] | undefined {
  const first = elements[0][key];
  return elements.every((e) => e[key] === first) ? first : undefined;
}

/** Vyber jednej z moznosti; pri roznych hodnotach prazdny s popisom "Rozne". */
function SelectField<T extends string>({
  label,
  value,
  options,
  onChange,
  disabled,
  className,
}: {
  label: string;
  value: T | undefined;
  options: Option<T>[];
  onChange: (value: T) => void;
  disabled: boolean;
  className?: string;
}) {
  return (
    <FormField label={label} className={className}>
      <OptionSelect value={value ?? null} placeholder={t("faces.mixed")} disabled={disabled} onValueChange={onChange} options={options} className="w-full" />
    </FormField>
  );
}

interface Props {
  /** Vybrane prvky (aspon jeden). */
  elements: FaceElementDto[];
  readOnly: boolean;
  /** Vyber sa da posunut dopredu (kresli sa neskor) / dozadu. */
  canForward: boolean;
  canBackward: boolean;
  /** Zmena plati pre vsetky vybrane prvky. */
  onPatch: (patch: Partial<FaceElementDto>) => void;
  onForward: () => void;
  onBackward: () => void;
  onRemove: () => void;
  onAlign: (op: AlignOp) => void;
}

export function ElementProperties({ elements, readOnly, canForward, canBackward, onPatch, onForward, onBackward, onRemove, onAlign }: Props) {
  const set = onPatch;
  const single = elements.length === 1 ? elements[0] : null;
  const kind = common(elements, "kind");
  const anchor = common(elements, "anchor");
  const locked = elements.every((e) => e.locked);
  const image = common(elements, "image");
  const text = common(elements, "text");
  const bold = common(elements, "bold");
  const Icon = kind ? KIND_ICONS[kind] : Layers;
  const file = useRef<HTMLInputElement>(null);
  const icon = (title: string, onClick: () => void, children: ReactNode, disabled = false) => (
    <Button type="button" variant="ghost" size="icon-sm" title={title} aria-label={title} disabled={readOnly || disabled} onClick={onClick}>
      {children}
    </Button>
  );
  const color = (key: "color" | "fill", label: string, emptyLabel: string) => (
    <ColorField
      label={label}
      value={common(elements, key)}
      emptyLabel={emptyLabel}
      disabled={readOnly}
      onChange={(value) => set(key === "color" ? { color: value } : { fill: value })}
    />
  );

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <Icon className="size-4 shrink-0 text-muted-foreground" />
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium">
            {single ? t(`face.kind.${single.kind}`) : t(elements.length < 5 ? "faces.selection.few" : "faces.selection.many", { count: elements.length })}
          </p>
          <p className="truncate text-xs text-muted-foreground">
            {single ? elementSummary(single) : kind ? t(`face.kind.${kind}`) : t("faces.selection.kinds")}
          </p>
        </div>
        {icon(t(locked ? "faces.element.unlock" : "faces.element.lock"), () => set({ locked: !locked }), locked ? <Lock /> : <LockOpen />)}
        {icon(t("faces.element.forward"), onForward, <ArrowUpToLine />, !canForward)}
        {icon(t("faces.element.backward"), onBackward, <ArrowDownToLine />, !canBackward)}
        {icon(t("common.delete"), onRemove, <Trash2 />)}
      </div>

      <PanelSection title={t("faces.section.align")}>
        <div className="col-span-2 flex flex-wrap gap-0.5" role="toolbar" aria-label={t("faces.section.align")}>
          {ALIGN_OPS.map((op) => {
            const AlignIcon = ALIGN_ICONS[op];
            return <Fragment key={op}>{icon(t(`faces.align.${op}`), () => onAlign(op), <AlignIcon />, !canAlign(op, elements.length))}</Fragment>;
          })}
        </div>
        <p className="col-span-2 text-xs text-muted-foreground">{t(single ? "faces.align.hint.single" : "faces.align.hint.many")}</p>
      </PanelSection>

      <PanelSection title={t("faces.section.position")}>
        <SelectField
          label={t("face.anchor")}
          value={anchor}
          disabled={readOnly}
          onChange={(value) => set({ anchor: value })}
          options={ANCHORS.map((a) => ({ value: a, label: t(`face.anchor.${a}`) }))}
          className={anchor === "row" ? undefined : "col-span-2"}
        />
        {anchor === "row" && (
          <NumberField
            label={t("face.row")}
            value={common(elements, "row")}
            step={1}
            min={1}
            disabled={readOnly}
            onChange={(row) => set({ row: Math.max(1, Math.round(row)) })}
          />
        )}
        <SelectField
          label={t("face.align")}
          value={common(elements, "align")}
          disabled={readOnly}
          onChange={(align) => set({ align })}
          options={ALIGNS.map((a) => ({ value: a, label: t(`face.align.${a}`) }))}
        />
        <SelectField
          label={t("face.valign")}
          value={common(elements, "vAlign")}
          disabled={readOnly}
          onChange={(vAlign) => set({ vAlign })}
          options={VALIGNS.map((a) => ({ value: a, label: t(`face.valign.${a}`) }))}
        />
        <NumberField label="X" value={common(elements, "x")} disabled={readOnly} onChange={(x) => set({ x })} />
        <NumberField label="Y" value={common(elements, "y")} disabled={readOnly} onChange={(y) => set({ y })} />
      </PanelSection>

      {(kind === "rect" || kind === "image") && (
        <PanelSection title={t("faces.section.size")}>
          <NumberField label={t("face.width")} value={common(elements, "width")} disabled={readOnly} onChange={(width) => set({ width })} />
          <NumberField label={t("face.height")} value={common(elements, "height")} disabled={readOnly} onChange={(height) => set({ height })} />
          <p className="col-span-2 text-xs text-muted-foreground">{t("face.size.hint")}</p>
        </PanelSection>
      )}

      {kind === "rect" && (
        <PanelSection title={t("faces.section.fill")}>
          {color("fill", t("face.fill"), t("face.color.none"))}
          <NumberField label={t("face.border")} value={common(elements, "border")} min={0} disabled={readOnly} onChange={(border) => set({ border })} />
          <NumberField label={t("face.radius")} value={common(elements, "radius")} min={0} disabled={readOnly} onChange={(radius) => set({ radius })} />
          {color("color", t("face.borderColor"), t("face.color.default"))}
        </PanelSection>
      )}

      {kind === "text" && (
        <>
          <PanelSection title={t("face.text")}>
            <FormField label={t("face.text")} hint={t("face.text.hint")} className="col-span-2">
              <Textarea
                rows={3}
                value={text ?? ""}
                placeholder={text === undefined ? t("faces.mixed") : undefined}
                disabled={readOnly}
                onChange={(e) => set({ text: e.target.value })}
              />
            </FormField>
          </PanelSection>
          <PanelSection title={t("faces.section.font")}>
            <NumberField label={t("face.size")} value={common(elements, "size")} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
            <NumberField label={t("face.size2")} value={common(elements, "size2")} min={0} disabled={readOnly} onChange={(size2) => set({ size2 })} />
            <NumberField
              label={t("face.maxWidth")}
              value={common(elements, "maxWidth")}
              min={0}
              disabled={readOnly}
              onChange={(maxWidth) => set({ maxWidth })}
            />
            <NumberField label={t("face.spacing")} value={common(elements, "spacing")} disabled={readOnly} onChange={(spacing) => set({ spacing })} />
            <SelectField
              label={t("face.style")}
              value={common(elements, "style")}
              disabled={readOnly}
              onChange={(style) => set({ style })}
              options={STYLES.map((style) => ({ value: style, label: t(`face.style.${style || "default"}`) }))}
            />
            <SelectField
              label={t("face.bold")}
              value={bold === undefined ? undefined : bold === null ? "default" : bold ? "yes" : "no"}
              disabled={readOnly}
              onChange={(v) => set({ bold: v === "default" ? null : v === "yes" })}
              options={[
                { value: "default", label: t("face.bold.default") },
                { value: "yes", label: t("face.bold.yes") },
                { value: "no", label: t("face.bold.no") },
              ]}
            />
            {color("color", t("face.color"), t("face.color.default"))}
          </PanelSection>
        </>
      )}

      {kind === "clock" && (
        <PanelSection title={t("face.kind.clock")}>
          <NumberField label={t("face.size")} value={common(elements, "size")} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
          <SelectField
            label={t("face.format")}
            value={common(elements, "format")}
            disabled={readOnly}
            onChange={(format) => set({ format })}
            options={FORMATS.map((f) => ({ value: f, label: f }))}
          />
          <SelectField
            label={t("face.render")}
            value={common(elements, "render")}
            disabled={readOnly}
            onChange={(render) => set({ render })}
            options={(["text", "display"] as const).map((r) => ({ value: r, label: t(`face.render.${r}`) }))}
            className="col-span-2"
          />
          {color("color", t("face.clockColor"), t("face.color.default"))}
          {color("fill", t("face.clockFill"), t("face.color.default"))}
        </PanelSection>
      )}

      {kind === "symbol" && (
        <PanelSection title={t("face.kind.symbol")}>
          <SelectField
            label={t("face.symbol")}
            value={common(elements, "symbol")}
            disabled={readOnly}
            onChange={(symbol) => set({ symbol })}
            options={FACE_SYMBOLS.map((s) => ({ value: s, label: t(`face.symbol.${s}`) }))}
            className="col-span-2"
          />
          <NumberField label={t("face.symbolSize")} value={common(elements, "size")} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
          {color("color", t("face.color"), t("face.color.default"))}
        </PanelSection>
      )}

      {kind === "image" && (
        <PanelSection title={t("face.image")}>
          <div className="col-span-2 flex flex-col gap-2">
            {image ? (
              <img src={image} alt="" className="h-16 w-full rounded border bg-muted object-contain p-1" />
            ) : (
              <p className="text-sm text-muted-foreground">{image === undefined ? t("faces.mixed") : t("face.image.empty")}</p>
            )}
            {!readOnly && (
              <div className="flex flex-wrap gap-2">
                <input
                  ref={file}
                  type="file"
                  accept={IMAGE_TYPES.join(",")}
                  className="hidden"
                  aria-label={t("face.image.choose")}
                  onChange={(e) => {
                    const chosen = e.target.files?.[0];
                    e.target.value = "";
                    if (!chosen) return;
                    readImage(chosen).then(
                      (data) => set({ image: data }),
                      (error: Error) => toast.error(t(error.message as TextKey)),
                    );
                  }}
                />
                <Button type="button" variant="outline" size="sm" onClick={() => file.current?.click()}>
                  <ImageUp />
                  {t("face.image.choose")}
                </Button>
                {image !== "" && (
                  <Button type="button" variant="ghost" size="sm" onClick={() => set({ image: "" })}>
                    <X />
                    {t("face.image.remove")}
                  </Button>
                )}
              </div>
            )}
            <p className="text-xs text-muted-foreground">{t("face.image.hint")}</p>
          </div>
        </PanelSection>
      )}
    </div>
  );
}
