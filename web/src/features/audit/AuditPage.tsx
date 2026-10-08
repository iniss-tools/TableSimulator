// Audit - kto co v simulatore urobil (kazda zmena aj neuspesne prihlasenie); filtruje server, starsie po strankach.
import { useCallback, useEffect, useMemo, useState } from "react";
import { CircleX, History, RefreshCw } from "lucide-react";
import { errorText, getAudit, type AuditEntryDto } from "@/api";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { formatDateTimeParts } from "@/lib/format";
import { hasText, t } from "@/i18n";

const PAGE = 100;

/** Akcie auditu - mena prikazov servera. */
const ACTIONS = [
  "Login",
  "Logout",
  "Setup",
  "ChangePassword",
  "CreateUser",
  "UpdateUser",
  "SetUserPassword",
  "UnlockUser",
  "DeleteUser",
  "CreateAccessKey",
  "DeleteAccessKey",
  "UpdateSettings",
  "SetLine",
  "DeleteLine",
  "SetBoard",
  "DeleteBoard",
  "ClearBoard",
  "SetBoardFault",
  "ClearLog",
  "ImportStation",
  "ImportMonitorProject",
  "StartRecording",
  "StopRecording",
  "UploadRecording",
  "DeleteRecording",
  "PlayRecording",
  "StopReplay",
  "SetVirtualBoard",
  "DeleteVirtualBoard",
  "ImportInissView",
  "SetVirtualLayout",
  "DeleteVirtualLayout",
  "ResetAdmin",
  "ImportConfig",
];

function actionText(action: string): string {
  const key = `audit.action.${action}`;
  return hasText(key) ? t(key) : action;
}

function userText(user: string): string {
  return user === "-" ? t("audit.anonymous") : user === "(console)" ? t("audit.console") : user;
}

const column = columnsFor<AuditEntryDto>();

export function AuditPage() {
  const [entries, setEntries] = useState<AuditEntryDto[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [user, setUser] = useState("");
  const [action, setAction] = useState("");
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(
    (before?: number) => {
      getAudit({ query: { before, user: user.trim() || undefined, action: action || undefined, limit: PAGE }, throwOnError: true })
        .then(({ data: page }) => {
          setEntries((old) => (before ? [...old, ...page.entries] : page.entries));
          setHasMore(page.hasMore);
          setError(null);
        })
        .catch((e) => setError(errorText(e)));
    },
    [user, action],
  );
  useEffect(() => load(), [load]);

  const columns = useMemo(
    () => [
      column.accessor("time", {
        header: t("audit.col.time"),
        // datum a cas sa v uzkej tabulke zalomia pod seba
        cell: ({ getValue }) => (
          <span className="flex flex-wrap gap-x-1">
            {formatDateTimeParts(getValue()).map((part, i) => (
              <span key={i}>{part}</span>
            ))}
          </span>
        ),
      }),
      column.accessor((e) => userText(e.user), {
        id: "user",
        header: t("audit.col.user"),
        cell: ({ row }) =>
          row.original.user === "-" || row.original.user === "(console)" ? (
            <span className="text-muted-foreground">{userText(row.original.user)}</span>
          ) : (
            <code className="font-mono text-xs">{row.original.user}</code>
          ),
      }),
      column.accessor((e) => actionText(e.action), { id: "action", header: t("audit.col.action"), meta: { className: "whitespace-normal" } }),
      column.accessor("target", { header: t("audit.col.target"), meta: { className: "whitespace-normal break-words" } }),
      column.accessor((e) => (e.success ? t("audit.ok") : (e.error ?? "")), {
        id: "result",
        header: t("audit.col.result"),
        cell: ({ row }) => {
          const e = row.original;
          if (e.success) return <StatusBadge tone="ok">{t("audit.ok")}</StatusBadge>;
          const key = `error.${e.error}`;
          // text chyby moze byt dlhy - zalomi sa namiesto stitku, aby tabulka nemusela posuvat do strany
          return (
            <span className="flex max-w-md min-w-40 items-start gap-1.5 whitespace-normal text-red-700 dark:text-red-400">
              <CircleX className="mt-0.5 size-3.5 shrink-0" />
              {hasText(key) ? t(key) : e.error}
            </span>
          );
        },
      }),
      column.accessor((e) => e.address ?? "", {
        id: "address",
        header: t("audit.col.address"),
        cell: ({ getValue }) => <span className="text-xs text-muted-foreground">{getValue()}</span>,
      }),
      column.accessor("details", {
        header: t("audit.col.details"),
        enableSorting: false,
        cell: ({ getValue }) =>
          getValue() && (
            <Popover>
              <PopoverTrigger render={<Button variant="link" size="sm" className="h-auto p-0" />}>{t("audit.show")}</PopoverTrigger>
              <PopoverContent align="end" className="w-[min(36rem,90vw)]">
                <pre className="max-h-80 overflow-auto font-mono text-xs break-all whitespace-pre-wrap">{getValue()}</pre>
              </PopoverContent>
            </Popover>
          ),
      }),
    ],
    [],
  );

  return (
    <>
      <PageHeader description={t("audit.hint")} />
      {error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
      <DataTable
        columns={columns}
        data={entries}
        getRowId={(e) => String(e.id)}
        search={false}
        pageSize={null}
        alignTop
        empty={t("audit.empty")}
        rowClassName={(e) => (e.success ? undefined : "bg-destructive/5")}
        toolbar={
          <>
            <Input
              className="max-w-56"
              placeholder={t("audit.filter.user")}
              aria-label={t("audit.filter.user")}
              value={user}
              onChange={(e) => setUser(e.target.value)}
            />
            <OptionSelect
              value={action}
              onValueChange={setAction}
              options={[{ value: "", label: t("audit.filter.allActions") }, ...ACTIONS.map((a) => ({ value: a, label: actionText(a) }))]}
              ariaLabel={t("audit.col.action")}
              className="min-w-48"
            />
            <Button variant="outline" onClick={() => load()}>
              <RefreshCw />
              {t("audit.refresh")}
            </Button>
          </>
        }
      />
      {hasMore && (
        <Button variant="outline" className="mt-3" onClick={() => load(entries[entries.length - 1]?.id)}>
          <History />
          {t("audit.more")}
        </Button>
      )}
    </>
  );
}
