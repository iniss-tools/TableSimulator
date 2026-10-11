// Novy vlastny vzhlad z aktualneho (kopia): nazov a id priecinka (z nazvu bez diakritiky).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { errorText, getDesignsQueryKey, queryClient, setDesignMutation, type DesignInfoDto } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import type { BoardDesign } from "@/render/design";
import { t } from "@/i18n";

/** Id priecinka z nazvu: male pismena bez diakritiky, cislice a pomlcky. */
function slug(name: string): string {
  return name
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 40);
}

interface NewDesignDialogProps {
  design: BoardDesign;
  existing: string[];
  onClose: () => void;
  onCreated: (id: string) => void;
}

export function NewDesignDialog({ design, existing, onClose, onCreated }: NewDesignDialogProps) {
  const [name, setName] = useState(design.name + t("designs.copySuffix"));
  // id sa odvodzuje z nazvu, kym ho obsluha nezmeni
  const [id, setId] = useState<string | null>(null);
  const effectiveId = id ?? slug(name);
  const taken = existing.includes(effectiveId);
  const create = useMutation({
    ...setDesignMutation(),
    onSuccess: () => {
      // novy vzhlad do zoznamu hned - vyber inak kym sa zoznam nacita ukaze len id
      queryClient.setQueryData<DesignInfoDto[]>(getDesignsQueryKey(), (old) => [
        ...(old ?? []),
        { id: effectiveId, name: name.trim(), renderer: design.renderer, builtIn: false, overrides: false },
      ]);
      void queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
      onCreated(effectiveId);
    },
  });

  return (
    <FormDialog
      title={t("designs.new")}
      onClose={onClose}
      onSubmit={() => !taken && create.mutate({ path: { id: effectiveId }, body: { ...design, name: name.trim() } })}
      error={taken ? t("designs.idTaken", { id: effectiveId }) : create.error && errorText(create.error)}
      busy={create.isPending}
      submitLabel={t("designs.create")}
    >
      <FieldGroup>
        <FormField label={t("designs.name")}>
          <Input autoFocus value={name} onChange={(e) => setName(e.target.value)} />
        </FormField>
        <FormField label={t("designs.id")} hint={t("designs.id.hint")}>
          <Input className="font-mono" value={effectiveId} onChange={(e) => setId(e.target.value)} />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}
