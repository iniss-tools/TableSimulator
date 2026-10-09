// Nova linka alebo uprava linky - zodpoveda sekcii Driver v INISSe.
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { errorText, manufacturersFor, setLineMutation, useSimulatorState, type LineMode, type SetLineRequest } from "@/api";
import { CheckField, FormField } from "@/components/common/form-field";
import { FormDialog } from "@/components/common/form-dialog";
import { OptionSelect } from "@/components/common/option-select";
import { FieldDescription, FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { defaultEndpoint, REMOTE_INISS_CLASS } from "@/lib/boards";
import { t, type TextKey } from "@/i18n";

export function LineDialog({ initial, isNew, onClose }: { initial: SetLineRequest; isNew: boolean; onClose: () => void }) {
  const live = useSimulatorState();
  const [config, setConfig] = useState(initial);
  const save = useMutation({ ...setLineMutation(), onSuccess: onClose });
  const manufacturers = manufacturersFor(live, config.tableClass);
  const remote = config.tableClass === REMOTE_INISS_CLASS;

  return (
    <FormDialog
      title={isNew ? t("dialog.line.new") : t("dialog.line.edit", { number: config.number })}
      onClose={onClose}
      onSubmit={() => save.mutate({ path: { number: config.number }, body: config })}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
    >
      <FieldGroup>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={t("field.number")}>
            <Input
              type="number"
              min={1}
              max={100}
              value={config.number}
              disabled={!isNew}
              onChange={(e) => {
                const number = Number.parseInt(e.target.value, 10) || 1;
                setConfig({ ...config, number, endpoint: defaultEndpoint(number) });
              }}
            />
          </FormField>
          <FormField label={t("field.tableClass")}>
            <OptionSelect
              value={config.tableClass}
              onValueChange={(tableClass) => {
                const forClass = manufacturersFor(live, tableClass);
                setConfig({ ...config, tableClass, manufacturer: forClass.includes(config.manufacturer) ? config.manufacturer : (forClass[0] ?? "") });
              }}
              options={live.supportedClasses.map((c) => ({ value: c, label: t(`line.class.${c}` as TextKey) }))}
              className="w-full"
            />
          </FormField>
        </div>
        {remote && <FieldDescription>{t("line.class.128.hint")}</FieldDescription>}
        <FormField label={t("field.name")}>
          <Input value={config.name} onChange={(e) => setConfig({ ...config, name: e.target.value })} />
        </FormField>
        <FormField label={t("field.endpoint")} hint={t("field.endpoint.hint")}>
          <Input value={config.endpoint} className="font-mono" onChange={(e) => setConfig({ ...config, endpoint: e.target.value })} />
        </FormField>
        <FormField label={t("field.mode")} hint={t("line.mode.hint")}>
          <OptionSelect<LineMode>
            value={config.mode}
            onValueChange={(mode) => setConfig({ ...config, mode })}
            options={[
              { value: "display", label: t("line.mode.display") },
              { value: "monitor", label: t("line.mode.monitor") },
            ]}
            className="w-full"
          />
        </FormField>
        <CheckField label={t("field.autoLearn")} checked={config.autoLearn} onCheckedChange={(autoLearn) => setConfig({ ...config, autoLearn })} />
        {!remote && (
          <FormField label={t("field.lineManufacturer")}>
            <OptionSelect
              value={config.manufacturer}
              onValueChange={(manufacturer) => setConfig({ ...config, manufacturer })}
              options={manufacturers.map((m) => ({ value: m, label: m }))}
              className="w-full"
            />
          </FormField>
        )}
        <FormField label={t("field.mirror")} hint={t("field.mirror.hint")}>
          <Input
            value={config.mirror ?? ""}
            placeholder="COM10:9600,n,8,1"
            className="font-mono"
            onChange={(e) => setConfig({ ...config, mirror: e.target.value.trim() || null })}
          />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}
