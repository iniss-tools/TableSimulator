// Hlavicka stlpca: kliknutie triedi vzostupne, zostupne a znova podla povodneho poradia.
import type { Column, RowData } from "@tanstack/react-table";
import { ArrowDown, ArrowUp, ChevronsUpDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";
import type { DataTableFeatures } from "./features";

export function DataTableColumnHeader<TData extends RowData, TValue>({
  column,
  title,
}: {
  column: Column<DataTableFeatures, TData, TValue>;
  title: string;
}) {
  if (!column.getCanSort()) return <>{title}</>;
  const sorted = column.getIsSorted();
  const Icon = sorted === "asc" ? ArrowUp : sorted === "desc" ? ArrowDown : ChevronsUpDown;
  return (
    <Button
      variant="ghost"
      size="sm"
      className="-ml-2 h-7 px-2 font-medium text-foreground"
      title={t("table.sort")}
      onClick={column.getToggleSortingHandler()}
    >
      {title}
      <Icon className={sorted ? "" : "text-muted-foreground/60"} />
    </Button>
  );
}
