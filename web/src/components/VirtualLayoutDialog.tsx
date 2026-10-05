// Editor rozlozenia virtualnej tabule s nahladom na udajoch vybranej tabule.
import { useEffect, useState } from "react";
import {
  api,
  ApiError,
  type VirtualAlign,
  type VirtualBoard,
  type VirtualClock,
  type VirtualColors,
  type VirtualColumn,
  type VirtualLayout,
  type VirtualView,
} from "../api";
import { hasText, t, type TextKey } from "../i18n";
import { CELL_NAMES, defaultColumn, layoutColumns } from "../render/virtual";
import { Dialog, Field } from "./Dialog";
import { useNow, VirtualScreen } from "./VirtualScreen";

/** Nazov rozlozenia pre obsluhu - vstavane v jazyku obsluhy. */
export function layoutName(layout: VirtualLayout): string {
  const key = `vlayout.${layout.id}`;
  return layout.builtIn && hasText(key) ? t(key) : layout.name;
}

const COLORS: (keyof VirtualColors)[] = ["background", "text", "headBackground", "headText", "accent", "muted", "delay", "rowLine"];
const CLOCKS: VirtualClock[] = ["none", "minutes", "seconds"];
const ALIGNS: VirtualAlign[] = ["left", "center", "right"];

/** Ukazkove udaje, ak ziadna virtualna tabula nie je. */
function sample(layout: VirtualLayout): VirtualView {
  const row = (time: string, type: string, number: string, destination: string, track: string, delay = "") => ({
    cells: [
      { name: "type", text: type },
      { name: "number", text: number },
      { name: "time", text: time },
      { name: "delay", text: delay },
      { name: "track", text: track },
      { name: "destination", text: destination },
    ],
  });
  return {
    board: { id: 0, name: "Odchody", description: "", source: "", rowCount: null, rowStart: 1, separator: "", template: "", parameter: "", layout: layout.id },
    source: { name: "", path: "", modified: "", station: "Dolné Mesto", key: null, written: null, validTo: null, rows: 4, stale: false, error: null },
    rows: [
      row("11:35", "Os", "7813", "Horné Mesto", "2", "5"),
      row("11:52", "R", "601", "Veľká Ves#Bratislava", "1"),
      row("12:10", "REX", "1771", "Nové Zámky", "3#"),
      row("12:35", "Os", "7815", "Horné Mesto", "2"),
    ],
    layout,
  };
}

interface Props {
  initial: VirtualLayout;
  isNew: boolean;
  boards: VirtualBoard[];
  onClose: () => void;
}

export function VirtualLayoutDialog({ initial, isNew, boards, onClose }: Props) {
  const [layout, setLayout] = useState(initial);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [boardId, setBoardId] = useState<number | null>(boards.find((b) => b.layout === initial.id)?.id ?? boards[0]?.id ?? null);
  const [view, setView] = useState<VirtualView | null>(null);
  const now = useNow(250);

  useEffect(() => {
    if (boardId === null) setView(null);
    else
      api
        .virtualView(boardId)
        .then(setView)
        .catch(() => setView(null));
  }, [boardId]);

  const set = (patch: Partial<VirtualLayout>) => setLayout({ ...layout, ...patch });
  const setColor = (key: keyof VirtualColors, value: string) => set({ colors: { ...layout.colors, [key]: value } });
  const setColumn = (index: number, patch: Partial<VirtualColumn>) => set({ columns: layout.columns.map((c, i) => (i === index ? { ...c, ...patch } : c)) });
  const move = (index: number, by: number) => {
    const columns = layout.columns.slice();
    const [column] = columns.splice(index, 1);
    columns.splice(index + by, 0, column);
    set({ columns });
  };
  const preview = view ?? sample(layout);
  const number = (value: string, fallback: number) => Number.parseInt(value, 10) || fallback;

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      await api.putLayout(layout);
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog
      title={isNew ? t("dialog.layout.new") : t("dialog.layout.edit", { name: layoutName(layout) })}
      onClose={onClose}
      onSubmit={save}
      error={error}
      busy={busy}
      wide
    >
      <div className="layout-editor">
        <div className="form-grid">
          <Field label={t("layout.field.id")} hint={isNew ? t("layout.field.id.hint") : undefined}>
            <input value={layout.id} disabled={!isNew} onChange={(e) => set({ id: e.target.value.trim().toLowerCase() })} />
          </Field>
          <Field label={t("layout.field.name")}>
            <input value={layout.name} onChange={(e) => set({ name: e.target.value })} />
          </Field>
          <Field label={t("layout.field.title")} hint={t("layout.field.title.hint")} wide>
            <input value={layout.title} onChange={(e) => set({ title: e.target.value })} />
          </Field>
          <label className="check">
            <input type="checkbox" checked={layout.showStation} onChange={(e) => set({ showStation: e.target.checked })} />
            {t("layout.field.showStation")}
          </label>
          <label className="check">
            <input type="checkbox" checked={layout.showDate} onChange={(e) => set({ showDate: e.target.checked })} />
            {t("layout.field.showDate")}
          </label>
          <label className="check">
            <input type="checkbox" checked={layout.columnHeaders} onChange={(e) => set({ columnHeaders: e.target.checked })} />
            {t("layout.field.columnHeaders")}
          </label>
          <Field label={t("layout.field.clock")}>
            <select value={layout.clock} onChange={(e) => set({ clock: e.target.value as VirtualClock })}>
              {CLOCKS.map((c) => (
                <option key={c} value={c}>
                  {t(`layout.clock.${c}` as TextKey)}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t("layout.field.rows")} hint={t("layout.field.rows.hint")}>
            <input type="number" min={1} max={40} value={layout.rows} onChange={(e) => set({ rows: number(e.target.value, 1) })} />
          </Field>
          <Field label={t("layout.field.font")} hint={t("layout.field.font.hint")}>
            <input value={layout.font} placeholder="Segoe UI, Arial, sans-serif" onChange={(e) => set({ font: e.target.value })} />
          </Field>

          <h3 className="field-wide">{t("layout.colors")}</h3>
          {COLORS.map((key) => (
            <label key={key} className="color-field">
              <input type="color" value={layout.colors[key]} onChange={(e) => setColor(key, e.target.value)} />
              {t(`layout.color.${key}` as TextKey)}
            </label>
          ))}
          <label className="color-field">
            <input type="checkbox" checked={layout.colors.rowAlternate !== ""} onChange={(e) => setColor("rowAlternate", e.target.checked ? layout.colors.rowLine : "")} />
            {layout.colors.rowAlternate !== "" && <input type="color" value={layout.colors.rowAlternate} onChange={(e) => setColor("rowAlternate", e.target.value)} />}
            {t("layout.color.rowAlternate")}
          </label>

          <h3 className="field-wide">{t("layout.timing")}</h3>
          <Field label={t("layout.field.alternationMs")}>
            <input type="number" min={300} max={20000} step={100} value={layout.alternationMs} onChange={(e) => set({ alternationMs: number(e.target.value, 1500) })} />
          </Field>
          <Field label={t("layout.field.blinkMs")}>
            <input type="number" min={100} max={5000} step={50} value={layout.blinkMs} onChange={(e) => set({ blinkMs: number(e.target.value, 600) })} />
          </Field>
          <Field label={t("layout.field.blinkPhases")} hint={t("layout.field.blinkPhases.hint")}>
            <input type="number" min={2} max={10} value={layout.blinkPhases} onChange={(e) => set({ blinkPhases: number(e.target.value, 3) })} />
          </Field>
        </div>

        <div className="layout-preview">
          <Field label={t("layout.preview")} wide>
            <select value={boardId ?? ""} onChange={(e) => setBoardId(e.target.value === "" ? null : Number(e.target.value))}>
              <option value="">{t("layout.preview.sample")}</option>
              {boards.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.id} – {b.name}
                </option>
              ))}
            </select>
          </Field>
          <VirtualScreen view={preview} layout={layout} now={now} preview />
        </div>
      </div>

      <h3>{t("layout.columns")}</h3>
      <label className="check">
        <input
          type="checkbox"
          checked={layout.columns.length === 0}
          onChange={(e) =>
            set({
              columns: e.target.checked ? [] : layoutColumns({ ...layout, columns: [] }, preview.rows).map(({ key: _, ...column }) => column),
            })
          }
        />
        {t("layout.columns.auto")}
      </label>
      {layout.columns.length > 0 && (
        <>
          <p className="muted small">{t("layout.column.hint")}</p>
          <datalist id="layout-cells">
            {CELL_NAMES.map((n) => (
              <option key={n} value={n} />
            ))}
          </datalist>
          <datalist id="layout-widths">
            {["fill", "8%", "12%", "20%", "6em"].map((w) => (
              <option key={w} value={w} />
            ))}
          </datalist>
          <datalist id="layout-colors">
            {["accent", "muted", "delay"].map((c) => (
              <option key={c} value={c} />
            ))}
          </datalist>
          <div className="table-wrap">
            <table className="compact">
              <thead>
                <tr>
                  <th>{t("layout.column.cells")}</th>
                  <th>{t("layout.column.label")}</th>
                  <th>{t("layout.column.width")}</th>
                  <th>{t("layout.column.align")}</th>
                  <th>{t("layout.column.color")}</th>
                  <th>{t("layout.column.bold")}</th>
                  <th>{t("layout.column.size")}</th>
                  <th>{t("layout.column.separator")}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {layout.columns.map((c, i) => (
                  <tr key={i}>
                    <td className="grow">
                      <input
                        list="layout-cells"
                        value={c.cells.join(" ")}
                        onChange={(e) => setColumn(i, { cells: e.target.value.split(/[\s,+]+/).filter((n) => n !== "") })}
                      />
                    </td>
                    <td className="grow">
                      <input value={c.label} onChange={(e) => setColumn(i, { label: e.target.value })} />
                    </td>
                    <td>
                      <input list="layout-widths" size={6} value={c.width} onChange={(e) => setColumn(i, { width: e.target.value.trim() })} />
                    </td>
                    <td>
                      <select value={c.align} onChange={(e) => setColumn(i, { align: e.target.value as VirtualAlign })}>
                        {ALIGNS.map((a) => (
                          <option key={a} value={a}>
                            {t(`layout.align.${a}` as TextKey)}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input list="layout-colors" size={8} value={c.color} onChange={(e) => setColumn(i, { color: e.target.value.trim() })} />
                    </td>
                    <td>
                      <input type="checkbox" checked={c.bold} onChange={(e) => setColumn(i, { bold: e.target.checked })} />
                    </td>
                    <td>
                      <input type="number" min={30} max={300} step={10} value={c.size} onChange={(e) => setColumn(i, { size: number(e.target.value, 100) })} />
                    </td>
                    <td>
                      <input size={4} value={c.separator} onChange={(e) => setColumn(i, { separator: e.target.value })} />
                    </td>
                    <td className="nowrap">
                      <button type="button" className="icon-button" title={t("common.up")} disabled={i === 0} onClick={() => move(i, -1)}>
                        ↑
                      </button>
                      <button type="button" className="icon-button" title={t("common.down")} disabled={i === layout.columns.length - 1} onClick={() => move(i, 1)}>
                        ↓
                      </button>
                      <button type="button" className="icon-button" title={t("common.delete")} onClick={() => set({ columns: layout.columns.filter((_, j) => j !== i) })}>
                        ✕
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <button type="button" className="secondary" onClick={() => set({ columns: [...layout.columns, defaultColumn(["time"])] })}>
            {t("layout.column.add")}
          </button>
        </>
      )}
    </Dialog>
  );
}
