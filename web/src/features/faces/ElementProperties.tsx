// Panel vlastnosti vybraneho prvku cela (ako vo Figme): ukotvenie a poloha, podla druhu rozmer a vypln plochy, text,
// hodiny, symbol alebo obrazok; zamok, poradie kreslenia a odstranenie.
import { useRef } from "react";
import { ArrowDownToLine, ArrowUpToLine, ImageUp, Lock, LockOpen, Trash2, X } from "lucide-react";
import { toast } from "sonner";
import type { FaceAlign, FaceAnchor, FaceClockFormat, FaceElementDto, FaceVAlign } from "@/api";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { FACE_SYMBOLS } from "@/render/face";
import { t, type TextKey } from "@/i18n";
import { elementSummary, IMAGE_TYPES, KIND_ICONS, readImage } from "./faces";
import { ColorField, NumberField, PanelSection } from "./fields";

const ANCHORS: FaceAnchor[] = ["face", "matrix", "row", "top", "bottom", "left", "right"];
const ALIGNS: FaceAlign[] = ["left", "center", "right"];
const VALIGNS: FaceVAlign[] = ["top", "middle", "bottom"];
const FORMATS: FaceClockFormat[] = ["H:mm", "HH:mm", "HH:mm:ss"];
/** Pomenovane styly textov zo vzhladu (prazdny = ostatne texty cela). */
const STYLES = ["", "title", "label", "number"] as const;

interface Props {
  element: FaceElementDto;
  readOnly: boolean;
  /** Prvok sa da posunut dopredu (kresli sa neskor) / dozadu. */
  canForward: boolean;
  canBackward: boolean;
  onChange: (element: FaceElementDto) => void;
  onForward: () => void;
  onBackward: () => void;
  onRemove: () => void;
}

export function ElementProperties({ element, readOnly, canForward, canBackward, onChange, onForward, onBackward, onRemove }: Props) {
  const set = (patch: Partial<FaceElementDto>) => onChange({ ...element, ...patch });
  const Icon = KIND_ICONS[element.kind];
  const file = useRef<HTMLInputElement>(null);
  const icon = (title: string, onClick: () => void, children: React.ReactNode, disabled = false) => (
    <Button type="button" variant="ghost" size="icon-sm" title={title} aria-label={title} disabled={readOnly || disabled} onClick={onClick}>
      {children}
    </Button>
  );

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <Icon className="size-4 shrink-0 text-muted-foreground" />
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium">{t(`face.kind.${element.kind}`)}</p>
          <p className="truncate text-xs text-muted-foreground">{elementSummary(element)}</p>
        </div>
        {icon(
          t(element.locked ? "faces.element.unlock" : "faces.element.lock"),
          () => set({ locked: !element.locked }),
          element.locked ? <Lock /> : <LockOpen />,
        )}
        {icon(t("faces.element.forward"), onForward, <ArrowUpToLine />, !canForward)}
        {icon(t("faces.element.backward"), onBackward, <ArrowDownToLine />, !canBackward)}
        {icon(t("common.delete"), onRemove, <Trash2 />)}
      </div>

      <PanelSection title={t("faces.section.position")}>
        <FormField label={t("face.anchor")} className={element.anchor === "row" ? undefined : "col-span-2"}>
          <OptionSelect
            value={element.anchor}
            disabled={readOnly}
            onValueChange={(anchor) => set({ anchor })}
            options={ANCHORS.map((a) => ({ value: a, label: t(`face.anchor.${a}`) }))}
            className="w-full"
          />
        </FormField>
        {element.anchor === "row" && (
          <NumberField
            label={t("face.row")}
            value={element.row}
            step={1}
            min={1}
            disabled={readOnly}
            onChange={(row) => set({ row: Math.max(1, Math.round(row)) })}
          />
        )}
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
        <NumberField label="X" value={element.x} disabled={readOnly} onChange={(x) => set({ x })} />
        <NumberField label="Y" value={element.y} disabled={readOnly} onChange={(y) => set({ y })} />
      </PanelSection>

      {(element.kind === "rect" || element.kind === "image") && (
        <PanelSection title={t("faces.section.size")}>
          <NumberField label={t("face.width")} value={element.width} disabled={readOnly} onChange={(width) => set({ width })} />
          <NumberField label={t("face.height")} value={element.height} disabled={readOnly} onChange={(height) => set({ height })} />
          <p className="col-span-2 text-xs text-muted-foreground">{t("face.size.hint")}</p>
        </PanelSection>
      )}

      {element.kind === "rect" && (
        <PanelSection title={t("faces.section.fill")}>
          <ColorField label={t("face.fill")} value={element.fill} emptyLabel={t("face.color.none")} disabled={readOnly} onChange={(fill) => set({ fill })} />
          <NumberField label={t("face.border")} value={element.border} min={0} disabled={readOnly} onChange={(border) => set({ border })} />
          <NumberField label={t("face.radius")} value={element.radius} min={0} disabled={readOnly} onChange={(radius) => set({ radius })} />
          <ColorField
            label={t("face.borderColor")}
            value={element.color}
            emptyLabel={t("face.color.default")}
            disabled={readOnly}
            onChange={(color) => set({ color })}
          />
        </PanelSection>
      )}

      {element.kind === "text" && (
        <>
          <PanelSection title={t("face.text")}>
            <FormField label={t("face.text")} hint={t("face.text.hint")} className="col-span-2">
              <Textarea rows={3} value={element.text} disabled={readOnly} onChange={(e) => set({ text: e.target.value })} />
            </FormField>
          </PanelSection>
          <PanelSection title={t("faces.section.font")}>
            <NumberField label={t("face.size")} value={element.size} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
            <NumberField label={t("face.size2")} value={element.size2} min={0} disabled={readOnly} onChange={(size2) => set({ size2 })} />
            <NumberField label={t("face.maxWidth")} value={element.maxWidth} min={0} disabled={readOnly} onChange={(maxWidth) => set({ maxWidth })} />
            <NumberField label={t("face.spacing")} value={element.spacing} disabled={readOnly} onChange={(spacing) => set({ spacing })} />
            <FormField label={t("face.style")}>
              <OptionSelect
                value={element.style}
                disabled={readOnly}
                onValueChange={(style) => set({ style })}
                options={STYLES.map((style) => ({ value: style, label: t(`face.style.${style || "default"}`) }))}
                className="w-full"
              />
            </FormField>
            <FormField label={t("face.bold")}>
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
            <ColorField
              label={t("face.color")}
              value={element.color}
              emptyLabel={t("face.color.default")}
              disabled={readOnly}
              onChange={(color) => set({ color })}
            />
          </PanelSection>
        </>
      )}

      {element.kind === "clock" && (
        <PanelSection title={t("face.kind.clock")}>
          <NumberField label={t("face.size")} value={element.size} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
          <FormField label={t("face.format")}>
            <OptionSelect
              value={element.format}
              disabled={readOnly}
              onValueChange={(format) => set({ format })}
              options={FORMATS.map((f) => ({ value: f, label: f }))}
              className="w-full"
            />
          </FormField>
          <FormField label={t("face.render")} className="col-span-2">
            <OptionSelect
              value={element.render}
              disabled={readOnly}
              onValueChange={(render) => set({ render })}
              options={(["text", "display"] as const).map((r) => ({ value: r, label: t(`face.render.${r}`) }))}
              className="w-full"
            />
          </FormField>
          <ColorField
            label={t("face.clockColor")}
            value={element.color}
            emptyLabel={t("face.color.default")}
            disabled={readOnly}
            onChange={(color) => set({ color })}
          />
          <ColorField
            label={t("face.clockFill")}
            value={element.fill}
            emptyLabel={t("face.color.default")}
            disabled={readOnly}
            onChange={(fill) => set({ fill })}
          />
        </PanelSection>
      )}

      {element.kind === "symbol" && (
        <PanelSection title={t("face.kind.symbol")}>
          <FormField label={t("face.symbol")} className="col-span-2">
            <OptionSelect
              value={element.symbol}
              disabled={readOnly}
              onValueChange={(symbol) => set({ symbol })}
              options={FACE_SYMBOLS.map((s) => ({ value: s, label: t(`face.symbol.${s}`) }))}
              className="w-full"
            />
          </FormField>
          <NumberField label={t("face.symbolSize")} value={element.size} min={1} disabled={readOnly} onChange={(size) => set({ size })} />
          <ColorField
            label={t("face.color")}
            value={element.color}
            emptyLabel={t("face.color.default")}
            disabled={readOnly}
            onChange={(color) => set({ color })}
          />
        </PanelSection>
      )}

      {element.kind === "image" && (
        <PanelSection title={t("face.image")}>
          <div className="col-span-2 flex flex-col gap-2">
            {element.image ? (
              <img src={element.image} alt="" className="h-16 w-full rounded border bg-muted object-contain p-1" />
            ) : (
              <p className="text-sm text-muted-foreground">{t("face.image.empty")}</p>
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
                      (image) => set({ image }),
                      (error: Error) => toast.error(t(error.message as TextKey)),
                    );
                  }}
                />
                <Button type="button" variant="outline" size="sm" onClick={() => file.current?.click()}>
                  <ImageUp />
                  {t("face.image.choose")}
                </Button>
                {element.image && (
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
