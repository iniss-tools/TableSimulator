// Vrstvy cela (ako vo Figme): prvky od vrchneho (kresli sa posledny) po spodny, vyber, zamok, skrytie v editore
// a zmena poradia tahanim. Vybrany prvok sa posunie do pohladu len v zozname, stranka sa neposuva.
import { useEffect, useRef, useState, type DragEvent } from "react";
import { Eye, EyeOff, Lock, LockOpen } from "lucide-react";
import type { FaceElementDto } from "@/api";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { KIND_ICONS, layerLabel } from "./faces";

interface Props {
  elements: FaceElementDto[];
  selected: number | null;
  hidden: ReadonlySet<number>;
  readOnly: boolean;
  onSelect: (index: number) => void;
  onToggleLock: (index: number) => void;
  onToggleHidden: (index: number) => void;
  /** Nove poradie kreslenia: povodne indexy prvkov. */
  onReorder: (order: number[]) => void;
}

/** Miesto vlozenia pri tahani: pred alebo za riadkom v zozname (zhora). */
interface Drop {
  row: number;
  after: boolean;
}

export function LayersPanel({ elements, selected, hidden, readOnly, onSelect, onToggleLock, onToggleHidden, onReorder }: Props) {
  const list = useRef<HTMLUListElement>(null);
  const rows = useRef(new Map<number, HTMLLIElement>());
  const [dragged, setDragged] = useState<number | null>(null);
  const [drop, setDrop] = useState<Drop | null>(null);
  // zoznam zhora: vrchny prvok (posledny v poradi kreslenia) prvy
  const shown = elements.map((_, i) => elements.length - 1 - i);

  // vybrany prvok do pohladu - posuva sa len zoznam
  useEffect(() => {
    const box = list.current;
    const row = selected === null ? undefined : rows.current.get(selected);
    if (!box || !row) return;
    if (row.offsetTop < box.scrollTop) box.scrollTop = row.offsetTop;
    else if (row.offsetTop + row.offsetHeight > box.scrollTop + box.clientHeight) box.scrollTop = row.offsetTop + row.offsetHeight - box.clientHeight;
  }, [selected]);

  const over = (e: DragEvent<HTMLLIElement>, row: number) => {
    if (dragged === null) return;
    e.preventDefault();
    const rect = e.currentTarget.getBoundingClientRect();
    setDrop({ row, after: e.clientY > rect.top + rect.height / 2 });
  };

  const finish = () => {
    if (dragged !== null && drop) {
      const order = shown.filter((i) => i !== dragged);
      const target = order.indexOf(shown[drop.row]);
      // riadok, nad ktorym sa pusti, este moze byt sam tahany prvok
      const at = target < 0 ? shown.indexOf(dragged) : target + (drop.after ? 1 : 0);
      order.splice(at, 0, dragged);
      const drawn = order.reverse();
      if (drawn.some((v, i) => v !== i)) onReorder(drawn);
    }
    setDragged(null);
    setDrop(null);
  };

  if (elements.length === 0) return <p className="text-sm text-muted-foreground">{t("faces.layers.empty")}</p>;

  return (
    <ul ref={list} aria-label={t("faces.layers")} className="relative -mx-1 min-h-0 flex-1 overflow-y-auto">
      {shown.map((index, row) => {
        const element = elements[index];
        const Icon = KIND_ICONS[element.kind];
        const isHidden = hidden.has(index);
        return (
          <li
            key={index}
            ref={(el) => {
              if (el) rows.current.set(index, el);
              else rows.current.delete(index);
            }}
            draggable={!readOnly}
            onDragStart={(e) => {
              e.dataTransfer.effectAllowed = "move";
              e.dataTransfer.setData("text/plain", String(index));
              setDragged(index);
            }}
            onDragOver={(e) => over(e, row)}
            onDrop={(e) => {
              e.preventDefault();
              finish();
            }}
            onDragEnd={() => {
              setDragged(null);
              setDrop(null);
            }}
            className={cn(
              "group relative flex h-8 items-center gap-1 rounded-md px-1",
              selected === index ? "bg-accent text-accent-foreground" : "hover:bg-muted",
              dragged === index && "opacity-50",
              drop?.row === row && (drop.after ? "shadow-[inset_0_-2px_0_var(--color-primary)]" : "shadow-[inset_0_2px_0_var(--color-primary)]"),
            )}
          >
            <button
              type="button"
              aria-pressed={selected === index}
              className={cn(
                "flex min-w-0 flex-1 items-center gap-2 text-left text-sm outline-none focus-visible:underline",
                isHidden && "text-muted-foreground",
              )}
              onClick={() => onSelect(index)}
            >
              <Icon className="size-3.5 shrink-0 text-muted-foreground" />
              <span className="truncate">{layerLabel(element)}</span>
            </button>
            <button
              type="button"
              title={t(element.locked ? "faces.element.unlock" : "faces.element.lock")}
              aria-label={t(element.locked ? "faces.element.unlock" : "faces.element.lock")}
              aria-pressed={element.locked}
              disabled={readOnly}
              className={cn(
                "rounded p-1 text-muted-foreground hover:text-foreground focus-visible:opacity-100 disabled:pointer-events-none",
                element.locked ? "opacity-100" : "opacity-0 group-hover:opacity-100",
              )}
              onClick={() => onToggleLock(index)}
            >
              {element.locked ? <Lock className="size-3.5" /> : <LockOpen className="size-3.5" />}
            </button>
            <button
              type="button"
              title={t(isHidden ? "faces.element.show" : "faces.element.hide")}
              aria-label={t(isHidden ? "faces.element.show" : "faces.element.hide")}
              aria-pressed={isHidden}
              className={cn(
                "rounded p-1 text-muted-foreground hover:text-foreground focus-visible:opacity-100",
                isHidden ? "opacity-100" : "opacity-0 group-hover:opacity-100",
              )}
              onClick={() => onToggleHidden(index)}
            >
              {isHidden ? <EyeOff className="size-3.5" /> : <Eye className="size-3.5" />}
            </button>
          </li>
        );
      })}
    </ul>
  );
}
