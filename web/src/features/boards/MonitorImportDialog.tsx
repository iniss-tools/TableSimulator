// Import projektu ELEN Monitora: simulatory projektu sa stanu tabulami na zvolenej linke (len linky s tabulami ELEN).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { errorText, importMonitorProjectMutation, type LineDto, type MonitorDto } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { FieldGroup } from "@/components/ui/field";
import { t } from "@/i18n";

export function MonitorImportDialog({
  monitor,
  lines,
  initialLine,
  onClose,
}: {
  monitor: MonitorDto;
  lines: LineDto[];
  initialLine?: number;
  onClose: () => void;
}) {
  const [project, setProject] = useState(monitor.projects[0] ?? "");
  const [line, setLine] = useState(lines.find((l) => l.number === initialLine)?.number ?? lines[0]?.number ?? 1);
  const save = useMutation({ ...importMonitorProjectMutation(), onSuccess: onClose });

  return (
    <FormDialog
      title={t("monitor.import.title")}
      description={t("monitor.import.hint")}
      onClose={onClose}
      onSubmit={() => save.mutate({ body: { project, line } })}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      submitLabel={t("monitor.import")}
    >
      <FieldGroup>
        <FormField label={t("monitor.import.project")}>
          <OptionSelect value={project} onValueChange={setProject} options={monitor.projects.map((p) => ({ value: p, label: p }))} className="w-full" />
        </FormField>
        <FormField label={t("field.line")}>
          <OptionSelect
            value={line}
            onValueChange={setLine}
            options={lines.map((l) => ({ value: l.number, label: l.name ? `${l.number} – ${l.name}` : String(l.number) }))}
            className="w-full"
          />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}
