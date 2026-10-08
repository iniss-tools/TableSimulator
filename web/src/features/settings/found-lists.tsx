// Co server nasiel v priecinkoch nastavenia: pocet ako odkaz, zoznam az v okne (pisma ELEN, vzhlady tabul).
import { useMemo, useState, type ReactNode } from "react";
import type { DesignInfoDto } from "@/api";
import { StatusBadge } from "@/components/common/status-badge";
import { DataTable } from "@/components/data-table/data-table";
import { columnsFor } from "@/components/data-table/features";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { t } from "@/i18n";

interface ListDialogProps {
  /** Text odkazu, ktory okno otvori. */
  link: string;
  title: string;
  description: ReactNode;
  children: ReactNode;
}

function ListDialog({ link, title, description, children }: ListDialogProps) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button type="button" variant="link" size="sm" className="h-auto p-0 font-normal" onClick={() => setOpen(true)}>
        {link}
      </Button>
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-h-[calc(100dvh-2rem)] grid-rows-[auto_minmax(0,1fr)_auto] sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>
          <div className="-mx-4 min-h-0 overflow-y-auto px-4">{children}</div>
          <DialogFooter showCloseButton />
        </DialogContent>
      </Dialog>
    </>
  );
}

const fontColumn = columnsFor<{ name: string }>();
const designColumn = columnsFor<DesignInfoDto>();

/** Pocet najdenych pisiem ELEN; zoznam suborov v okne. */
export function FontsFound({ fonts, folder }: { fonts: string[]; folder: string }) {
  const columns = useMemo(
    () => [
      fontColumn.accessor("name", { header: t("settings.fonts.col.file"), cell: ({ getValue }) => <code className="font-mono text-xs">{getValue()}</code> }),
    ],
    [],
  );
  const rows = useMemo(() => fonts.map((name) => ({ name })), [fonts]);
  if (fonts.length === 0) return t("settings.noFonts");
  return (
    <ListDialog
      link={t("settings.fontsFound", { count: fonts.length })}
      title={t("settings.fonts.title")}
      description={t("settings.fonts.description", { folder })}
    >
      <DataTable columns={columns} data={rows} getRowId={(f) => f.name} pageSize={null} />
    </ListDialog>
  );
}

/** Pocet vzhladov (vstavane aj vlastne); zoznam v okne. */
export function DesignsFound({ designs }: { designs: DesignInfoDto[] }) {
  const columns = useMemo(
    () => [
      designColumn.accessor("name", { header: t("settings.designs.col.name") }),
      designColumn.accessor("id", { header: t("settings.designs.col.id"), cell: ({ getValue }) => <code className="font-mono text-xs">{getValue()}</code> }),
      designColumn.accessor((d) => (d.builtIn ? t("settings.designs.builtIn") : t("settings.designs.custom")), {
        id: "origin",
        header: t("settings.designs.col.origin"),
        cell: ({ row }) => (
          <StatusBadge tone={row.original.builtIn ? "off" : "ok"}>
            {row.original.builtIn ? t("settings.designs.builtIn") : t("settings.designs.custom")}
          </StatusBadge>
        ),
      }),
    ],
    [],
  );
  return (
    <ListDialog
      link={t("settings.designsFound", { count: designs.length })}
      title={t("settings.designs.title")}
      description={t("settings.designs.description")}
    >
      <DataTable columns={columns} data={designs} getRowId={(d) => d.id} pageSize={null} search={false} />
    </ListDialog>
  );
}
