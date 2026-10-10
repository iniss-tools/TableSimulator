// Informacne panely (monitory): skupina, ci je pripojeny a co prave hra; odkaz na stranku panelu pre monitor.
import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ExternalLink, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createSignagePanel,
  deleteSignagePanel,
  errorText,
  getSignageOverviewOptions,
  getSignageOverviewQueryKey,
  PERMISSIONS,
  queryClient,
  updateSignagePanel,
  useCan,
  type SetSignagePanelRequest,
  type SignageOverviewDto,
  type SignagePanelDto,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { CopyButton } from "@/components/common/copy-button";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { useNow } from "@/hooks/use-now";
import { t } from "@/i18n";
import { SignageBackup } from "./SignageBackup";
import { formatAgo } from "@/lib/format";
import { messageName } from "./signage";

/** Prehlad sa obnovi o tolko - ci je panel pripojeny a co prave hra. */
const REFRESH_MS = 5000;

const column = columnsFor<SignagePanelDto>();

function PanelDialog({ id, initial, data, onClose }: { id: number | null; initial: SetSignagePanelRequest; data: SignageOverviewDto; onClose: () => void }) {
  const [panel, setPanel] = useState(initial);
  const save = useMutation({
    mutationFn: async (body: SetSignagePanelRequest) => {
      if (id === null) await createSignagePanel({ body, throwOnError: true });
      else await updateSignagePanel({ path: { id }, body, throwOnError: true });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: getSignageOverviewQueryKey() });
      onClose();
    },
  });
  return (
    <FormDialog
      title={id === null ? t("signage.panel.new") : t("signage.panel.edit", { id })}
      onClose={onClose}
      onSubmit={() => save.mutate(panel)}
      error={save.error && errorText(save.error)}
      busy={save.isPending}
    >
      <FieldGroup>
        <FormField label={t("signage.field.name")}>
          <Input autoFocus value={panel.name} onChange={(e) => setPanel({ ...panel, name: e.target.value })} />
        </FormField>
        <FormField label={t("signage.field.group")} hint={t("signage.field.group.hint")}>
          <OptionSelect<number>
            value={panel.group ?? 0}
            onValueChange={(group) => setPanel({ ...panel, group: group || null })}
            options={[{ value: 0, label: t("signage.group.none") }, ...data.groups.map((g) => ({ value: g.id, label: g.definition.name }))]}
            className="w-full"
          />
        </FormField>
        <FormField label={t("signage.field.location")}>
          <Input value={panel.note} onChange={(e) => setPanel({ ...panel, note: e.target.value })} />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}

/** Pripojeny / nepripojeny a kedy sa naposledy ozval. */
function StatusCell({ panel }: { panel: SignagePanelDto }) {
  const now = useNow(1000);
  const status = panel.status;
  if (!status) return <StatusBadge tone="off">{t("signage.panel.never")}</StatusBadge>;
  return (
    <div className="flex flex-col items-start gap-0.5">
      <StatusBadge tone={status.online ? "ok" : "bad"}>{t(status.online ? "signage.panel.online" : "signage.panel.offline")}</StatusBadge>
      <span className="text-xs text-muted-foreground">
        {status.address} · {formatAgo(status.lastSeenAt, now)}
      </span>
    </div>
  );
}

const panelUrl = (id: number) => `${location.origin}/#/panel/${id}`;

export function PanelsPage() {
  const edit = useCan()(PERMISSIONS.signage);
  const data = useQuery({ ...getSignageOverviewOptions(), refetchInterval: REFRESH_MS, refetchOnMount: "always" }).data;
  const [editing, setEditing] = useState<{ id: number | null; panel: SetSignagePanelRequest } | null>(null);

  const remove = async (panel: SignagePanelDto) => {
    if (!(await confirmAction({ title: t("signage.panel.delete", { name: panel.definition.name }), destructive: true }))) return;
    deleteSignagePanel({ path: { id: panel.id }, throwOnError: true }).catch((e) => toast.error(errorText(e)));
  };

  const columns = useMemo(
    () => [
      column.accessor("id", { header: t("signage.col.number"), meta: { className: "w-16" } }),
      column.accessor((p) => p.definition.name, { id: "name", header: t("signage.col.name") }),
      column.accessor((p) => data?.groups.find((g) => g.id === p.definition.group)?.definition.name ?? "", {
        id: "group",
        header: t("signage.col.group"),
        cell: ({ getValue }) => getValue() || <span className="text-muted-foreground">{t("signage.group.none")}</span>,
      }),
      column.accessor((p) => (p.status?.online ? 1 : 0), {
        id: "status",
        header: t("signage.col.status"),
        cell: ({ row }) => <StatusCell panel={row.original} />,
      }),
      column.accessor((p) => messageName(p.playing, data), {
        id: "playing",
        header: t("signage.col.playing"),
        cell: ({ getValue }) => getValue() || <span className="text-muted-foreground">{t("signage.offline")}</span>,
      }),
      column.accessor((p) => p.definition.note, { id: "note", header: t("signage.col.location") }),
      column.display({
        id: "link",
        header: t("signage.col.link"),
        cell: ({ row }) => (
          <span className="flex items-center gap-1 whitespace-nowrap">
            <code className="font-mono text-xs">#/panel/{row.original.id}</code>
            <CopyButton text={panelUrl(row.original.id)} />
          </span>
        ),
      }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => (
          <RowActions
            actions={[
              { label: t("signage.panel.open"), icon: ExternalLink, onClick: () => window.open(`#/panel/${row.original.id}`, "_blank") },
              { label: t("common.edit"), icon: Pencil, onClick: () => setEditing({ id: row.original.id, panel: row.original.definition }), hidden: !edit },
              { label: t("common.delete"), icon: Trash2, onClick: () => remove(row.original), destructive: true, hidden: !edit },
            ]}
          />
        ),
      }),
    ],
    [edit, data],
  );

  return (
    <>
      <PageHeader description={t("signage.panels.hint")}>
        <SignageBackup edit={edit} />
        {edit && (
          <Button onClick={() => setEditing({ id: null, panel: { name: "", note: "", group: data?.groups[0]?.id ?? null } })}>
            <Plus />
            {t("signage.panel.add")}
          </Button>
        )}
      </PageHeader>
      <DataTable
        columns={columns}
        data={data?.panels ?? []}
        getRowId={(p) => String(p.id)}
        pageSize={null}
        empty={t("signage.panels.empty")}
        storageKey="signage-panels"
      />
      {editing && data && <PanelDialog id={editing.id} initial={editing.panel} data={data} onClose={() => setEditing(null)} />}
    </>
  );
}
