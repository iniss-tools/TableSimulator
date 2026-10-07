// Strankovanie tabulky: pocet riadkov, riadkov na stranu a posun po stranach (skryte, ak je len jedna strana).
import type { ReactTable, RowData } from "@tanstack/react-table";
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { OptionSelect } from "@/components/common/option-select";
import { t } from "@/i18n";
import type { DataTableFeatures } from "./features";

const PAGE_SIZES = [10, 25, 50, 100];

export function DataTablePagination<TData extends RowData>({ table }: { table: ReactTable<DataTableFeatures, TData> }) {
  const { pageIndex, pageSize } = table.state.pagination;
  const pages = table.getPageCount();
  const count = table.getFilteredRowModel().rows.length;
  if (count <= PAGE_SIZES[0]) return null;

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 text-sm">
      <span className="text-muted-foreground">{t("table.count", { count })}</span>
      <div className="flex flex-wrap items-center gap-4">
        <div className="flex items-center gap-2">
          <span>{t("table.rowsPerPage")}</span>
          <OptionSelect
            value={pageSize}
            onValueChange={(size) => table.setPageSize(size)}
            options={PAGE_SIZES.map((size) => ({ value: size, label: String(size) }))}
            size="sm"
            ariaLabel={t("table.rowsPerPage")}
          />
        </div>
        <span>{t("table.page", { page: pageIndex + 1, pages: Math.max(1, pages) })}</span>
        <div className="flex items-center gap-1">
          <Button variant="outline" size="icon-sm" onClick={() => table.setPageIndex(0)} disabled={!table.getCanPreviousPage()} title={t("table.first")}>
            <ChevronsLeft />
          </Button>
          <Button variant="outline" size="icon-sm" onClick={() => table.previousPage()} disabled={!table.getCanPreviousPage()} title={t("table.previous")}>
            <ChevronLeft />
          </Button>
          <Button variant="outline" size="icon-sm" onClick={() => table.nextPage()} disabled={!table.getCanNextPage()} title={t("table.next")}>
            <ChevronRight />
          </Button>
          <Button variant="outline" size="icon-sm" onClick={() => table.setPageIndex(pages - 1)} disabled={!table.getCanNextPage()} title={t("table.last")}>
            <ChevronsRight />
          </Button>
        </div>
      </div>
    </div>
  );
}
