// Obsah virtualnej tabule: stranka zo sablony XSL INISSView (ak ju tabula ma), inak podla rozlozenia simulatora.
import type { VirtualViewDto } from "@/api";
import { t, type TextKey } from "@/i18n";
import { TemplateFrame } from "./TemplateFrame";
import { VirtualScreen } from "./VirtualScreen";

interface VirtualDisplayProps {
  view: VirtualViewDto;
  now: number;
  /** Chyba nacitania (posledny obsah ostava). */
  error?: string | null;
  /** Nahlad - stranka sablony zmensena do miesta. */
  preview?: boolean;
}

export function VirtualDisplay({ view, now, error, preview }: VirtualDisplayProps) {
  const page = view.page;
  if (!page) return <VirtualScreen view={view} layout={view.layout} now={now} error={error} />;
  if (page.html !== null) return <TemplateFrame html={page.html} title={view.board.name} preview={preview} />;
  return (
    <div className="flex size-full items-center justify-center bg-black p-6 text-center text-sm text-[#ffd0d0]">
      {t(`virtual.page.${page.error ?? "failed"}` as TextKey, { detail: page.detail ?? "" })}
    </div>
  );
}
