// Nova vlastna predloha cela z aktualnej (kopia): nazov a kluc (z nazvu bez diakritiky).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { errorText, getFacesQueryKey, queryClient, setFaceMutation, type BoardFaceDto } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";

/** Kluc z nazvu: male pismena bez diakritiky, cislice a pomlcky. */
function slug(name: string): string {
  return name
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 40);
}

interface Props {
  face: BoardFaceDto;
  existing: string[];
  onClose: () => void;
  onCreated: (id: string) => void;
}

export function NewFaceDialog({ face, existing, onClose, onCreated }: Props) {
  const [name, setName] = useState(face.name + t("designs.copySuffix"));
  // kluc sa odvodzuje z nazvu, kym ho obsluha nezmeni
  const [id, setId] = useState<string | null>(null);
  const effectiveId = id ?? slug(name);
  const taken = existing.includes(effectiveId);
  const create = useMutation({
    ...setFaceMutation(),
    onSuccess: () => {
      const created: BoardFaceDto = { ...face, id: effectiveId, name: name.trim(), builtIn: false };
      // nova predloha do zoznamu hned - vyber by inak cakal na nove nacitanie
      queryClient.setQueryData<BoardFaceDto[]>(getFacesQueryKey(), (old) => [...(old ?? []), created]);
      void queryClient.invalidateQueries({ queryKey: getFacesQueryKey() });
      onCreated(effectiveId);
    },
  });

  return (
    <FormDialog
      title={t("faces.new")}
      onClose={onClose}
      onSubmit={() => !taken && create.mutate({ path: { id: effectiveId }, body: { ...face, id: effectiveId, name: name.trim(), builtIn: false } })}
      error={taken ? t("faces.idTaken", { id: effectiveId }) : create.error && errorText(create.error)}
      busy={create.isPending}
      submitLabel={t("designs.create")}
    >
      <FieldGroup>
        <FormField label={t("faces.name")}>
          <Input autoFocus value={name} onChange={(e) => setName(e.target.value)} />
        </FormField>
        <FormField label={t("faces.id")} hint={t("faces.id.hint")}>
          <Input className="font-mono" value={effectiveId} onChange={(e) => setId(e.target.value)} />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}
