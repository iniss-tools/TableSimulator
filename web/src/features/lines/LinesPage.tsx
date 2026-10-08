// Komunikacne linky - zodpovedaju sekciam Driver v INISSe; export a import liniek s tabulami.
import { useMemo, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { ChevronDown, Download, MonitorDown, Pencil, Plus, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import { deleteLine, errorText, importConfigMutation, PERMISSIONS, useCan, useSimulatorState, type LineDto, type SetLineRequest } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { CopyButton } from "@/components/common/copy-button";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { useNow } from "@/hooks/use-now";
import { defaultEndpoint, lineConfig } from "@/lib/boards";
import { t, type TextKey } from "@/i18n";
import { InissStationDialog } from "./InissStationDialog";
import { LineDialog } from "./LineDialog";
import { lineStatusTime, STATUS_TONES } from "./line-status";

const column = columnsFor<LineDto>();

/** Stav linky s podrobnostou (klient, chyba) a casom - posledna sprava alebo od kedy trva vypadok. */
function StatusCell({ line }: { line: LineDto }) {
  const time = lineStatusTime(line, useNow(1000));
  return (
    <div className="flex flex-col items-start gap-0.5">
      <StatusBadge tone={STATUS_TONES[line.status]}>{t(`line.status.${line.status}`)}</StatusBadge>
      {line.statusDetail && <span className="text-xs text-muted-foreground">{line.statusDetail}</span>}
      {time && <span className="text-xs text-muted-foreground">{time}</span>}
    </div>
  );
}

export function LinesPage() {
  const live = useSimulatorState();
  const edit = useCan()(PERMISSIONS.edit);
  const [editing, setEditing] = useState<{ config: SetLineRequest; isNew: boolean } | null>(null);
  const [inissOpen, setInissOpen] = useState(false);
  const upload = useRef<HTMLInputElement>(null);
  const replace = useRef(false);
  const importing = useMutation({
    ...importConfigMutation(),
    onSuccess: (result) => toast.success(t(result.replaced ? "lines.import.replaced" : "lines.import.added", { lines: result.lines, boards: result.boards })),
    onError: (e) => toast.error(errorText(e)),
  });

  const chooseFile = (replaceAll: boolean) => {
    replace.current = replaceAll;
    upload.current?.click();
  };

  // nahradenie zmaze linky a tabule, ktore v subore nie su - len s potvrdenim
  const onImport = async (files: FileList | null) => {
    const file = files?.[0];
    if (upload.current) upload.current.value = "";
    if (!file) return;
    if (
      replace.current &&
      !(await confirmAction({ title: t("lines.import.replaceTitle"), description: t("lines.import.replaceConfirm"), confirmLabel: t("lines.import") }))
    )
      return;
    importing.mutate({ body: { content: await file.text(), replace: replace.current } });
  };

  const add = () => {
    const used = new Set(live.lines.map((l) => l.number));
    let number = 1;
    while (used.has(number)) number++;
    setEditing({
      isNew: true,
      config: { number, tableClass: 4, endpoint: defaultEndpoint(number), name: "", mode: "display", autoLearn: true, manufacturer: "ELEN", mirror: null },
    });
  };

  const remove = async (line: LineDto) => {
    if (!(await confirmAction({ title: t("line.delete.confirm", { number: line.number }), destructive: true }))) return;
    deleteLine({ path: { number: line.number }, throwOnError: true }).catch((e) => toast.error(errorText(e)));
  };

  const columns = useMemo(
    () => [
      column.accessor("number", { header: t("lines.col.number"), meta: { className: "w-16" } }),
      column.accessor("name", { header: t("lines.col.name") }),
      column.accessor((l) => t(`line.class.${l.tableClass}` as TextKey), { id: "class", header: t("lines.col.class") }),
      column.accessor("endpoint", {
        header: t("lines.col.endpoint"),
        cell: ({ row }) => {
          const l = row.original;
          return (
            <div className="flex flex-col gap-0.5">
              <code className="font-mono text-xs">{l.endpoint}</code>
              {l.mirror && (
                <span className="text-xs text-muted-foreground">
                  {t("lines.mirror", { port: l.mirror })}
                  {l.mirrorError && <span className="ml-1 text-destructive">{l.mirrorError}</span>}
                </span>
              )}
            </div>
          );
        },
      }),
      column.accessor((l) => t(`line.mode.${l.mode}`), { id: "mode", header: t("lines.col.mode") }),
      column.accessor((l) => t(`line.status.${l.status}`), {
        id: "status",
        header: t("lines.col.status"),
        cell: ({ row }) => <StatusCell line={row.original} />,
      }),
      column.accessor("inissTablePort", {
        header: t("lines.col.tablePort"),
        enableSorting: false,
        cell: ({ row }) =>
          row.original.inissTablePort ? (
            <span className="flex items-center gap-1 whitespace-nowrap">
              <code className="font-mono text-xs">{row.original.inissTablePort}</code>
              <CopyButton text={row.original.inissTablePort} />
            </span>
          ) : (
            <span className="text-xs text-muted-foreground">{t("lines.serialTablePort")}</span>
          ),
      }),
      column.accessor((l) => l.stats.frames, {
        id: "traffic",
        header: t("lines.col.traffic"),
        cell: ({ row }) => {
          const s = row.original.stats;
          return (
            <span className="text-xs text-muted-foreground">{t("lines.traffic", { frames: s.frames, bad: s.badFrames, connections: s.connections })}</span>
          );
        },
      }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => (
          <RowActions
            actions={[
              { label: t("common.edit"), icon: Pencil, onClick: () => setEditing({ config: lineConfig(row.original), isNew: false }), hidden: !edit },
              { label: t("common.delete"), icon: Trash2, onClick: () => remove(row.original), destructive: true, hidden: !edit },
            ]}
          />
        ),
      }),
    ],
    [edit],
  );

  return (
    <>
      <PageHeader description={t("lines.hint")}>
        <Button variant="outline" render={<a href="/api/config" download title={t("lines.export.hint")} />}>
          <Download />
          {t("lines.export")}
        </Button>
        {edit && (
          <>
            <Button variant="outline" onClick={() => setInissOpen(true)} title={t("iniss.button.hint")}>
              <MonitorDown />
              {t("iniss.button")}
            </Button>
            <input ref={upload} type="file" accept=".json" hidden onChange={(e) => onImport(e.target.files)} />
            <DropdownMenu>
              <DropdownMenuTrigger render={<Button variant="outline" disabled={importing.isPending} title={t("lines.import.hint")} />}>
                <Upload />
                {t("lines.import")}
                <ChevronDown />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => chooseFile(false)}>{t("lines.import.add")}</DropdownMenuItem>
                <DropdownMenuItem variant="destructive" onClick={() => chooseFile(true)}>
                  {t("lines.import.replaceAll")}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
            <Button onClick={add}>
              <Plus />
              {t("lines.add")}
            </Button>
          </>
        )}
      </PageHeader>
      <DataTable columns={columns} data={live.lines} getRowId={(l) => String(l.number)} pageSize={null} empty={t("lines.empty")} storageKey="lines" />
      {editing && <LineDialog initial={editing.config} isNew={editing.isNew} onClose={() => setEditing(null)} />}
      {inissOpen && <InissStationDialog onClose={() => setInissOpen(false)} />}
    </>
  );
}
