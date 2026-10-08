// Nova virtualna tabula alebo uprava - co z XML tabule INISSu ukazat (ako definicia v INISSView).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { errorText, setVirtualBoardMutation, type VirtualBoardDto, type VirtualLayoutDto, type VirtualSourceDto } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";
import { layoutName } from "./layouts";

interface VirtualDialogProps {
  initial: VirtualBoardDto;
  isNew: boolean;
  sources: VirtualSourceDto[];
  layouts: VirtualLayoutDto[];
  defaultLayout: string;
  onClose: () => void;
}

export function VirtualDialog({ initial, isNew, sources, layouts, defaultLayout, onClose }: VirtualDialogProps) {
  const fallback = layouts.find((l) => l.id === defaultLayout);
  const [board, setBoard] = useState(initial);
  const number = (value: string) => Number.parseInt(value, 10);
  const save = useMutation({ ...setVirtualBoardMutation(), onSuccess: onClose });

  return (
    <FormDialog
      title={isNew ? t("dialog.virtual.new") : t("dialog.virtual.edit", { id: board.id })}
      onClose={onClose}
      onSubmit={() => save.mutate({ path: { id: board.id }, body: board })}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="lg"
    >
      <FieldGroup>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={t("virtual.field.id")} hint={t("virtual.field.id.hint")}>
            <Input
              type="number"
              min={1}
              max={99999}
              value={board.id}
              disabled={!isNew}
              onChange={(e) => setBoard({ ...board, id: number(e.target.value) || 1 })}
            />
          </FormField>
          <FormField label={t("virtual.field.source")} hint={t("virtual.field.source.hint")}>
            <Input list="virtual-sources" className="font-mono" value={board.source} onChange={(e) => setBoard({ ...board, source: e.target.value })} />
          </FormField>
        </div>
        <datalist id="virtual-sources">
          {sources.map((s) => (
            <option key={s.name} value={s.name}>
              {[s.station, s.key].filter(Boolean).join(" – ")}
            </option>
          ))}
        </datalist>
        <FormField label={t("virtual.field.name")}>
          <Input value={board.name} onChange={(e) => setBoard({ ...board, name: e.target.value })} />
        </FormField>
        <FormField label={t("virtual.field.description")}>
          <Input value={board.description} onChange={(e) => setBoard({ ...board, description: e.target.value })} />
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={t("virtual.field.rowCount")} hint={t("virtual.field.rowCount.hint")}>
            <Input
              type="number"
              min={1}
              max={999}
              value={board.rowCount ?? ""}
              onChange={(e) => setBoard({ ...board, rowCount: e.target.value === "" ? null : number(e.target.value) || 1 })}
            />
          </FormField>
          <FormField label={t("virtual.field.rowStart")} hint={t("virtual.field.rowStart.hint")}>
            <Input type="number" min={1} max={999} value={board.rowStart} onChange={(e) => setBoard({ ...board, rowStart: number(e.target.value) || 1 })} />
          </FormField>
        </div>
        <FormField label={t("virtual.field.layout")}>
          <OptionSelect
            value={board.layout}
            onValueChange={(layout) => setBoard({ ...board, layout })}
            options={[
              { value: "", label: t("virtual.layout.default", { name: fallback ? layoutName(fallback) : defaultLayout }) },
              ...layouts.map((l) => ({ value: l.id, label: layoutName(l) })),
            ]}
            className="w-full"
          />
        </FormField>
        <FormField label={t("virtual.field.separator")} hint={t("virtual.field.separator.hint")}>
          <Input value={board.separator} placeholder=", " onChange={(e) => setBoard({ ...board, separator: e.target.value })} />
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={t("virtual.field.template")} hint={t("virtual.field.template.hint")}>
            <Input value={board.template} onChange={(e) => setBoard({ ...board, template: e.target.value })} />
          </FormField>
          <FormField label={t("virtual.field.parameter")} hint={t("virtual.field.parameter.hint")}>
            <Input value={board.parameter} onChange={(e) => setBoard({ ...board, parameter: e.target.value })} />
          </FormField>
        </div>
      </FieldGroup>
    </FormDialog>
  );
}
