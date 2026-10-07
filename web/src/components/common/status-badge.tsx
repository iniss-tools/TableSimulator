// Stav ako farebny stitok: ok (zelena), wait (oranzova), bad (cervena), off (siva).
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";

export type Tone = "ok" | "wait" | "bad" | "off";

const TONES: Record<Tone, string> = {
  ok: "border-emerald-500/40 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400",
  wait: "border-amber-500/40 bg-amber-500/10 text-amber-700 dark:text-amber-400",
  bad: "border-red-500/40 bg-red-500/10 text-red-700 dark:text-red-400",
  off: "text-muted-foreground",
};

export function StatusBadge({ tone, children, title }: { tone: Tone; children: ReactNode; title?: string }) {
  return (
    <Badge variant="outline" className={cn("whitespace-nowrap", TONES[tone])} title={title}>
      {children}
    </Badge>
  );
}
