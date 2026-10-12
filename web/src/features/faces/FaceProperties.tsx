// Panel vlastnosti, ked nie je vybrany ziadny prvok: nazov a kluc predlohy, miesto okolo bodov a medzery riadkov.
import { Info } from "lucide-react";
import type { BoardFaceDto } from "@/api";
import { FormField } from "@/components/common/form-field";
import { StatusBadge } from "@/components/common/status-badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";
import { PanelSection } from "./fields";

const int = (value: string) => Number.parseInt(value, 10) || 0;

interface Props {
  face: BoardFaceDto;
  readOnly: boolean;
  onChange: (patch: Partial<BoardFaceDto>) => void;
}

export function FaceProperties({ face, readOnly, onChange }: Props) {
  return (
    <div className="flex flex-col gap-3">
      <p className="text-xs text-muted-foreground">{t("faces.hint")}</p>
      {face.builtIn && (
        <Alert className="border-amber-500/50 bg-amber-500/10 text-amber-800 dark:text-amber-300">
          <Info />
          <AlertDescription className="text-current">{t("faces.builtIn.hint")}</AlertDescription>
        </Alert>
      )}
      <PanelSection title={t("faces.section.face")}>
        <FormField label={t("faces.name")} className="col-span-2">
          <Input value={face.name} disabled={readOnly} onChange={(e) => onChange({ name: e.target.value })} />
        </FormField>
        <div className="col-span-2 flex flex-wrap items-center gap-2 text-sm">
          <span className="text-muted-foreground">{t("faces.id")}</span>
          <code className="font-mono">{face.id}</code>
          <StatusBadge tone={face.builtIn ? "off" : "ok"}>{t(face.builtIn ? "settings.designs.builtIn" : "settings.designs.custom")}</StatusBadge>
        </div>
      </PanelSection>

      <PanelSection title={t("faces.section.layout")}>
        <p className="col-span-2 text-xs text-muted-foreground">{t("faces.section.layout.hint")}</p>
        {(["left", "top", "right", "bottom"] as const).map((side) => (
          <FormField key={side} label={t(`faces.margin.${side}`)}>
            <Input
              type="number"
              min={0}
              value={face.margin[side]}
              disabled={readOnly}
              onChange={(e) => onChange({ margin: { ...face.margin, [side]: Math.max(0, int(e.target.value)) } })}
            />
          </FormField>
        ))}
        <FormField label={t("faces.rowGap")} hint={t("faces.rowGap.hint")} className="col-span-2">
          <Input
            type="number"
            min={0}
            placeholder={t("faces.rowGap.design")}
            value={face.rowGap ?? ""}
            disabled={readOnly}
            onChange={(e) => onChange({ rowGap: e.target.value === "" ? null : Math.max(0, int(e.target.value)) })}
          />
        </FormField>
        <FormField label={t("faces.rowGroup")}>
          <Input type="number" min={0} value={face.rowGroup} disabled={readOnly} onChange={(e) => onChange({ rowGroup: Math.max(0, int(e.target.value)) })} />
        </FormField>
        <FormField label={t("faces.groupGap")}>
          <Input type="number" min={0} value={face.groupGap} disabled={readOnly} onChange={(e) => onChange({ groupGap: Math.max(0, int(e.target.value)) })} />
        </FormField>
        <FormField label={t("faces.moduleWidth")} hint={t("faces.moduleWidth.hint")} className="col-span-2">
          <Input
            type="number"
            min={0}
            value={face.moduleWidth}
            disabled={readOnly}
            onChange={(e) => onChange({ moduleWidth: Math.max(0, int(e.target.value)) })}
          />
        </FormField>
      </PanelSection>
    </div>
  );
}
