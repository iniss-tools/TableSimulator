// Vyber zobrazenych stlpcov tabulky.
import type { ReactTable, RowData } from "@tanstack/react-table";
import { Columns3 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { t } from "@/i18n";
import type { DataTableFeatures } from "./features";

export function DataTableViewOptions<TData extends RowData>({ table }: { table: ReactTable<DataTableFeatures, TData> }) {
  const columns = table.getAllLeafColumns().filter((column) => column.getCanHide());
  if (columns.length === 0) return null;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="outline" className="ml-auto" />}>
        <Columns3 />
        {t("table.columns")}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-44">
        <DropdownMenuGroup>
          <DropdownMenuLabel>{t("table.columns")}</DropdownMenuLabel>
          {columns.map((column) => {
            const def = column.columnDef;
            const label = def.meta?.label ?? (typeof def.header === "string" ? def.header : column.id);
            return (
              <DropdownMenuCheckboxItem key={column.id} checked={column.getIsVisible()} onCheckedChange={(value) => column.toggleVisibility(!!value)}>
                {label}
              </DropdownMenuCheckboxItem>
            );
          })}
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
