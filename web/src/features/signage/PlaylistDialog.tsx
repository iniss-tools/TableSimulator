// Novy playlist alebo uprava: kedy plati (den, kazdy den, dni v tyzdni a casove okno) a spravy po poradi s trvanim.
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import {
  createSignagePlaylist,
  errorText,
  getSignageOverviewQueryKey,
  queryClient,
  updateSignagePlaylist,
  type SetSignagePlaylistRequest,
  type SignageOverviewDto,
  type SignagePeriod,
} from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import { t, type TextKey } from "@/i18n";
import { durationText, itemDuration, minutesText, parseMinutes, WEEKDAYS } from "./signage";

const PERIODS: SignagePeriod[] = ["daily", "weekly", "once"];

interface PlaylistDialogProps {
  id: number | null;
  initial: SetSignagePlaylistRequest;
  data: SignageOverviewDto;
  onClose: () => void;
}

export function PlaylistDialog({ id, initial, data, onClose }: PlaylistDialogProps) {
  const [playlist, setPlaylist] = useState(initial);
  const set = (patch: Partial<SetSignagePlaylistRequest>) => setPlaylist((p) => ({ ...p, ...patch }));
  const save = useMutation({
    mutationFn: async (body: SetSignagePlaylistRequest) => {
      if (id === null) await createSignagePlaylist({ body, throwOnError: true });
      else await updateSignagePlaylist({ path: { id }, body, throwOnError: true });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getSignageOverviewQueryKey() });
      onClose();
    },
  });
  const items = playlist.items;
  const setItems = (next: typeof items) => set({ items: next });
  const move = (i: number, by: number) => {
    const copy = items.slice();
    [copy[i], copy[i + by]] = [copy[i + by], copy[i]];
    setItems(copy);
  };
  const total = items.reduce((sum, i) => sum + itemDuration(i, data), 0);
  const firstMessage = data.messages[0]?.id;

  return (
    <FormDialog
      title={id === null ? t("signage.playlist.new") : t("signage.playlist.edit", { name: initial.name })}
      onClose={onClose}
      onSubmit={() => save.mutate(playlist)}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="lg"
    >
      <FieldGroup>
        <FormField label={t("signage.field.name")}>
          <Input autoFocus value={playlist.name} onChange={(e) => set({ name: e.target.value })} />
        </FormField>
        <div className="grid gap-4 sm:grid-cols-3">
          <FormField label={t("signage.field.period")}>
            <OptionSelect<SignagePeriod>
              value={playlist.period}
              onValueChange={(period) => set({ period })}
              options={PERIODS.map((p) => ({ value: p, label: t(`signage.period.${p}`) }))}
              className="w-full"
            />
          </FormField>
          <FormField label={t(playlist.period === "once" ? "signage.field.day" : "signage.field.start")}>
            <Input type="date" value={playlist.start} onChange={(e) => e.target.value && set({ start: e.target.value })} />
          </FormField>
          {playlist.period !== "once" && (
            <FormField label={t("signage.field.end")} hint={t("signage.field.end.hint")}>
              <Input type="date" value={playlist.end ?? ""} min={playlist.start} onChange={(e) => set({ end: e.target.value || null })} />
            </FormField>
          )}
        </div>
        {playlist.period === "weekly" && (
          <FormField label={t("signage.field.weekdays")}>
            <ToggleGroup
              multiple
              variant="outline"
              value={playlist.weekdays.map(String)}
              onValueChange={(values: string[]) => set({ weekdays: values.map(Number).sort((a, b) => a - b) })}
              className="flex-wrap"
            >
              {WEEKDAYS.map((d) => (
                <ToggleGroupItem key={d} value={String(d)} aria-label={t(`signage.weekday.${d}` as TextKey)}>
                  {t(`signage.weekday.${d}` as TextKey)}
                </ToggleGroupItem>
              ))}
            </ToggleGroup>
          </FormField>
        )}
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={t("signage.field.from")} hint={t("signage.field.window.hint")}>
            <Input type="time" value={minutesText(playlist.from)} onChange={(e) => set({ from: parseMinutes(e.target.value) ?? 0 })} />
          </FormField>
          <FormField label={t("signage.field.to")}>
            <Input type="time" value={minutesText(playlist.to % (24 * 60))} onChange={(e) => set({ to: parseMinutes(e.target.value) ?? 0 })} />
          </FormField>
        </div>

        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between gap-2">
            <span className="text-sm font-medium">{t("signage.items")}</span>
            <span className="text-xs text-muted-foreground">{t("signage.items.total", { count: items.length, duration: durationText(total) })}</span>
          </div>
          {items.length === 0 && <p className="text-sm text-muted-foreground">{t("signage.items.empty")}</p>}
          {items.map((item, i) => (
            <div key={i} className="flex flex-wrap items-center gap-2 rounded-md border p-2">
              <span className="w-6 text-right text-xs text-muted-foreground tabular-nums">{i + 1}.</span>
              <OptionSelect<number>
                size="sm"
                ariaLabel={t("signage.item.message", { n: i + 1 })}
                value={item.message}
                onValueChange={(message) => setItems(items.map((x, j) => (j === i ? { ...x, message } : x)))}
                options={data.messages.map((m) => ({ value: m.id, label: m.definition.name }))}
                className="min-w-48 flex-1"
              />
              <Input
                type="number"
                min={1}
                max={3600}
                className="h-7 w-24"
                aria-label={t("signage.item.duration", { n: i + 1 })}
                title={t("signage.item.duration.hint")}
                placeholder={String(itemDuration({ message: item.message, duration: null }, data))}
                value={item.duration ?? ""}
                onChange={(e) => setItems(items.map((x, j) => (j === i ? { ...x, duration: e.target.value === "" ? null : Number(e.target.value) || 1 } : x)))}
              />
              <span className="text-xs text-muted-foreground">s</span>
              <div className="ml-auto flex">
                <Button type="button" variant="ghost" size="icon-sm" title={t("common.up")} disabled={i === 0} onClick={() => move(i, -1)}>
                  <ArrowUp />
                </Button>
                <Button type="button" variant="ghost" size="icon-sm" title={t("common.down")} disabled={i === items.length - 1} onClick={() => move(i, 1)}>
                  <ArrowDown />
                </Button>
                <Button type="button" variant="ghost" size="icon-sm" title={t("common.delete")} onClick={() => setItems(items.filter((_, j) => j !== i))}>
                  <Trash2 />
                </Button>
              </div>
            </div>
          ))}
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="self-start"
            disabled={firstMessage === undefined}
            onClick={() => firstMessage !== undefined && setItems([...items, { message: items[items.length - 1]?.message ?? firstMessage, duration: null }])}
          >
            <Plus />
            {t("signage.item.add")}
          </Button>
          {firstMessage === undefined && <p className="text-xs text-muted-foreground">{t("signage.items.noMessages")}</p>}
        </div>
      </FieldGroup>
    </FormDialog>
  );
}
