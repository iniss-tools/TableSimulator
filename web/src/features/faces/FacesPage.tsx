// Editor predloh ciel tabul ako vo Figme: hore nastroje (predloha, kroky spat, pridanie prvku, mierka, ulozenie),
// vlavo vrstvy, zobrazenie a tabula nahladu, v strede platno s tabulou a vpravo vlastnosti vybraneho prvku (bez vyberu
// predlohy). Stranka sa neposuva - posuvaju sa len panely. Vstavane predlohy sa daju len skopirovat; vlastne su
// v databaze servera a tabule ich pouzivaju podla kluca.
import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import { CircleAlert, CopyPlus, Plus, Redo, Save, Trash2, Undo, Undo2, ZoomIn, ZoomOut } from "lucide-react";
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
import { faceVariables } from "@/render/face";
import { t } from "@/i18n";
import { ElementProperties } from "./ElementProperties";
import { FaceCanvas } from "./FaceCanvas";
import { FaceProperties } from "./FaceProperties";
import { ELEMENT_KINDS, KIND_ICONS, newElement, previewBoard, variableLabel } from "./faces";
import { useHistory } from "./history";
import { LayersPanel } from "./LayersPanel";
import { NewFaceDialog } from "./NewFaceDialog";
import { typing, zoomStep } from "./zoom";

/** Hodnoty premennych v nahlade, kym ich obsluha nezmeni. */
const SAMPLE_VALUES: Record<string, string> = { track: "1", platform: "2", station: "STANICA", direction: "SEVER" };

const leave = () => confirmAction({ title: t("settings.leave.title"), description: t("faces.leave.text"), confirmLabel: t("settings.leave.confirm") });

export function FacesPage() {
  const live = useSimulatorState();
  const faces = useQuery(getFacesOptions()).data;
  const [selected, setSelected] = useState<string | null>(null);
  // rozpracovana predloha s krokmi spat; null = bez zmien
  const history = useHistory<BoardFaceDto | null>(null);
  const draft = history.value;
  const [element, setElement] = useState<number | null>(null);
  // prvky skryte len v editore (indexy) - neukladaju sa
  const [hidden, setHidden] = useState<ReadonlySet<number>>(new Set());
  const [creating, setCreating] = useState(false);
  const [preview, setPreview] = useState(SAMPLE_BOARD_ID);
  const [values, setValues] = useState(SAMPLE_VALUES);
  const [outlines, setOutlines] = useState(true);
  const [zoom, setZoom] = useState<number | null>(null);
  const [fitScale, setFitScale] = useState(1);

  const base = faces?.find((f) => f.id === selected) ?? faces?.[0];
  const form = draft ?? base;
  const dirty = draft !== null && base !== undefined && JSON.stringify(draft) !== JSON.stringify(base);
  const readOnly = form?.builtIn ?? true;
  // vybrany prvok po kroku spat moze chybat
  const current = element !== null && form && element < form.elements.length ? element : null;

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

  // Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) mimo poli formulara, Ctrl+S kdekolvek
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (!(e.ctrlKey || e.metaKey) || e.altKey) return;
      const key = e.key.toLowerCase();
      if (key === "s") {
        e.preventDefault();
        saveNow();
        return;
      }
      if (readOnly || typing(e.target)) return;
      if (key === "z" && !e.shiftKey) undo();
      else if (key === "y" || (key === "z" && e.shiftKey)) redo();
      else return;
      e.preventDefault();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  });

  const liveBoard = live.boards.find((b) => b.id === preview);
  const board = useMemo(() => {
    const source = liveBoard ?? SAMPLE_BOARD;
    return previewBoard({ ...source.profile, faceValues: values }, source.manufacturer, source.name, liveBoard);
  }, [liveBoard, values]);

  const select = async (id: string) => {
    if (dirty && !(await leave())) return;
    history.reset(null);
    setElement(null);
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
      setElement(null);
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
  const setElements = (elements: FaceElementDto[], transient = false) => change({ elements }, transient);
  const updateElement = (index: number, changed: FaceElementDto, transient = false) =>
    setElements(
      form.elements.map((e, j) => (j === index ? changed : e)),
      transient,
    );
  const add = (kind: FaceElementKind) => {
    setElements([...form.elements, newElement(kind)]);
    setElement(form.elements.length);
  };
  const removeElement = (index: number) => {
    setElements(form.elements.filter((_, j) => j !== index));
    setElement(current === index ? null : current !== null && current > index ? current - 1 : current);
    setHidden(new Set([...hidden].filter((i) => i !== index).map((i) => (i > index ? i - 1 : i))));
  };
  /** Nove poradie kreslenia (povodne indexy) - vyber a skrytie idu s prvkami. */
  const reorder = (order: number[]) => {
    setElements(order.map((i) => form.elements[i]));
    if (current !== null) setElement(order.indexOf(current));
    setHidden(new Set(order.flatMap((old, i) => (hidden.has(old) ? [i] : []))));
  };
  const swap = (index: number, other: number) => reorder(form.elements.map((_, i) => (i === index ? other : i === other ? index : i)));
  const toggleHidden = (index: number) => {
    const next = new Set(hidden);
    if (!next.delete(index)) next.add(index);
    setHidden(next);
  };

  const variables = faceVariables(form);
  const shown = form.elements.flatMap((_, i) => (hidden.has(i) ? [] : [i]));
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
              selected={current}
              hidden={hidden}
              readOnly={readOnly}
              onSelect={setElement}
              onToggleLock={(i) => updateElement(i, { ...form.elements[i], locked: !form.elements[i].locked })}
              onToggleHidden={toggleHidden}
              onReorder={reorder}
            />
          </section>
          <section className="flex shrink-0 flex-col gap-2 border-t pt-3">
            <h2 className="text-xs font-medium text-muted-foreground">{t("faces.view")}</h2>
            <CheckField label={t("faces.outlines")} checked={outlines} onCheckedChange={setOutlines} />
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
          <div className="h-[min(60vh,32rem)] min-h-0 xl:h-auto xl:flex-1">
            <FaceCanvas
              board={board}
              face={form}
              shown={shown}
              selected={current}
              readOnly={readOnly}
              outlines={outlines}
              zoom={zoom}
              onZoom={setZoom}
              onFitScale={setFitScale}
              onSelect={setElement}
              onChange={updateElement}
              onRemove={removeElement}
            />
          </div>
          <p className="shrink-0 text-xs text-muted-foreground">{t(readOnly ? "faces.canvas.readOnly" : "faces.canvas.hint")}</p>
        </div>

        <aside aria-label={t("faces.properties")} className="min-h-0 overflow-y-auto rounded-xl border bg-card p-3">
          {current !== null ? (
            <ElementProperties
              key={current}
              element={form.elements[current]}
              readOnly={readOnly}
              canForward={current < form.elements.length - 1}
              canBackward={current > 0}
              onChange={(changed) => updateElement(current, changed)}
              onForward={() => swap(current, current + 1)}
              onBackward={() => swap(current, current - 1)}
              onRemove={() => removeElement(current)}
            />
          ) : (
            <FaceProperties face={form} readOnly={readOnly} onChange={(patch) => change(patch)} />
          )}
        </aside>
      </div>

      {creating && (
        <NewFaceDialog
          face={form}
          existing={faces.map((f) => f.id)}
          onClose={() => setCreating(false)}
          onCreated={(id) => {
            setCreating(false);
            history.reset(null);
            setElement(null);
            setHidden(new Set());
            setSelected(id);
          }}
        />
      )}
    </div>
  );
}
