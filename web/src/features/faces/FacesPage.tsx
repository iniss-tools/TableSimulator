// Editor predloh ciel tabul ako vo Figme: hore nastroje (predloha, kroky spat, pridanie prvku, mierka, ulozenie),
// vlavo vrstvy, zobrazenie a tabula nahladu, v strede platno s tabulou a vpravo vlastnosti vybranych prvkov (bez vyberu
// predlohy) so zarovnanim. Stranka sa neposuva - posuvaju sa len panely. Prikazy (schranka, poradie, zamok…) idu
// z klaves aj z kontextovej ponuky platna. Vstavane predlohy sa daju len skopirovat; vlastne su v databaze servera
// a tabule ich pouzivaju podla kluca.
import { useCallback, useMemo, useRef, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import { CircleAlert, CopyPlus, Keyboard, Plus, Redo, Save, Trash2, Undo, Undo2, ZoomIn, ZoomOut } from "lucide-react";
import { toast } from "sonner";
import {
  deleteFace,
  errorText,
  getFacesOptions,
  getFacesQueryKey,
  queryClient,
  setFaceMutation,
  useSimulatorState,
  type BoardFaceDto,
  type FaceElementDto,
  type FaceElementKind,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { CheckField, FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Empty, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { SAMPLE_BOARD, SAMPLE_BOARD_ID } from "@/features/designs/sample-board";
import { anchorBox, faceVariables, type Box, type FaceOverlayLayout } from "@/render/face";
import { t } from "@/i18n";
import { copyElements, shortcutAction, useClipboard, type FaceAction } from "./actions";
import { alignDeltas, type AlignOp } from "./align";
import { DocumentKeys } from "./DocumentKeys";
import { extremeOrder, moveElement, stepOrder } from "./edit";
import { ElementProperties, type ElementPatch } from "./ElementProperties";
import { FaceCanvas } from "./FaceCanvas";
import { FaceContextMenu } from "./FaceContextMenu";
import { FaceProperties } from "./FaceProperties";
import { ELEMENT_KINDS, KIND_ICONS, newElement, previewBoard, variableLabel } from "./faces";
import { useHistory } from "./history";
import { LayersPanel } from "./LayersPanel";
import { NewFaceDialog } from "./NewFaceDialog";
import { ShortcutsDialog } from "./ShortcutsDialog";
import { GRID_STEPS, loadView, saveView, type FaceView } from "./view";
import { typing, zoomStep } from "./zoom";

/** Hodnoty premennych v nahlade, kym ich obsluha nezmeni. */
const SAMPLE_VALUES: Record<string, string> = { track: "1", platform: "2", station: "STANICA", direction: "SEVER" };

/** Krok mriezky v ponuke: 1 bod, 2 body, 5 bodov…, pri 8 aj bunka znaku LCD. */
function stepLabel(step: number): string {
  const count = step === 1 ? t("faces.grid.one") : step < 5 ? t("faces.grid.few", { count: step }) : t("faces.grid.many", { count: step });
  return step === 8 ? `${count} ${t("faces.grid.lcd")}` : count;
}

const leave = () => confirmAction({ title: t("settings.leave.title"), description: t("faces.leave.text"), confirmLabel: t("settings.leave.confirm") });

export function FacesPage() {
  const live = useSimulatorState();
  const faces = useQuery(getFacesOptions()).data;
  const [selected, setSelected] = useState<string | null>(null);
  // rozpracovana predloha s krokmi spat; null = bez zmien
  const history = useHistory<BoardFaceDto | null>(null);
  const draft = history.value;
  // vybrane prvky (indexy v predlohe), posledny je naposledy vybrany
  const [selection, setSelection] = useState<number[]>([]);
  // prvky skryte len v editore (indexy) - neukladaju sa
  const [hidden, setHidden] = useState<ReadonlySet<number>>(new Set());
  const [creating, setCreating] = useState(false);
  const [help, setHelp] = useState(false);
  const clipboard = useClipboard();
  const [preview, setPreview] = useState(SAMPLE_BOARD_ID);
  const [values, setValues] = useState(SAMPLE_VALUES);
  const [view, setViewState] = useState<FaceView>(loadView);
  const setView = (patch: Partial<FaceView>) => {
    const next = { ...view, ...patch };
    setViewState(next);
    saveView(next);
  };
  const [zoom, setZoom] = useState<number | null>(null);
  const [fitScale, setFitScale] = useState(1);
  // posledne rozlozenie prvkov v nahlade - zarovnanie potrebuje ich plochy (ref, aby kazde prekreslenie neprekreslilo stranku)
  const layout = useRef<FaceOverlayLayout | null>(null);
  const onLayout = useCallback((value: FaceOverlayLayout) => {
    layout.current = value;
  }, []);

  const base = faces?.find((f) => f.id === selected) ?? faces?.[0];
  const form = draft ?? base;
  const dirty = draft !== null && base !== undefined && JSON.stringify(draft) !== JSON.stringify(base);
  const readOnly = form?.builtIn ?? true;
  // vybrane prvky po kroku spat mozu chybat
  const chosen = form ? selection.filter((i) => i < form.elements.length) : [];

  const save = useMutation({
    ...setFaceMutation(),
    onSuccess: () => {
      toast.success(t("faces.saved"));
      void queryClient.invalidateQueries({ queryKey: getFacesQueryKey() });
    },
  });

  useBlocker({
    shouldBlockFn: async () => dirty && !(await leave()),
    enableBeforeUnload: () => dirty,
  });

  // kroky spat a znova menia prvky - skrytie podla indexov by uz nesedelo
  const { undo: historyUndo, redo: historyRedo } = history;
  const undo = () => {
    historyUndo();
    setHidden(new Set());
  };
  const redo = () => {
    historyRedo();
    setHidden(new Set());
  };
  const saveNow = () => {
    if (form && dirty && !readOnly && !save.isPending) save.mutate({ path: { id: form.id }, body: form });
  };

  const liveBoard = live.boards.find((b) => b.id === preview);
  const board = useMemo(() => {
    const source = liveBoard ?? SAMPLE_BOARD;
    return previewBoard({ ...source.profile, faceValues: values }, source.manufacturer, source.name, liveBoard);
  }, [liveBoard, values]);

  const select = async (id: string) => {
    if (dirty && !(await leave())) return;
    history.reset(null);
    setSelection([]);
    setHidden(new Set());
    save.reset();
    setSelected(id);
  };

  const remove = async () => {
    if (!form || form.builtIn) return;
    if (!(await confirmAction({ title: t("faces.delete.confirm", { name: form.name }), destructive: true }))) return;
    try {
      await deleteFace({ path: { id: form.id }, throwOnError: true });
      history.reset(null);
      setSelection([]);
      setHidden(new Set());
      setSelected(null);
      void queryClient.invalidateQueries({ queryKey: getFacesQueryKey() });
    } catch (e) {
      toast.error(errorText(e));
    }
  };

  if (!faces) return <Skeleton className="h-96 w-full" />;
  if (!form)
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>{t("faces.empty")}</EmptyTitle>
        </EmptyHeader>
      </Empty>
    );

  const change = (patch: Partial<BoardFaceDto>, transient = false) => history.set({ ...form, ...patch }, transient);
  const shown = form.elements.flatMap((_, i) => (hidden.has(i) ? [] : [i]));
  const setElements = (elements: FaceElementDto[], transient = false) => change({ elements }, transient);
  /** Zmena prvkov podla indexu; transient = pokracovanie tahania (nie novy krok historie). */
  const updateMany = (changes: ReadonlyMap<number, FaceElementDto>, transient = false) =>
    setElements(
      form.elements.map((e, j) => changes.get(j) ?? e),
      transient,
    );
  /** Zmena vsetkych vybranych prvkov (panel vlastnosti): rovnaka, alebo pre kazdy zvlast. */
  const patchSelected = (patch: ElementPatch, transient = false) =>
    updateMany(new Map(chosen.map((i, k) => [i, { ...form.elements[i], ...(typeof patch === "function" ? patch(form.elements[i], k) : patch) }])), transient);
  /** Nove prvky navrch, vybrane. */
  const insert = (added: readonly FaceElementDto[]) => {
    setElements([...form.elements, ...added]);
    setSelection(added.map((_, k) => form.elements.length + k));
  };
  const add = (kind: FaceElementKind) => {
    setElements([...form.elements, newElement(kind)]);
    setSelection([form.elements.length]);
  };
  const removeMany = (indices: number[]) => {
    const gone = new Set(indices);
    // index po odstraneni prvkov pred nim
    const shift = (i: number) => i - indices.filter((r) => r < i).length;
    setElements(form.elements.filter((_, j) => !gone.has(j)));
    setSelection(chosen.filter((i) => !gone.has(i)).map(shift));
    setHidden(new Set([...hidden].filter((i) => !gone.has(i)).map(shift)));
  };
  /** Nove poradie kreslenia (povodne indexy) - vyber a skrytie idu s prvkami. */
  const reorder = (order: number[]) => {
    setElements(order.map((i) => form.elements[i]));
    setSelection(chosen.map((i) => order.indexOf(i)));
    setHidden(new Set(order.flatMap((old, i) => (hidden.has(old) ? [i] : []))));
  };
  const inSelection = new Set(chosen);
  const canForward = chosen.some((i) => i < form.elements.length - 1 && !inSelection.has(i + 1));
  const canBackward = chosen.some((i) => i > 0 && !inSelection.has(i - 1));
  /** Vyber vo vrstvach: len prvok, pridat/odobrat (Ctrl) alebo rozsah od naposledy vybraneho (Shift). */
  const selectLayer = (index: number, mode: "replace" | "toggle" | "range") => {
    if (mode === "toggle") setSelection(inSelection.has(index) ? chosen.filter((i) => i !== index) : [...chosen, index]);
    else if (mode === "range" && chosen.length) {
      const from = chosen[chosen.length - 1];
      const range = Array.from({ length: Math.abs(index - from) + 1 }, (_, k) => (index > from ? from + k : from - k));
      setSelection([...chosen.filter((i) => !range.includes(i)), ...range]);
    } else setSelection([index]);
  };
  /** Zarovnanie vybranych prvkov podla ich ploch v nahlade (jeden prvok k ukotveniu); skryte prvky ostanu. */
  const align = (op: AlignOp) => {
    const current = layout.current;
    if (!current) return;
    const { geometry: g, boxes } = current;
    const points = (b: Box): Box => ({ x: b.x / g.unit, y: b.y / g.unit, w: b.w / g.unit, h: b.h / g.unit });
    const items = chosen.flatMap((i) => {
      const box = boxes[shown.indexOf(i)];
      return shown.includes(i) && box ? [{ index: i, box: points(box) }] : [];
    });
    if (items.length === 0) return;
    const anchor = items.length === 1 ? anchorBox(g, form.elements[items[0].index]) : null;
    if (items.length === 1 && !anchor) return;
    const deltas = alignDeltas(
      items.map((item) => item.box),
      op,
      anchor ? points(anchor) : undefined,
    );
    const changes = new Map(items.map((item, k) => [item.index, moveElement(form.elements[item.index], deltas[k].dx, deltas[k].dy, 0.01)]));
    if ([...changes].some(([i, changed]) => changed.x !== form.elements[i].x || changed.y !== form.elements[i].y)) updateMany(changes);
  };
  const toggleHidden = (index: number) => {
    const next = new Set(hidden);
    if (!next.delete(index)) next.add(index);
    setHidden(next);
  };
  const allLocked = chosen.length > 0 && chosen.every((i) => form.elements[i].locked);
  const allHidden = chosen.length > 0 && chosen.every((i) => hidden.has(i));

  /** Prikaz z klaves alebo kontextovej ponuky; false = neda sa pouzit (klavesa ostane prehliadacu). */
  const run = (action: FaceAction): boolean => {
    const edit = !readOnly && chosen.length > 0;
    const picked = chosen.map((i) => form.elements[i]);
    switch (action) {
      case "undo":
        if (readOnly) return false;
        undo();
        return true;
      case "redo":
        if (readOnly) return false;
        redo();
        return true;
      case "selectAll":
        setSelection(form.elements.flatMap((e, i) => (e.locked || hidden.has(i) ? [] : [i])));
        return true;
      case "copy":
        if (chosen.length === 0) return false;
        copyElements(picked);
        return true;
      case "cut":
        if (!edit) return false;
        copyElements(picked);
        removeMany(chosen);
        return true;
      case "paste":
        if (readOnly || clipboard.length === 0) return false;
        insert(clipboard);
        return true;
      case "duplicate":
        if (!edit) return false;
        insert(picked.map((e) => ({ ...e, locked: false })));
        return true;
      case "remove":
        if (!edit) return false;
        removeMany(chosen);
        return true;
      case "forward":
      case "front":
        if (!edit || !canForward) return false;
        reorder(action === "front" ? extremeOrder(form.elements.length, chosen, true) : stepOrder(form.elements.length, chosen, 1));
        return true;
      case "backward":
      case "back":
        if (!edit || !canBackward) return false;
        reorder(action === "back" ? extremeOrder(form.elements.length, chosen, false) : stepOrder(form.elements.length, chosen, -1));
        return true;
      case "lock":
        if (!edit) return false;
        patchSelected({ locked: !allLocked });
        return true;
      case "hide": {
        if (chosen.length === 0) return false;
        const next = new Set(hidden);
        for (const i of chosen) {
          if (allHidden) next.delete(i);
          else next.add(i);
        }
        setHidden(next);
        return true;
      }
    }
  };

  // Ctrl+S kdekolvek, ostatne skratky mimo poli formulara, okien a zoznamov vyberu
  const onKey = (e: KeyboardEvent) => {
    if (e.defaultPrevented) return;
    const mod = e.ctrlKey || e.metaKey;
    if (mod && !e.altKey && e.key.toLowerCase() === "s") {
      e.preventDefault();
      saveNow();
      return;
    }
    if (typing(e.target) || (e.target instanceof Element && e.target.closest('[role="dialog"],[role="alertdialog"],[role="listbox"]'))) return;
    if (e.key === "?" && !mod) {
      e.preventDefault();
      setHelp(true);
      return;
    }
    const action = shortcutAction(e);
    if (action && run(action)) e.preventDefault();
  };

  /** Dvojklik na prvok: vyberie ho a text da do pola na upravu. */
  const open = (index: number) => {
    setSelection([index]);
    if (form.elements[index]?.kind !== "text") return;
    requestAnimationFrame(() => {
      const field = document.getElementById("face-element-text");
      if (field instanceof HTMLTextAreaElement) {
        field.focus();
        field.select();
      }
    });
  };

  const variables = faceVariables(form);
  const scale = zoom ?? fitScale;

  return (
    <div className="flex flex-col gap-3 xl:h-[calc(100dvh-5rem)] xl:min-h-[34rem]">
      <div className="flex flex-wrap items-center gap-2">
        <OptionSelect
          value={form.id}
          onValueChange={(id) => void select(id)}
          ariaLabel={t("faces.select")}
          className="min-w-64"
          options={faces.map((f) => ({ value: f.id, label: f.name }))}
        />
        <Button variant="outline" onClick={() => setCreating(true)}>
          <CopyPlus />
          {t("faces.new")}
        </Button>
        {!form.builtIn && (
          <Button variant="outline" onClick={() => void remove()}>
            <Trash2 />
            {t("common.delete")}
          </Button>
        )}
        <Separator orientation="vertical" className="mx-1 data-[orientation=vertical]:h-6" />
        <Button variant="ghost" size="icon" title={t("faces.undo")} aria-label={t("faces.undo")} disabled={readOnly || !history.canUndo} onClick={undo}>
          <Undo />
        </Button>
        <Button variant="ghost" size="icon" title={t("faces.redo")} aria-label={t("faces.redo")} disabled={readOnly || !history.canRedo} onClick={redo}>
          <Redo />
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger render={<Button variant="outline" disabled={readOnly} />}>
            <Plus />
            {t("faces.add")}
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="min-w-40">
            {ELEMENT_KINDS.map((kind) => {
              const Icon = KIND_ICONS[kind];
              return (
                <DropdownMenuItem key={kind} onClick={() => add(kind)}>
                  <Icon />
                  {t(`faces.add.${kind}`)}
                </DropdownMenuItem>
              );
            })}
          </DropdownMenuContent>
        </DropdownMenu>
        <Separator orientation="vertical" className="mx-1 data-[orientation=vertical]:h-6" />
        <Button variant="ghost" size="icon" title={t("faces.zoom.out")} aria-label={t("faces.zoom.out")} onClick={() => setZoom(zoomStep(scale, -1))}>
          <ZoomOut />
        </Button>
        <Button
          variant="ghost"
          className="min-w-16 tabular-nums"
          title={t("faces.zoom.fit")}
          aria-label={t("faces.zoom", { percent: Math.round(scale * 100) })}
          onClick={() => setZoom(null)}
        >
          {Math.round(scale * 100)} %
        </Button>
        <Button variant="ghost" size="icon" title={t("faces.zoom.in")} aria-label={t("faces.zoom.in")} onClick={() => setZoom(zoomStep(scale, 1))}>
          <ZoomIn />
        </Button>
        <Button variant="ghost" size="icon" title={t("faces.shortcuts")} aria-label={t("faces.keys.title")} onClick={() => setHelp(true)}>
          <Keyboard />
        </Button>
        {!readOnly && (
          <div className="ml-auto flex flex-wrap items-center gap-2">
            {dirty && <span className="text-sm text-muted-foreground">{t("settings.unsaved")}</span>}
            <Button
              variant="outline"
              disabled={!dirty || save.isPending}
              onClick={() => {
                history.reset(null);
                setHidden(new Set());
                save.reset();
              }}
            >
              <Undo2 />
              {t("settings.discard")}
            </Button>
            <Button title={t("faces.save")} disabled={!dirty || save.isPending} onClick={saveNow}>
              {save.isPending ? <Spinner /> : <Save />}
              {t("common.save")}
            </Button>
          </div>
        )}
      </div>

      {save.error && (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription className="whitespace-pre-line">{errorText(save.error)}</AlertDescription>
        </Alert>
      )}

      <div className="grid min-h-0 flex-1 grid-cols-1 gap-3 xl:grid-cols-[14rem_minmax(0,1fr)_18rem]">
        <aside className="flex max-h-[32rem] min-h-0 flex-col gap-4 rounded-xl border bg-card p-3 xl:max-h-none">
          <section className="flex min-h-0 flex-1 flex-col gap-2">
            <h2 className="text-xs font-medium text-muted-foreground">{t("faces.layers")}</h2>
            <LayersPanel
              elements={form.elements}
              selection={chosen}
              hidden={hidden}
              readOnly={readOnly}
              onSelect={selectLayer}
              onToggleLock={(i) => updateMany(new Map([[i, { ...form.elements[i], locked: !form.elements[i].locked }]]))}
              onToggleHidden={toggleHidden}
              onReorder={reorder}
            />
          </section>
          <section className="flex shrink-0 flex-col gap-2 border-t pt-3">
            <h2 className="text-xs font-medium text-muted-foreground">{t("faces.view")}</h2>
            <CheckField label={t("faces.outlines")} checked={view.outlines} onCheckedChange={(outlines) => setView({ outlines })} />
            <div className="flex items-center gap-2">
              <CheckField label={t("faces.grid")} checked={view.grid} onCheckedChange={(grid) => setView({ grid })} className="w-auto" />
              <OptionSelect
                value={view.gridStep}
                onValueChange={(gridStep) => setView({ gridStep })}
                ariaLabel={t("faces.grid.step")}
                size="sm"
                className="ml-auto w-auto"
                options={GRID_STEPS.map((step) => ({ value: step, label: stepLabel(step) }))}
              />
            </div>
            <CheckField label={t("faces.snapGrid")} checked={view.snapGrid} onCheckedChange={(snapGrid) => setView({ snapGrid })} />
            <CheckField label={t("faces.guides")} checked={view.guides} onCheckedChange={(guides) => setView({ guides })} />
            <p className="text-xs text-muted-foreground">{t("faces.view.hint")}</p>
          </section>
          <section className="flex shrink-0 flex-col gap-2 border-t pt-3">
            <h2 className="text-xs font-medium text-muted-foreground">{t("designs.preview")}</h2>
            <OptionSelect
              value={preview}
              onValueChange={setPreview}
              ariaLabel={t("designs.preview.board")}
              className="w-full"
              options={[
                { value: SAMPLE_BOARD_ID, label: t("designs.preview.sample") },
                ...live.boards.map((b) => ({ value: b.id, label: b.name ? `${b.name} (${b.id})` : b.id })),
              ]}
            />
            {variables.map((key) => (
              <FormField key={key} label={variableLabel(key)}>
                <Input value={values[key] ?? ""} onChange={(e) => setValues({ ...values, [key]: e.target.value })} />
              </FormField>
            ))}
          </section>
        </aside>

        <div className="order-first flex min-h-0 flex-col gap-2 xl:order-none">
          <FaceContextMenu
            className="h-[min(60vh,32rem)] min-h-0 xl:h-auto xl:flex-1"
            count={chosen.length}
            readOnly={readOnly}
            canPaste={clipboard.length > 0}
            canForward={canForward}
            canBackward={canBackward}
            locked={allLocked}
            hidden={allHidden}
            onAction={(action) => {
              run(action);
            }}
          >
            <FaceCanvas
              board={board}
              face={form}
              shown={shown}
              selection={chosen}
              readOnly={readOnly}
              view={view}
              zoom={zoom}
              onZoom={setZoom}
              onFitScale={setFitScale}
              onSelect={setSelection}
              onChange={updateMany}
              onRemove={removeMany}
              onLayout={onLayout}
              onOpen={open}
            />
          </FaceContextMenu>
          <p className="shrink-0 text-xs text-muted-foreground">{t(readOnly ? "faces.canvas.readOnly" : "faces.canvas.hint")}</p>
        </div>

        <aside aria-label={t("faces.properties")} className="min-h-0 overflow-y-auto rounded-xl border bg-card p-3">
          {chosen.length ? (
            <ElementProperties
              key={chosen.join(",")}
              elements={chosen.map((i) => form.elements[i])}
              readOnly={readOnly}
              canForward={canForward}
              canBackward={canBackward}
              onPatch={patchSelected}
              onForward={() => run("forward")}
              onBackward={() => run("backward")}
              onRemove={() => removeMany(chosen)}
              onAlign={align}
            />
          ) : (
            <FaceProperties face={form} readOnly={readOnly} onChange={(patch) => change(patch)} />
          )}
        </aside>
      </div>

      <DocumentKeys onKeyDown={onKey} />
      <ShortcutsDialog open={help} onClose={() => setHelp(false)} />
      {creating && (
        <NewFaceDialog
          face={form}
          existing={faces.map((f) => f.id)}
          onClose={() => setCreating(false)}
          onCreated={(id) => {
            setCreating(false);
            history.reset(null);
            setSelection([]);
            setHidden(new Set());
            setSelected(id);
          }}
        />
      )}
    </div>
  );
}
