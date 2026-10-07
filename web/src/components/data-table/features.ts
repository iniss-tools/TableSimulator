// Funkcie tabuliek (TanStack Table v9) - co nie je v zozname, do zostavy nejde.
import {
  columnFilteringFeature,
  columnVisibilityFeature,
  createColumnHelper,
  createFilteredRowModel,
  createPaginatedRowModel,
  createSortedRowModel,
  filterFn_includesString,
  globalFilteringFeature,
  rowPaginationFeature,
  rowSortingFeature,
  sortFn_alphanumeric,
  sortFn_basic,
  sortFn_datetime,
  sortFn_text,
  tableFeatures,
  type ColumnDef,
  type RowData,
} from "@tanstack/react-table";

/** Doplnky stlpca pre DataTable. */
export interface DataTableColumnMeta {
  /** Nazov stlpca vo vybere stlpcov (inak hlavicka, ak je text). */
  label?: string;
  /** Triedy bunky aj hlavicky (sirka, zarovnanie). */
  className?: string;
}

export const features = tableFeatures({
  columnFilteringFeature,
  globalFilteringFeature,
  columnVisibilityFeature,
  rowPaginationFeature,
  rowSortingFeature,
  filteredRowModel: createFilteredRowModel(),
  paginatedRowModel: createPaginatedRowModel(),
  sortedRowModel: createSortedRowModel(),
  filterFns: { includesString: filterFn_includesString },
  sortFns: { alphanumeric: sortFn_alphanumeric, text: sortFn_text, basic: sortFn_basic, datetime: sortFn_datetime },
  columnMeta: {} as DataTableColumnMeta,
});

export type DataTableFeatures = typeof features;

// stlpce maju rozne typy hodnot - pole stlpcov ich musi pripustit vsetky
export type DataTableColumn<TData extends RowData> = ColumnDef<DataTableFeatures, TData, any>;

/** Pomocnik na stlpce s typom hodnoty z riadku. */
export function columnsFor<TData extends RowData>() {
  return createColumnHelper<DataTableFeatures, TData>();
}
