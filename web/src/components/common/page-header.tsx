// Zaciatok stranky pod zahlavim ramu: kratky popis vlavo, akcie stranky vpravo (nadpis ma ram).
import type { ReactNode } from "react";

export function PageHeader({ description, children }: { description?: ReactNode; children?: ReactNode }) {
  return (
    <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
      <div className="max-w-3xl text-sm text-muted-foreground">{description}</div>
      {children && <div className="flex flex-wrap items-center gap-2">{children}</div>}
    </div>
  );
}

/** Nadpis casti stranky. */
export function SectionTitle({ children, actions }: { children: ReactNode; actions?: ReactNode }) {
  return (
    <div className="mt-8 mb-3 flex flex-wrap items-center justify-between gap-2">
      <h2 className="text-base font-semibold">{children}</h2>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  );
}
