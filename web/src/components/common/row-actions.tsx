// Akcie riadku tabulky v ponuke "…" (uprava, odstranenie a podobne).
import type { LucideIcon } from "lucide-react";
import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { t } from "@/i18n";

export interface RowAction {
  label: string;
  icon?: LucideIcon;
  /** Moze vratit sluz (async) - chyby riesi sama. */
  onClick: () => unknown;
  /** Akcia nieco zmaze - cervena a oddelena od ostatnych. */
  destructive?: boolean;
  disabled?: boolean;
  hidden?: boolean;
}

export function RowActions({ actions }: { actions: RowAction[] }) {
  const shown = actions.filter((a) => !a.hidden);
  if (shown.length === 0) return null;
  const normal = shown.filter((a) => !a.destructive);
  const destructive = shown.filter((a) => a.destructive);
  const item = (a: RowAction) => (
    <DropdownMenuItem key={a.label} variant={a.destructive ? "destructive" : "default"} disabled={a.disabled} onClick={() => void a.onClick()}>
      {a.icon && <a.icon />}
      {a.label}
    </DropdownMenuItem>
  );
  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" title={t("common.actions")} />}>
        <MoreHorizontal />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-40">
        {normal.map(item)}
        {normal.length > 0 && destructive.length > 0 && <DropdownMenuSeparator />}
        {destructive.map(item)}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
