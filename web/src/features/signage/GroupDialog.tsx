// Nova skupina panelov alebo uprava: playlisty skupiny a co hra mimo ich platnosti (predvoleny playlist alebo sprava).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import {
  createSignageGroup,
  errorText,
  getSignageOverviewQueryKey,
  queryClient,
  updateSignageGroup,
  type SetSignageGroupRequest,
  type SignageOverviewDto,
} from "@/api";
import { CheckField, FormField } from "@/components/common/form-field";
import { FormDialog } from "@/components/common/form-dialog";
import { OptionSelect } from "@/components/common/option-select";
import { FieldGroup, FieldLegend, FieldSet } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";
import { playlistSummary } from "./signage";

interface GroupDialogProps {
  id: number | null;
  initial: SetSignageGroupRequest;
  data: SignageOverviewDto;
  onClose: () => void;
}

/** Predvoleny obsah ako jedna volba: nic, playlist N alebo sprava N (ako v INISSView 2). */
function fallbackValue(g: SetSignageGroupRequest): string {
  if (g.defaultPlaylist !== null) return `pl${g.defaultPlaylist}`;
  if (g.defaultMessage !== null) return `msg${g.defaultMessage}`;
  return "";
}

export function GroupDialog({ id, initial, data, onClose }: GroupDialogProps) {
  const [group, setGroup] = useState(initial);
  const set = (patch: Partial<SetSignageGroupRequest>) => setGroup((g) => ({ ...g, ...patch }));
  const save = useMutation({
    mutationFn: async (body: SetSignageGroupRequest) => {
      if (id === null) await createSignageGroup({ body, throwOnError: true });
      else await updateSignageGroup({ path: { id }, body, throwOnError: true });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getSignageOverviewQueryKey() });
      onClose();
    },
  });
  const toggle = (playlist: number, on: boolean) => set({ playlists: on ? [...group.playlists, playlist] : group.playlists.filter((p) => p !== playlist) });
  const setFallback = (value: string) =>
    set({
      defaultPlaylist: value.startsWith("pl") ? Number(value.slice(2)) : null,
      defaultMessage: value.startsWith("msg") ? Number(value.slice(3)) : null,
    });

  return (
    <FormDialog
      title={id === null ? t("signage.group.new") : t("signage.group.edit", { name: initial.name })}
      onClose={onClose}
      onSubmit={() => save.mutate(group)}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="lg"
    >
      <FieldGroup>
        <FormField label={t("signage.field.name")}>
          <Input autoFocus value={group.name} onChange={(e) => set({ name: e.target.value })} />
        </FormField>
        <FieldSet>
          <FieldLegend variant="label">{t("signage.field.playlists")}</FieldLegend>
          {data.playlists.length === 0 && <p className="text-sm text-muted-foreground">{t("signage.playlists.empty")}</p>}
          {data.playlists.map((p) => (
            <CheckField
              key={p.id}
              label={p.definition.name}
              hint={playlistSummary(p.definition)}
              checked={group.playlists.includes(p.id)}
              onCheckedChange={(on) => toggle(p.id, on)}
            />
          ))}
        </FieldSet>
        <FormField label={t("signage.field.fallback")} hint={t("signage.field.fallback.hint")}>
          <OptionSelect<string>
            value={fallbackValue(group)}
            onValueChange={setFallback}
            options={[
              { value: "", label: t("signage.fallback.none") },
              ...data.playlists.map((p) => ({ value: `pl${p.id}`, label: t("signage.fallback.playlist", { name: p.definition.name }) })),
              ...data.messages.map((m) => ({ value: `msg${m.id}`, label: t("signage.fallback.message", { name: m.definition.name }) })),
            ]}
            className="w-full"
          />
        </FormField>
        <FormField label={t("signage.field.note")}>
          <Input value={group.note} onChange={(e) => set({ note: e.target.value })} />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}
