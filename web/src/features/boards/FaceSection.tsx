// Celo tabule v dialogu tabule: vyber predlohy, hodnoty jej premennych (koľaj, nástupište…) a nahlad.
import { useQuery } from "@tanstack/react-query";
import { getFacesOptions, type BoardDto, type BoardProfileDto } from "@/api";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { FieldDescription } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { previewBoard, variableLabel } from "@/features/faces/faces";
import { faceVariables, resolveFace } from "@/render/face";
import { t } from "@/i18n";
import { BoardView } from "./BoardView";

/** Bez predlohy. */
const NONE = "";
/** Hlavicka z predlohy ELEN Monitora - server ju pri ulozeni zmeni na predlohu cela. */
const HEADER = "@header";

interface Props {
  profile: BoardProfileDto;
  onChange: (patch: Partial<BoardProfileDto>) => void;
  manufacturer: string;
  name: string;
  /** Ziva tabula pri uprave (nahlad s jej obsahom). */
  live: BoardDto | undefined;
}

export function FaceSection({ profile, onChange, manufacturer, name, live }: Props) {
  const faces = useQuery(getFacesOptions()).data ?? [];
  const face = resolveFace(profile.face, profile.header, faces);
  const variables = faceVariables(face);
  const value = profile.face ?? (profile.header ? HEADER : NONE);
  const setValue = (key: string, text: string) => onChange({ faceValues: { ...profile.faceValues, [key]: text } });

  return (
    <div className="flex flex-col gap-4">
      <FieldDescription>{t("face.section.hint")}</FieldDescription>
      <FormField label={t("face.template")} hint={<a href="#/faces">{t("face.editTemplates")}</a>}>
        <OptionSelect
          value={value}
          onValueChange={(id) => id !== HEADER && onChange({ face: id || null, header: null })}
          options={[
            { value: NONE, label: t("face.none") },
            ...(profile.header ? [{ value: HEADER, label: t("face.fromHeader") }] : []),
            ...faces.map((f) => ({ value: f.id, label: f.builtIn ? f.name : `${f.name} (${f.id})` })),
          ]}
          className="w-full"
        />
      </FormField>
      {profile.face && !face && <FieldDescription className="text-amber-700 dark:text-amber-300">{t("face.missing", { id: profile.face })}</FieldDescription>}
      {variables.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-3">
          {variables.map((key) => (
            <FormField key={key} label={variableLabel(key)}>
              <Input value={profile.faceValues?.[key] ?? ""} onChange={(e) => setValue(key, e.target.value)} />
            </FormField>
          ))}
        </div>
      )}
      {face && <BoardView board={previewBoard(profile, manufacturer, name, live)} face={face} />}
    </div>
  );
}
