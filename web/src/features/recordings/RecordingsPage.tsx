// Zaznamy komunikacie: nahravanie prevadzky liniek a jej prehratie do simulatora.
import { useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { Download, GitCompareArrows, Play, Square, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import {
  deleteRecording,
  errorText,
  getRecordingsOptions,
  getRecordingsQueryKey,
  PERMISSIONS,
  playRecording,
  queryClient,
  startRecording,
  stopRecording,
  uploadRecording,
  useCan,
  type RecorderStatusDto,
  type RecordingInfoDto,
  type RecordingsDto,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useNow } from "@/hooks/use-now";
import { locale, t } from "@/i18n";
import { duration, isOlderReplay, SPEEDS } from "./replay";
import { ReplayPlayer } from "./ReplayPlayer";

/** Zaznamy, nahravanie a prehravanie - zmeny prinasa WebSocket. */
// znova nacitany zoznam nesmie prepisat novsi stav prehravania zo servera (WebSocket, odpoved na posun)
const useRecordings = () =>
  useQuery({
    ...getRecordingsOptions(),
    structuralSharing: (old: unknown, next: unknown) => {
      const previous = old as RecordingsDto | undefined;
      const fresh = next as RecordingsDto;
      return previous && isOlderReplay(fresh.replay, previous.replay) ? { ...fresh, replay: previous.replay } : fresh;
    },
  }).data;
const refresh = () => queryClient.invalidateQueries({ queryKey: getRecordingsQueryKey() });
const failed = (e: unknown) => toast.error(errorText(e));

function size(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toLocaleString(locale(), { maximumFractionDigits: 1 })} kB`;
  return `${(bytes / 1024 / 1024).toLocaleString(locale(), { maximumFractionDigits: 1 })} MB`;
}

/** Nahravanie: poznamka a start, pocas nahravania priebeh a zastavenie. */
function Recorder({ recorder, manage }: { recorder: RecorderStatusDto | undefined; manage: boolean }) {
  const [note, setNote] = useState("");
  // dlzka zaznamu bezi aj medzi spravami servera
  const now = useNow(1000);

  if (recorder?.recording) {
    const elapsed = recorder.started ? (now - Date.parse(recorder.started)) / 1000 : 0;
    return (
      <Card className="border-red-500/40 bg-red-500/5">
        <CardContent className="flex flex-wrap items-center gap-3">
          <span className="size-3 animate-pulse rounded-full bg-red-500" />
          <span className="flex-1 text-sm">
            {t("rec.running", { name: recorder.name ?? "", duration: duration(elapsed), entries: recorder.entries, size: size(recorder.bytes) })}
          </span>
          {manage && (
            <Button variant="destructive" onClick={() => stopRecording({ throwOnError: true }).catch(failed)}>
              <Square />
              {t("rec.stop")}
            </Button>
          )}
        </CardContent>
      </Card>
    );
  }

  if (!manage) return null;
  return (
    <form
      className="flex flex-wrap items-center gap-2"
      onSubmit={(e) => {
        e.preventDefault();
        startRecording({ body: { note }, throwOnError: true })
          .then(() => setNote(""))
          .catch(failed);
      }}
    >
      <Input className="max-w-sm flex-1" placeholder={t("rec.note")} aria-label={t("rec.note")} value={note} onChange={(e) => setNote(e.target.value)} />
      <Button type="submit">
        <span className="size-2.5 rounded-full bg-red-500" />
        {t("rec.start")}
      </Button>
    </form>
  );
}

const column = columnsFor<RecordingInfoDto>();

export function RecordingsPage() {
  const navigate = useNavigate();
  const recordings = useRecordings();
  const recorder = recordings?.recorder;
  const manage = useCan()(PERMISSIONS.recordings);
  const [speed, setSpeed] = useState(1);
  const [loop, setLoop] = useState(false);
  const upload = useRef<HTMLInputElement>(null);
  // nahravany zaznam sa neda prehrat ani zmazat
  const recordingName = recorder?.recording ? recorder.name : null;

  const onUpload = async (files: FileList | null) => {
    for (const file of Array.from(files ?? [])) {
      try {
        await uploadRecording({ body: file, query: { name: file.name }, throwOnError: true });
      } catch {
        toast.error(t("rec.uploadBad"), { description: file.name });
      }
    }
    if (upload.current) upload.current.value = "";
    void refresh();
  };

  const remove = async (name: string) => {
    if (!(await confirmAction({ title: t("rec.delete.confirm", { name }), destructive: true }))) return;
    deleteRecording({ path: { name }, throwOnError: true }).then(refresh, failed);
  };

  const columns = useMemo(
    () => [
      column.accessor("name", { header: t("rec.col.name"), cell: ({ getValue }) => <span className="font-medium whitespace-nowrap">{getValue()}</span> }),
      column.accessor("created", {
        header: t("rec.col.created"),
        sortFn: "datetime",
        cell: ({ getValue }) => <span className="whitespace-nowrap">{new Date(getValue()).toLocaleString(locale())}</span>,
      }),
      column.accessor("duration", { header: t("rec.col.duration"), cell: ({ getValue }) => duration(getValue()) }),
      column.accessor((r) => r.lines.join(", "), { id: "lines", header: t("rec.col.lines") }),
      column.accessor("frames", { header: t("rec.col.frames") }),
      column.accessor("size", { header: t("rec.col.size"), cell: ({ getValue }) => <span className="whitespace-nowrap">{size(getValue())}</span> }),
      column.accessor((r) => r.note ?? "", { id: "note", header: t("rec.col.note") }),
      column.display({
        id: "actions",
        meta: { className: "w-px whitespace-nowrap text-right" },
        cell: ({ row }) => {
          const r = row.original;
          return (
            <div className="flex items-center justify-end gap-1">
              {manage && (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={recordingName === r.name}
                  onClick={() => playRecording({ body: { name: r.name, speed, loop, position: 0, paused: false }, throwOnError: true }).catch(failed)}
                >
                  <Play />
                  {t("rec.play")}
                </Button>
              )}
              <RowActions
                actions={[
                  {
                    label: t("rec.compare"),
                    icon: GitCompareArrows,
                    onClick: () => void navigate({ to: "/recordings/compare", search: { a: r.name } }),
                  },
                  {
                    label: t("rec.download"),
                    icon: Download,
                    onClick: () => window.open(`/api/recordings/${encodeURIComponent(r.name)}/file`, "_self"),
                  },
                  {
                    label: t("common.delete"),
                    icon: Trash2,
                    destructive: true,
                    hidden: !manage,
                    disabled: recordingName === r.name,
                    onClick: () => remove(r.name),
                  },
                ]}
              />
            </div>
          );
        },
      }),
    ],
    [manage, speed, loop, recordingName, navigate],
  );

  return (
    <>
      <PageHeader description={t("rec.hint")} />
      <div className="mb-6 flex flex-col gap-3">
        <Recorder recorder={recorder} manage={manage} />
        {recordings?.replay && <ReplayPlayer replay={recordings.replay} manage={manage} />}
      </div>
      <DataTable
        columns={columns}
        data={recordings?.items ?? []}
        getRowId={(r) => r.name}
        initialSorting={[{ id: "created", desc: true }]}
        empty={t("rec.empty")}
        toolbar={
          <>
            <div className="flex items-center gap-2">
              <Label>{t("rec.speed")}</Label>
              <OptionSelect
                value={speed}
                onValueChange={setSpeed}
                options={SPEEDS.map((s) => ({ value: s, label: s > 0 ? `${s}×` : t("rec.speed.max") }))}
                ariaLabel={t("rec.speed")}
              />
            </div>
            <Label className="font-normal">
              <Checkbox checked={loop} onCheckedChange={(value) => setLoop(value === true)} />
              {t("rec.loop")}
            </Label>
            {manage && (
              <>
                <input ref={upload} type="file" accept=".tsrec" multiple hidden onChange={(e) => onUpload(e.target.files)} />
                <Button variant="outline" className="ml-auto" onClick={() => upload.current?.click()}>
                  <Upload />
                  {t("rec.upload")}
                </Button>
              </>
            )}
          </>
        }
      />
    </>
  );
}
