// Virtualne tabule: definicie (ako vo Web.config INISSView), XML tabul najdene v priecinkoch logov INISSu a rozlozenia.
import { useCallback, useMemo, useRef, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ChevronDown, Copy, ExternalLink, FolderOpen, Info, Pencil, Plus, Trash2, TriangleAlert, Upload } from "lucide-react";
import { toast } from "sonner";
import {
  deleteVirtualBoard,
  deleteVirtualLayout,
  errorText,
  getStateQueryKey,
  getVirtualOverviewOptions,
  importInissViewMutation,
  PERMISSIONS,
  queryClient,
  updateSettings,
  useCan,
  useSimulatorState,
  type InissViewImportedDto,
  type VirtualBoardDto,
  type VirtualLayoutDto,
  type VirtualOverviewDto,
  type VirtualSourceDto,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { PageHeader } from "@/components/common/page-header";
import { RowActions } from "@/components/common/row-actions";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { t } from "@/i18n";
import { layoutName, rowsText, sourceStatus, xmlDateTime } from "./layouts";
import { VirtualDialog } from "./VirtualDialog";
import { VirtualLayoutDialog } from "./VirtualLayoutDialog";

const failed = (e: unknown) => toast.error(errorText(e));

/** Vysledok nacitania Web.config INISSView a ponuka sledovat jeho priecinky. */
function ImportResult({ result, folders, onClose }: { result: InissViewImportedDto; folders: string[]; onClose: () => void }) {
  const live = useSimulatorState();
  const can = useCan();
  const missing = result.folders.filter((f) => !folders.some((g) => g.toLowerCase() === f.toLowerCase()));
  const watchFolders = async () => {
    if (!live.settings) return;
    // heslo certifikatu ostava ulozene (null)
    await updateSettings({ body: { ...live.settings, virtualFolders: [...folders, ...missing], httpsPassword: null }, throwOnError: true });
    await queryClient.invalidateQueries({ queryKey: getStateQueryKey() });
    onClose();
  };
  return (
    <Alert className="mb-4">
      <Info />
      <AlertDescription className="flex flex-col gap-2">
        <p>{t("virtual.import.done", { added: result.added, replaced: result.replaced, skipped: result.skipped })}</p>
        {result.duplicates.length > 0 && <p>{t("virtual.import.duplicates", { list: result.duplicates.join(", ") })}</p>}
        {missing.length > 0 && <p>{t("virtual.import.folders", { list: missing.join(", ") })}</p>}
        <div className="flex flex-wrap gap-2">
          {missing.length > 0 && can(PERMISSIONS.settings) && (
            <Button size="sm" onClick={() => watchFolders().catch(failed)}>
              <FolderOpen />
              {t("virtual.import.useFolders")}
            </Button>
          )}
          <Button size="sm" variant="outline" onClick={onClose}>
            {t("common.close")}
          </Button>
        </div>
      </AlertDescription>
    </Alert>
  );
}

/** Sledovane priecinky s XML tabul; bez nich odkaz do nastaveni. */
function Folders({ data }: { data: VirtualOverviewDto }) {
  const can = useCan();
  if (data.folders.length === 0)
    return (
      <Alert className="mb-4 border-amber-500/40 text-amber-800 dark:text-amber-300">
        <TriangleAlert />
        <AlertDescription className="text-current">
          {t("virtual.noFolders")}{" "}
          {can(PERMISSIONS.settings) && (
            <a className="underline" href="#/settings">
              {t("nav.settings")}
            </a>
          )}
        </AlertDescription>
      </Alert>
    );
  return (
    <p className="mb-4 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
      {t("virtual.folders")}
      {data.folders.map((f) => (
        <Badge key={f.path} variant="outline" className="font-mono font-normal">
          {f.path}
          {!f.exists && <span className="text-destructive">· {t("virtual.folder.missing")}</span>}
        </Badge>
      ))}
    </p>
  );
}

const boardColumn = columnsFor<VirtualBoardDto>();
const sourceColumn = columnsFor<VirtualSourceDto>();
const layoutColumn = columnsFor<VirtualLayoutDto>();

export function VirtualBoardsPage() {
  const edit = useCan()(PERMISSIONS.edit);
  // zmeny XML a definicii prinasa WebSocket (live.ts)
  const overview = useQuery(getVirtualOverviewOptions());
  const data = overview.data;
  const [editing, setEditing] = useState<{ board: VirtualBoardDto; isNew: boolean } | null>(null);
  const [editingLayout, setEditingLayout] = useState<{ layout: VirtualLayoutDto; isNew: boolean } | null>(null);
  const importing = useMutation({ ...importInissViewMutation(), onError: failed });
  const upload = useRef<HTMLInputElement>(null);
  const replace = useRef(false);

  const chooseFile = (overwrite: boolean) => {
    replace.current = overwrite;
    upload.current?.click();
  };
  const onImport = async (files: FileList | null) => {
    const file = files?.[0];
    if (upload.current) upload.current.value = "";
    if (!file) return;
    importing.mutate({ body: { content: await file.text(), replace: replace.current } });
  };

  const add = useCallback(
    (source?: VirtualSourceDto) => {
      const used = new Set(data?.boards.map((b) => b.id));
      let id = 1;
      while (used.has(id)) id++;
      setEditing({
        isNew: true,
        board: {
          id,
          name: source?.key ?? "",
          description: "",
          source: source?.name ?? "",
          rowCount: null,
          rowStart: 1,
          separator: "",
          template: "",
          parameter: "",
          layout: "",
        },
      });
    },
    [data],
  );

  // kopia rozlozenia - nazov s priponou, kluc s prvym volnym cislom
  const copyLayout = useCallback(
    (layout: VirtualLayoutDto) => {
      const used = new Set(data?.layouts.map((l) => l.id));
      let n = 2;
      while (used.has(`${layout.id}-${n}`)) n++;
      setEditingLayout({
        isNew: true,
        layout: { ...layout, id: `${layout.id}-${n}`, name: layoutName(layout) + t("virtual.layout.copySuffix"), builtIn: false },
      });
    },
    [data],
  );
  const removeLayout = async (layout: VirtualLayoutDto) => {
    if (await confirmAction({ title: t("virtual.layout.delete.confirm", { name: layoutName(layout) }), destructive: true }))
      deleteVirtualLayout({ path: { id: layout.id }, throwOnError: true }).catch(failed);
  };
  const removeBoard = async (board: VirtualBoardDto) => {
    if (await confirmAction({ title: t("virtual.delete.confirm", { id: board.id }), destructive: true }))
      deleteVirtualBoard({ path: { id: board.id }, throwOnError: true }).catch(failed);
  };

  const boardColumns = useMemo(() => {
    const layoutOf = (b: VirtualBoardDto) => data?.layouts.find((l) => l.id === (b.layout || data.defaultLayout));
    // sablona INISSView sa pouzije, ked je v nastaveni priecinok INISSView (server vtedy pozna sablony)
    const usesTemplate = (b: VirtualBoardDto) => b.template.length > 0 && (data?.templates.length ?? 0) > 0;
    const sourceOf = (b: VirtualBoardDto) => data?.sources.find((s) => s.name.toLowerCase() === b.source.toLowerCase());
    return [
      boardColumn.accessor("id", { header: t("virtual.col.id"), meta: { className: "w-16" } }),
      boardColumn.accessor("name", {
        header: t("virtual.col.name"),
        cell: ({ row }) => (
          <div className="flex flex-col">
            <span className="font-medium">{row.original.name}</span>
            {row.original.description && <span className="text-xs text-muted-foreground">{row.original.description}</span>}
          </div>
        ),
      }),
      boardColumn.accessor("source", {
        header: t("virtual.col.source"),
        cell: ({ row }) => {
          const [status, tone] = sourceStatus(sourceOf(row.original));
          return (
            <div className="flex flex-col items-start gap-1">
              <code className="font-mono text-xs">{row.original.source}</code>
              <StatusBadge tone={tone}>{t(status)}</StatusBadge>
            </div>
          );
        },
      }),
      boardColumn.accessor(rowsText, { id: "rows", header: t("virtual.col.rows") }),
      boardColumn.accessor("separator", {
        header: t("virtual.col.separator"),
        cell: ({ getValue }) =>
          getValue() ? (
            <code className="font-mono text-xs">{`'${getValue()}'`}</code>
          ) : (
            <span className="text-xs text-muted-foreground">{t("virtual.separator.none")}</span>
          ),
      }),
      boardColumn.accessor((b) => (usesTemplate(b) ? t("virtual.template", { name: b.template }) : layoutOf(b) ? layoutName(layoutOf(b)!) : b.layout), {
        id: "layout",
        header: t("virtual.col.layout"),
        cell: ({ row, getValue }) =>
          usesTemplate(row.original) ? (
            data?.templates.some((n) => n.toLowerCase() === row.original.template.toLowerCase()) ? (
              getValue()
            ) : (
              <Badge variant="destructive">{getValue()}</Badge>
            )
          ) : layoutOf(row.original) ? (
            getValue()
          ) : (
            <Badge variant="destructive">{row.original.layout}</Badge>
          ),
      }),
      boardColumn.display({
        id: "actions",
        meta: { className: "w-px whitespace-nowrap text-right" },
        cell: ({ row }) => (
          <div className="flex items-center justify-end gap-1">
            <Button size="sm" variant="outline" render={<a href={`#/virtual/${row.original.id}`} target="_blank" rel="noreferrer" />}>
              <ExternalLink />
              {t("virtual.open")}
            </Button>
            <RowActions
              actions={[
                { label: t("common.edit"), icon: Pencil, hidden: !edit, onClick: () => setEditing({ board: row.original, isNew: false }) },
                { label: t("common.delete"), icon: Trash2, destructive: true, hidden: !edit, onClick: () => removeBoard(row.original) },
              ]}
            />
          </div>
        ),
      }),
    ];
  }, [data, edit]);

  const sourceColumns = useMemo(
    () => [
      sourceColumn.accessor("name", {
        header: t("virtual.col.source"),
        cell: ({ row }) => (
          <div className="flex flex-col">
            <code className="font-mono text-xs">{row.original.name}</code>
            <span className="text-xs break-all whitespace-normal text-muted-foreground">{row.original.path}</span>
          </div>
        ),
      }),
      sourceColumn.accessor((s) => s.station ?? "", { id: "station", header: t("virtual.col.station") }),
      sourceColumn.accessor((s) => s.key ?? "", { id: "key", header: t("virtual.col.key") }),
      sourceColumn.accessor((s) => s.written ?? "", { id: "written", header: t("virtual.col.written"), cell: ({ row }) => xmlDateTime(row.original.written) }),
      sourceColumn.accessor((s) => s.validTo ?? "", { id: "validTo", header: t("virtual.col.validTo"), cell: ({ row }) => xmlDateTime(row.original.validTo) }),
      sourceColumn.accessor("rows", { header: t("virtual.col.records") }),
      sourceColumn.accessor((s) => t(sourceStatus(s)[0]), {
        id: "status",
        header: t("virtual.col.status"),
        cell: ({ row }) => {
          const [status, tone] = sourceStatus(row.original);
          return (
            <div className="flex flex-col items-start gap-1">
              <StatusBadge tone={tone}>{t(status)}</StatusBadge>
              {row.original.error && <span className="text-xs whitespace-normal text-muted-foreground">{row.original.error}</span>}
            </div>
          );
        },
      }),
      sourceColumn.display({
        id: "actions",
        meta: { className: "w-px text-right" },
        cell: ({ row }) =>
          edit && (
            <Button size="sm" variant="outline" onClick={() => add(row.original)}>
              <Plus />
              {t("virtual.createFor")}
            </Button>
          ),
      }),
    ],
    // add cita aktualne cisla tabul
    [add, edit],
  );

  const layoutColumns = useMemo(
    () => [
      layoutColumn.accessor(layoutName, {
        id: "name",
        header: t("virtual.layout.col.name"),
        cell: ({ row, getValue }) => (
          <span>
            {getValue()}
            {row.original.builtIn && <span className="text-xs text-muted-foreground"> · {t("virtual.layout.builtIn")}</span>}
          </span>
        ),
      }),
      layoutColumn.accessor("id", { header: t("virtual.layout.col.id"), cell: ({ getValue }) => <code className="font-mono text-xs">{getValue()}</code> }),
      layoutColumn.accessor((l) => l.columns.length, {
        id: "columns",
        header: t("virtual.layout.col.columns"),
        cell: ({ getValue }) => (getValue() === 0 ? t("virtual.layout.columnsAuto") : getValue()),
      }),
      layoutColumn.accessor(
        (l) =>
          (data?.boards ?? [])
            .filter((b) => (b.layout || data?.defaultLayout) === l.id)
            .map((b) => b.id)
            .join(", "),
        {
          id: "used",
          header: t("virtual.layout.col.used"),
        },
      ),
      layoutColumn.display({
        id: "actions",
        meta: { className: "w-10 text-right" },
        cell: ({ row }) => (
          <RowActions
            actions={[
              { label: t("virtual.layout.copy"), icon: Copy, hidden: !edit, onClick: () => copyLayout(row.original) },
              {
                label: t("common.edit"),
                icon: Pencil,
                hidden: !edit || row.original.builtIn,
                onClick: () => setEditingLayout({ layout: row.original, isNew: false }),
              },
              { label: t("common.delete"), icon: Trash2, destructive: true, hidden: !edit || row.original.builtIn, onClick: () => removeLayout(row.original) },
            ]}
          />
        ),
      }),
    ],
    [copyLayout, data, edit],
  );

  if (!data)
    return overview.error ? (
      <Alert variant="destructive">
        <AlertDescription>{errorText(overview.error)}</AlertDescription>
      </Alert>
    ) : null;
  const folders = data.folders.map((f) => f.path);

  return (
    <>
      <PageHeader description={t("virtual.hint")}>
        {edit && (
          <>
            <input ref={upload} type="file" accept=".config,.xml" hidden onChange={(e) => onImport(e.target.files)} />
            <DropdownMenu>
              <DropdownMenuTrigger render={<Button variant="outline" disabled={importing.isPending} />}>
                <Upload />
                {t("virtual.import")}
                <ChevronDown />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => chooseFile(false)}>{t("virtual.import.add")}</DropdownMenuItem>
                <DropdownMenuItem variant="destructive" onClick={() => chooseFile(true)}>
                  {t("virtual.import.overwrite")}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
            <Button onClick={() => add()}>
              <Plus />
              {t("virtual.add")}
            </Button>
          </>
        )}
      </PageHeader>
      {importing.data && <ImportResult result={importing.data} folders={folders} onClose={() => importing.reset()} />}
      <Folders data={data} />

      <Tabs defaultValue="boards">
        <TabsList>
          <TabsTrigger value="boards">
            {t("virtual.section.boards")} <Badge variant="secondary">{data.boards.length}</Badge>
          </TabsTrigger>
          <TabsTrigger value="sources">
            {t("virtual.sources.title")} <Badge variant="secondary">{data.sources.length}</Badge>
          </TabsTrigger>
          <TabsTrigger value="layouts">
            {t("virtual.layouts.title")} <Badge variant="secondary">{data.layouts.length}</Badge>
          </TabsTrigger>
        </TabsList>
        <TabsContent value="boards" className="pt-2">
          <DataTable columns={boardColumns} data={data.boards} getRowId={(b) => String(b.id)} pageSize={null} empty={t("virtual.boards.empty")} />
        </TabsContent>
        <TabsContent value="sources" className="pt-2">
          <DataTable columns={sourceColumns} data={data.sources} getRowId={(s) => s.name} pageSize={null} empty={t("virtual.sources.empty")} />
        </TabsContent>
        <TabsContent value="layouts" className="flex flex-col gap-3 pt-2">
          <p className="text-sm text-muted-foreground">{t("virtual.layouts.hint")}</p>
          <DataTable columns={layoutColumns} data={data.layouts} getRowId={(l) => l.id} pageSize={null} search={false} />
        </TabsContent>
      </Tabs>

      {editing && (
        <VirtualDialog
          initial={editing.board}
          isNew={editing.isNew}
          sources={data.sources}
          layouts={data.layouts}
          defaultLayout={data.defaultLayout}
          templates={data.templates}
          onClose={() => setEditing(null)}
        />
      )}
      {editingLayout && (
        <VirtualLayoutDialog initial={editingLayout.layout} isNew={editingLayout.isNew} boards={data.boards} onClose={() => setEditingLayout(null)} />
      )}
    </>
  );
}
