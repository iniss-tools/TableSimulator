// Tabulka udajov (TanStack Table v9 podla shadcn/ui): triedenie kliknutim na hlavicku, hladanie vo vsetkych stlpcoch,
// strankovanie a volitelne vyber stlpcov (zapamata si ho prehliadac).
import { useState, type ReactNode } from "react";
import { useTable, type ColumnVisibilityState, type RowData, type SortingState } from "@tanstack/react-table";
import { Search } from "lucide-react";
import { cn } from "@/lib/utils";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { t } from "@/i18n";
import { DataTableColumnHeader } from "./column-header";
import { DataTablePagination } from "./pagination";
import { DataTableViewOptions } from "./view-options";
import { features, type DataTableColumn } from "./features";

/** Bez strankovania - vsetky riadky na jednej strane. */
const ALL_ROWS = 1_000_000;

export interface DataTableProps<TData extends RowData> {
  columns: DataTableColumn<TData>[];
  data: TData[];
  getRowId?: (row: TData) => string;
  /** Pole hladania vo vsetkych stlpcoch s textom; false = bez neho. */
  search?: boolean;
  /** Dalsie ovladanie vedla hladania (filtre, tlacidla). */
  toolbar?: ReactNode;
  /** Riadkov na stranu; null = bez strankovania. */
  pageSize?: number | null;
  /** Co ukazat, ked tabulka nema ziadne riadky (nie ked hladanie nic nenaslo). */
  empty?: ReactNode;
  rowClassName?: (row: TData) => string | undefined;
  initialSorting?: SortingState;
  /** Kluc v prehliadaci pre vyber stlpcov; bez neho sa vyber neponuka. */
  storageKey?: string;
  /** Triedenie kliknutim na hlavicku; false = poradie urcuje zdroj (napr. server po strankach). */
  sortable?: boolean;
  /** Bunky zarovnane hore - pre viacriadkove zaznamy. */
  alignTop?: boolean;
  className?: string;
}

function loadVisibility(storageKey: string | undefined): ColumnVisibilityState {
  if (!storageKey) return {};
  try {
    return JSON.parse(localStorage.getItem(`table.${storageKey}`) ?? "{}") as ColumnVisibilityState;
  } catch {
    return {};
  }
}

export function DataTable<TData extends RowData>({
  columns,
  data,
  getRowId,
  search = true,
  toolbar,
  pageSize = 25,
  empty,
  rowClassName,
  initialSorting = [],
  storageKey,
  sortable = true,
  alignTop = false,
  className,
}: DataTableProps<TData>) {
  const [sorting, setSorting] = useState<SortingState>(initialSorting);
  const [globalFilter, setGlobalFilter] = useState("");
  const [columnVisibility, setColumnVisibility] = useState<ColumnVisibilityState>(() => loadVisibility(storageKey));

  const table = useTable({
    features,
    data,
    columns,
    getRowId: getRowId ? (row) => getRowId(row) : undefined,
    globalFilterFn: "includesString",
    enableSorting: sortable,
    onSortingChange: setSorting,
    onGlobalFilterChange: setGlobalFilter,
    onColumnVisibilityChange: (update) =>
      setColumnVisibility((old) => {
        const next = typeof update === "function" ? update(old) : update;
        try {
          if (storageKey) localStorage.setItem(`table.${storageKey}`, JSON.stringify(next));
        } catch {
          // ulozisko nedostupne
        }
        return next;
      }),
    initialState: { pagination: { pageIndex: 0, pageSize: pageSize ?? ALL_ROWS } },
    state: { sorting, globalFilter, columnVisibility },
  });

  const rows = table.getRowModel().rows;
  const visibleColumns = table.getVisibleLeafColumns().length;
  const showToolbar = search || toolbar || storageKey;

  return (
    <div className={cn("flex flex-col gap-3", className)}>
      {showToolbar && (
        <div className="flex flex-wrap items-center gap-2">
          {search && (
            <div className="relative w-full max-w-xs">
              <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={globalFilter}
                onChange={(e) => setGlobalFilter(e.target.value)}
                placeholder={t("common.search")}
                aria-label={t("common.search")}
                className="pl-8"
              />
            </div>
          )}
          {toolbar}
          {storageKey && <DataTableViewOptions table={table} />}
        </div>
      )}
      <div className="overflow-hidden rounded-lg border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  const def = header.column.columnDef;
                  return (
                    <TableHead key={header.id} className={def.meta?.className}>
                      {header.isPlaceholder ? null : typeof def.header === "string" ? (
                        <DataTableColumnHeader column={header.column} title={def.header} />
                      ) : (
                        <table.FlexRender header={header} />
                      )}
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {rows.length ? (
              rows.map((row) => (
                <TableRow key={row.id} className={rowClassName?.(row.original)}>
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className={cn(alignTop && "align-top", cell.column.columnDef.meta?.className)}>
                      <table.FlexRender cell={cell} />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={visibleColumns} className="h-20 text-center text-muted-foreground">
                  {data.length === 0 ? (empty ?? t("table.empty")) : t("table.noResults")}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      {pageSize !== null && <DataTablePagination table={table} />}
    </div>
  );
}
