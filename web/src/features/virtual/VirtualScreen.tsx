// Obrazovka virtualnej tabule podla rozlozenia - kiosk (#/virtual/<cislo>) aj nahlad v editore rozlozenia.
import { useEffect, useMemo, useState, type CSSProperties } from "react";
import type { VirtualLayoutDto as VirtualLayout, VirtualViewDto as VirtualView } from "@/api";
import { cn } from "@/lib/utils";
import { hasText, t } from "@/i18n";
import { columnColor, columnText, columnWidth, labelKey, layoutColumns, layoutTitle } from "@/render/virtual";
import styles from "./VirtualScreen.module.css";

/** Aktualny cas, obnovovany po intervale (hodiny, striedanie a blikanie textov). */
export function useNow(interval: number): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), interval);
    return () => clearInterval(timer);
  }, [interval]);
  return now;
}

const pad = (n: number) => String(n).padStart(2, "0");

function clock(now: number, seconds: boolean): string {
  const d = new Date(now);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}${seconds ? `:${pad(d.getSeconds())}` : ""}`;
}

function date(now: number): string {
  const d = new Date(now);
  return `${d.getDate()}. ${d.getMonth() + 1}. ${d.getFullYear()}`;
}

/** Cas z XML INISSu (miestny, bez pasma) v tvare HH:MM, z ineho dna aj s datumom. */
function time(value: string | null, now: number): string {
  if (!value) return "";
  const d = new Date(now);
  const today = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const [, month, day] = value.slice(0, 10).split("-");
  return value.startsWith(today) ? value.slice(11, 16) : `${Number(day)}. ${Number(month)}. ${value.slice(11, 16)}`;
}

/** Popis stlpca: z rozlozenia, inak predvoleny podla prvku v jazyku obsluhy. */
function label(text: string, cells: string[]): string {
  if (text) return text;
  const key = `virtual.cell.${labelKey(cells)}`;
  return hasText(key) ? t(key) : labelKey(cells);
}

interface Props {
  view: VirtualView;
  layout: VirtualLayout;
  now: number;
  /** Chyba nacitania (napr. tabula neexistuje). */
  error?: string | null;
  /** Nahlad v editore - velkost podla rodica, nie celej obrazovky. */
  preview?: boolean;
}

export function VirtualScreen({ view, layout, now, error, preview }: Props) {
  const rows = view.rows;
  const columns = useMemo(() => layoutColumns(layout, rows), [layout, rows]);
  const { colors } = layout;
  const timing = layout;
  // pevny pocet zaznamov tabule urci velkost pisma; inak pocet riadkov rozlozenia
  const lines = view.board.rowCount ?? layout.rows;
  const style = {
    "--rows": lines,
    "--vb-bg": colors.background,
    "--vb-text": colors.text,
    "--vb-head-bg": colors.headBackground,
    "--vb-head-text": colors.headText,
    "--vb-accent": colors.accent,
    "--vb-muted": colors.muted,
    "--vb-line": colors.rowLine,
    "--vb-alt": colors.rowAlternate || colors.background,
    fontFamily: layout.font || undefined,
  } as CSSProperties;

  return (
    <div className={cn(styles.screen, preview && styles.preview)}>
      <div className={cn(styles.board, view.source?.stale && styles.stale)} style={style}>
        <header className={styles.head}>
          <span className={styles.title}>{layoutTitle(layout.title, view.board.name, view.source?.station)}</span>
          {layout.showStation && <span className={styles.muted}>{view.source?.station}</span>}
          <span className={styles.spacer} />
          {layout.showDate && <span className={styles.muted}>{date(now)}</span>}
          {layout.clock !== "none" && <span className={styles.clock}>{clock(now, layout.clock === "seconds")}</span>}
        </header>
        {error && <div className={styles.note}>{error}</div>}
        {!view.source && <div className={styles.note}>{t("virtual.view.missing", { name: view.board.source })}</div>}
        {view.source?.stale && <div className={styles.note}>{t("virtual.view.stale", { time: time(view.source.validTo, now) })}</div>}
        {view.source?.error && <div className={styles.note}>{t("virtual.view.error", { error: view.source.error })}</div>}
        {view.source && rows.length === 0 && <div className={styles.empty}>{t("virtual.view.empty")}</div>}
        {rows.length > 0 && (
          <table className={styles.table}>
            <colgroup>
              {columns.map((c) => (
                <col key={c.key} style={{ width: columnWidth(c.width) }} />
              ))}
            </colgroup>
            {layout.columnHeaders && (
              <thead>
                <tr>
                  {columns.map((c) => (
                    <th key={c.key} style={{ textAlign: c.align }}>
                      {label(c.label, c.cells)}
                    </th>
                  ))}
                </tr>
              </thead>
            )}
            <tbody>
              {rows.map((row, i) => (
                <tr key={i}>
                  {columns.map((c) => (
                    <td
                      key={c.key}
                      style={{
                        textAlign: c.align,
                        color: columnColor(c.color, colors),
                        fontWeight: c.bold ? 700 : undefined,
                        fontSize: c.size === 100 ? undefined : `${c.size}%`,
                        // stlpec bez sirky ma strop (styl), fill viac, stlpec so zadanou sirkou ziadny
                        maxWidth: c.width === "" ? undefined : c.width === "fill" ? "45cqw" : "none",
                      }}
                    >
                      {columnText(row, c, now, timing)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
