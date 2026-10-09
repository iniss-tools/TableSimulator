// Spravy informacnych panelov: tabule P/O, texty, obrazky, videa, tabule simulatora a webove stranky - hraju sa v playlistoch.
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, Copy, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteSignageMessage, errorText, getSignageOverviewOptions, PERMISSIONS, useCan, type SignageMessageDto, type SignageMessageKind } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { t } from "@/i18n";
import { SignageBackup } from "./SignageBackup";
import { MessageDialog } from "./MessageDialog";
import { durationText, KIND_ICONS, KINDS, messageUsers, newMessage } from "./signage";

const column = columnsFor<SignageMessageDto>();

export function MessagesPage() {
  const edit = useCan()(PERMISSIONS.edit);
  const data = useQuery(getSignageOverviewOptions()).data;
  const [editing, setEditing] = useState<{ id: number | null; message: SignageMessageDto["definition"]; mediaName: string | null } | null>(null);

  const remove = async (message: SignageMessageDto) => {
    if (!(await confirmAction({ title: t("signage.message.delete", { name: message.definition.name }), destructive: true }))) return;
    deleteSignageMessage({ path: { id: message.id }, throwOnError: true }).catch((e) => toast.error(errorText(e)));
  };

  const columns = useMemo(
    () => [
      column.accessor((m) => m.definition.name, { id: "name", header: t("signage.col.name") }),
      column.accessor((m) => t(`signage.kind.${m.definition.kind}`), {
        id: "kind",
        header: t("signage.col.kind"),
        cell: ({ row }) => {
          const Icon = KIND_ICONS[row.original.definition.kind];
          return (
            <span className="flex items-center gap-2 whitespace-nowrap">
              <Icon className="size-4 text-muted-foreground" />
              {t(`signage.kind.${row.original.definition.kind}`)}
            </span>
          );
        },
      }),
      column.accessor((m) => m.definition.duration, { id: "duration", header: t("signage.col.duration"), cell: ({ getValue }) => durationText(getValue()) }),
      column.accessor((m) => (data ? messageUsers(m, data).join(", ") : ""), {
        id: "used",
        header: t("signage.col.used"),
        cell: ({ getValue }) => <span className="text-muted-foreground">{getValue() || t("signage.unused")}</span>,
      }),
      column.accessor((m) => m.definition.note, { id: "note", header: t("signage.col.note") }),
      column.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => (
          <RowActions
            actions={[
              {
                label: t("common.edit"),
                icon: Pencil,
                onClick: () => setEditing({ id: row.original.id, message: row.original.definition, mediaName: row.original.mediaName }),
                hidden: !edit,
              },
              {
                label: t("signage.duplicate"),
                icon: Copy,
                onClick: () =>
                  setEditing({
                    id: null,
                    message: { ...row.original.definition, name: row.original.definition.name + t("designs.copySuffix") },
                    mediaName: row.original.mediaName,
                  }),
                hidden: !edit,
              },
              { label: t("common.delete"), icon: Trash2, onClick: () => remove(row.original), destructive: true, hidden: !edit },
            ]}
          />
        ),
      }),
    ],
    [edit, data],
  );

  const add = (kind: SignageMessageKind) => setEditing({ id: null, message: newMessage(kind), mediaName: null });

  return (
    <>
      <PageHeader description={t("signage.messages.hint")}>
        <SignageBackup edit={edit} />
        {edit && (
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button />}>
              <Plus />
              {t("signage.message.add")}
              <ChevronDown />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {KINDS.map((kind) => {
                const Icon = KIND_ICONS[kind];
                return (
                  <DropdownMenuItem key={kind} onClick={() => add(kind)}>
                    <Icon />
                    {t(`signage.kind.${kind}`)}
                  </DropdownMenuItem>
                );
              })}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </PageHeader>
      <DataTable
        columns={columns}
        data={data?.messages ?? []}
        getRowId={(m) => String(m.id)}
        pageSize={null}
        empty={t("signage.messages.empty")}
        storageKey="signage-messages"
      />
      {editing && <MessageDialog id={editing.id} initial={editing.message} initialMediaName={editing.mediaName} onClose={() => setEditing(null)} />}
    </>
  );
}
