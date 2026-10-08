// Nova sprava alebo uprava: druh, trvanie a obsah podla druhu (odseky textu, nahraty obrazok/video, tabula, adresa) s nahladom.
import { useRef, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, Plus, Trash2, Upload } from "lucide-react";
import {
  createSignageMessage,
  errorText,
  getSignageOverviewQueryKey,
  getVirtualOverviewOptions,
  queryClient,
  updateSignageMessage,
  uploadSignageMedia,
  useSimulatorState,
  type SetSignageMessageRequest,
  type SignageAlign,
  type SignageMessageKind,
  type SignageParagraphDto,
  type SignageTextSize,
} from "@/api";
import { ColorField } from "@/components/common/color-field";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";
import { t } from "@/i18n";
import { KINDS, newMessage } from "./signage";
import { SignageSlide } from "./SignageSlide";

const SIZES: SignageTextSize[] = ["small", "medium", "large"];
const ALIGNS: SignageAlign[] = ["left", "center", "right"];
const ACCEPT: Partial<Record<SignageMessageKind, string>> = { image: "image/png,image/jpeg,image/gif,image/webp", video: "video/mp4,video/webm" };

/** Odseky textovej spravy: text, farba, velkost, zarovnanie; poradie a odstranenie. */
function Paragraphs({ value, onChange }: { value: SignageParagraphDto[]; onChange: (value: SignageParagraphDto[]) => void }) {
  const set = (i: number, patch: Partial<SignageParagraphDto>) => onChange(value.map((p, j) => (j === i ? { ...p, ...patch } : p)));
  const move = (i: number, by: number) => {
    const copy = value.slice();
    [copy[i], copy[i + by]] = [copy[i + by], copy[i]];
    onChange(copy);
  };
  return (
    <div className="flex flex-col gap-3">
      {value.map((p, i) => (
        <div key={i} className="flex flex-col gap-2 rounded-md border p-2">
          <Textarea rows={2} value={p.text} aria-label={t("signage.paragraph", { n: i + 1 })} onChange={(e) => set(i, { text: e.target.value })} />
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="color"
              aria-label={t("signage.field.color")}
              title={t("signage.field.color")}
              className="h-8 w-10 cursor-pointer rounded-md border bg-transparent p-0.5"
              value={p.color}
              onChange={(e) => set(i, { color: e.target.value })}
            />
            <OptionSelect<SignageTextSize>
              size="sm"
              ariaLabel={t("signage.field.size")}
              value={p.size}
              onValueChange={(size) => set(i, { size })}
              options={SIZES.map((s) => ({ value: s, label: t(`signage.size.${s}`) }))}
            />
            <OptionSelect<SignageAlign>
              size="sm"
              ariaLabel={t("signage.field.align")}
              value={p.align}
              onValueChange={(align) => set(i, { align })}
              options={ALIGNS.map((a) => ({ value: a, label: t(`signage.align.${a}`) }))}
            />
            <div className="ml-auto flex">
              <Button type="button" variant="ghost" size="icon-sm" title={t("common.up")} disabled={i === 0} onClick={() => move(i, -1)}>
                <ArrowUp />
              </Button>
              <Button type="button" variant="ghost" size="icon-sm" title={t("common.down")} disabled={i === value.length - 1} onClick={() => move(i, 1)}>
                <ArrowDown />
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                title={t("common.delete")}
                disabled={value.length === 1}
                onClick={() => onChange(value.filter((_, j) => j !== i))}
              >
                <Trash2 />
              </Button>
            </div>
          </div>
        </div>
      ))}
      <Button
        type="button"
        variant="outline"
        size="sm"
        className="self-start"
        onClick={() => onChange([...value, { ...value[value.length - 1], text: "" }])}
        disabled={value.length >= 20}
      >
        <Plus />
        {t("signage.paragraph.add")}
      </Button>
    </div>
  );
}

/** Nahraty obrazok alebo video: meno suboru a tlacidlo na nahratie ineho. */
function MediaField({ kind, name, onUploaded }: { kind: SignageMessageKind; name: string | null; onUploaded: (id: string, name: string) => void }) {
  const input = useRef<HTMLInputElement>(null);
  const upload = useMutation({
    mutationFn: (file: File) => uploadSignageMedia({ body: file, query: { name: file.name }, throwOnError: true }),
    onSuccess: ({ data }) => onUploaded(data.id, data.name),
  });
  return (
    <FormField
      label={t(kind === "video" ? "signage.field.video" : "signage.field.image")}
      hint={upload.error ? errorText(upload.error) : t("signage.media.hint")}
    >
      <div className="flex items-center gap-2">
        <Button type="button" variant="outline" disabled={upload.isPending} onClick={() => input.current?.click()}>
          {upload.isPending ? <Spinner /> : <Upload />}
          {t(name ? "signage.media.replace" : "signage.media.upload")}
        </Button>
        <span className="truncate text-sm text-muted-foreground">{name ?? t("signage.media.none")}</span>
        <input
          ref={input}
          type="file"
          hidden
          accept={ACCEPT[kind]}
          onChange={(e) => {
            const file = e.target.files?.[0];
            e.target.value = "";
            if (file) upload.mutate(file);
          }}
        />
      </div>
    </FormField>
  );
}

interface MessageDialogProps {
  id: number | null;
  initial: SetSignageMessageRequest;
  initialMediaName: string | null;
  onClose: () => void;
}

export function MessageDialog({ id, initial, initialMediaName, onClose }: MessageDialogProps) {
  const live = useSimulatorState();
  const virtual = useQuery(getVirtualOverviewOptions()).data?.boards ?? [];
  const [message, setMessage] = useState(initial);
  const [mediaName, setMediaName] = useState(initialMediaName);
  const set = (patch: Partial<SetSignageMessageRequest>) => setMessage((m) => ({ ...m, ...patch }));
  const save = useMutation({
    mutationFn: async (body: SetSignageMessageRequest) => {
      if (id === null) await createSignageMessage({ body, throwOnError: true });
      else await updateSignageMessage({ path: { id }, body, throwOnError: true });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getSignageOverviewQueryKey() });
      onClose();
    },
  });
  // zmena druhu: nazov, trvanie a poznamka ostanu, obsah podla noveho druhu
  const changeKind = (kind: SignageMessageKind) => {
    setMessage((m) => ({ ...newMessage(kind), name: m.name, note: m.note, duration: m.duration, background: m.background }));
    setMediaName(null);
  };
  const kind = message.kind;

  return (
    <FormDialog
      title={id === null ? t("signage.message.new") : t("signage.message.edit", { name: initial.name })}
      onClose={onClose}
      onSubmit={() => save.mutate(message)}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
      size="xl"
    >
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
        <FieldGroup>
          <FormField label={t("signage.field.name")}>
            <Input autoFocus value={message.name} onChange={(e) => set({ name: e.target.value })} />
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("signage.field.kind")}>
              <OptionSelect<SignageMessageKind>
                value={kind}
                onValueChange={changeKind}
                options={KINDS.map((k) => ({ value: k, label: t(`signage.kind.${k}`) }))}
                className="w-full"
              />
            </FormField>
            <FormField label={t("signage.field.duration")} hint={t("signage.field.duration.hint")}>
              <Input type="number" min={1} max={3600} value={message.duration} onChange={(e) => set({ duration: Number.parseInt(e.target.value, 10) || 1 })} />
            </FormField>
          </div>

          {kind === "virtual" && (
            <FormField label={t("signage.field.virtual")} hint={virtual.length === 0 ? t("signage.virtual.none") : undefined}>
              <OptionSelect<number>
                value={message.virtualBoard ?? 0}
                onValueChange={(virtualBoard) => set({ virtualBoard })}
                options={[{ value: 0, label: t("signage.choose") }, ...virtual.map((b) => ({ value: b.id, label: `${b.id} – ${b.name || b.source}` }))]}
                className="w-full"
              />
            </FormField>
          )}
          {kind === "board" && (
            <FormField label={t("signage.field.board")}>
              <OptionSelect<string>
                value={message.board ?? ""}
                onValueChange={(board) => set({ board: board || null })}
                options={[
                  { value: "", label: t("signage.choose") },
                  ...live.boards.map((b) => ({ value: b.id, label: b.name ? `${b.name} (${b.id})` : b.id })),
                ]}
                className="w-full"
              />
            </FormField>
          )}
          {kind === "text" && <Paragraphs value={message.paragraphs} onChange={(paragraphs) => set({ paragraphs })} />}
          {(kind === "image" || kind === "video") && (
            <MediaField
              kind={kind}
              name={mediaName}
              onUploaded={(media, name) => {
                set({ media });
                setMediaName(name);
              }}
            />
          )}
          {kind === "web" && (
            <FormField label={t("signage.field.url")} hint={t("signage.field.url.hint")}>
              <Input type="url" className="font-mono" value={message.url ?? ""} onChange={(e) => set({ url: e.target.value })} />
            </FormField>
          )}
          {kind !== "virtual" && kind !== "web" && (
            <ColorField
              label={t("signage.field.background")}
              value={message.background}
              onChange={(background) => set({ background: background ?? "#000000" })}
            />
          )}
          <FormField label={t("signage.field.note")}>
            <Input value={message.note} onChange={(e) => set({ note: e.target.value })} />
          </FormField>
        </FieldGroup>
        {/* nahlad ostava na mieste pri posuvani dlheho formulara */}
        <div className="flex flex-col gap-2 self-start lg:sticky lg:top-0">
          <span className="text-sm font-medium">{t("signage.preview")}</span>
          <div className="aspect-video w-full overflow-hidden rounded-md border bg-black">
            <SignageSlide message={message} />
          </div>
        </div>
      </div>
    </FormDialog>
  );
}
