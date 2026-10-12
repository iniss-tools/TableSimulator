// Editor predloh ciel tabul: plochy, texty a hodiny okolo matice s nahladom na tabuli simulatora alebo ukazkovej.
// Prvky sa daju upravovat aj priamo v nahlade (FaceCanvasEditor) - vyber, tahanie, uchyty, kroky spat a znova.
// Vstavane predlohy sa daju len skopirovat; vlastne su v databaze servera a tabule ich pouzivaju podla kluca.
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import {
  ArrowUpRight,
  CircleAlert,
  Clock,
  CopyPlus,
  ImageIcon,
  Info,
  Maximize2,
  Minimize2,
  Redo,
  Save,
  Square,
  SquareDashed,
  Trash2,
  Type,
  Undo,
  Undo2,
} from "lucide-react";
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
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Empty, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { BoardView } from "@/features/boards/BoardView";
import { SAMPLE_BOARD, SAMPLE_BOARD_ID } from "@/features/designs/sample-board";
import { cn } from "@/lib/utils";
import { faceVariables } from "@/render/face";
import { t } from "@/i18n";
import { moveItem } from "./edit";
import { FaceCanvasEditor } from "./FaceCanvasEditor";
import { FaceElementCard } from "./FaceElementCard";
import { newElement, previewBoard, variableLabel } from "./faces";
import { useHistory } from "./history";
import { NewFaceDialog } from "./NewFaceDialog";

/** Hodnoty premennych v nahlade, kym ich obsluha nezmeni. */
const SAMPLE_VALUES: Record<string, string> = { track: "1", platform: "2", station: "STANICA", direction: "SEVER" };

function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <Card size="sm">
      <CardHeader className="border-b">
        <CardTitle>{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

const int = (value: string) => Number.parseInt(value, 10) || 0;

/** Klavesova skratka ide do pola formulara (tam patri jeho vlastne spat), nie do editora. */
function typing(target: EventTarget | null): boolean {
  return target instanceof HTMLElement && (target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName));
}

export function FacesPage() {
  const live = useSimulatorState();
  const faces = useQuery(getFacesOptions()).data;
  const [selected, setSelected] = useState<string | null>(null);
  // rozpracovana predloha s krokmi spat; null = bez zmien
  const history = useHistory<BoardFaceDto | null>(null);
  const draft = history.value;
  const [element, setElement] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [preview, setPreview] = useState(SAMPLE_BOARD_ID);
  const [values, setValues] = useState(SAMPLE_VALUES);
  const [outlines, setOutlines] = useState(true);
  const [wide, setWide] = useState(false);

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
    shouldBlockFn: async () =>
      dirty && !(await confirmAction({ title: t("settings.leave.title"), description: t("faces.leave.text"), confirmLabel: t("settings.leave.confirm") })),
    enableBeforeUnload: () => dirty,
  });

  // Ctrl+Z / Ctrl+Y (Ctrl+Shift+Z) mimo poli formulara
  const { undo, redo } = history;
  useEffect(() => {
    if (readOnly) return;
    const onKey = (e: KeyboardEvent) => {
      if (!(e.ctrlKey || e.metaKey) || e.altKey || typing(e.target)) return;
      const key = e.key.toLowerCase();
      if (key === "z" && !e.shiftKey) undo();
      else if (key === "y" || (key === "z" && e.shiftKey)) redo();
      else return;
      e.preventDefault();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [readOnly, undo, redo]);

  const liveBoard = live.boards.find((b) => b.id === preview);
  const board = useMemo(() => {
    const source = liveBoard ?? SAMPLE_BOARD;
    return previewBoard({ ...source.profile, faceValues: values }, source.manufacturer, source.name, liveBoard);
  }, [liveBoard, values]);

  const select = async (id: string) => {
    if (dirty && !(await confirmAction({ title: t("settings.leave.title"), description: t("faces.leave.text"), confirmLabel: t("settings.leave.confirm") })))
      return;
    history.reset(null);
    setElement(null);
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
  const add = (kind: FaceElementKind) => {
    setElements([...form.elements, newElement(kind)]);
    setElement(form.elements.length);
  };
  const removeElement = (index: number) => {
    setElements(form.elements.filter((_, j) => j !== index));
    setElement(current === index ? null : current !== null && current > index ? current - 1 : current);
  };
  const moveElement = (from: number, to: number) => {
    setElements(moveItem(form.elements, from, to));
    if (current === from) setElement(to);
    else if (current === to) setElement(from);
  };
  const variables = faceVariables(form);

  const previewCard = (
    <Card className={cn(wide ? "shadow-md" : "xl:sticky xl:top-16")}>
      <CardHeader className="border-b">
        <CardTitle>{t("designs.preview")}</CardTitle>
        <CardAction className="flex gap-1">
          {!readOnly && (
            <>
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                title={t("faces.undo")}
                aria-label={t("faces.undo")}
                disabled={!history.canUndo}
                onClick={undo}
              >
                <Undo />
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                title={t("faces.redo")}
                aria-label={t("faces.redo")}
                disabled={!history.canRedo}
                onClick={redo}
              >
                <Redo />
              </Button>
            </>
          )}
          <Button
            type="button"
            variant={outlines ? "secondary" : "ghost"}
            size="icon-sm"
            title={t("faces.outlines")}
            aria-label={t("faces.outlines")}
            aria-pressed={outlines}
            onClick={() => setOutlines(!outlines)}
          >
            <SquareDashed />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="hidden xl:inline-flex"
            title={t("faces.wide")}
            aria-label={t("faces.wide")}
            aria-pressed={wide}
            onClick={() => setWide(!wide)}
          >
            {wide ? <Minimize2 /> : <Maximize2 />}
          </Button>
        </CardAction>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <OptionSelect
          value={preview}
          onValueChange={setPreview}
          ariaLabel={t("designs.preview.board")}
          options={[
            { value: SAMPLE_BOARD_ID, label: t("designs.preview.sample") },
            ...live.boards.map((b) => ({ value: b.id, label: b.name ? `${b.name} (${b.id})` : b.id })),
          ]}
        />
        {variables.length > 0 && (
          <div className="grid gap-3 sm:grid-cols-3">
            {variables.map((key) => (
              <FormField key={key} label={variableLabel(key)}>
                <Input value={values[key] ?? ""} onChange={(e) => setValues({ ...values, [key]: e.target.value })} />
              </FormField>
            ))}
          </div>
        )}
        <BoardView
          board={board}
          face={form}
          overlay={(layout) => (
            <FaceCanvasEditor
              layout={layout}
              face={form}
              selected={current}
              readOnly={readOnly}
              outlines={outlines}
              onSelect={setElement}
              onChange={(i, changed, transient) =>
                setElements(
                  form.elements.map((e, j) => (j === i ? changed : e)),
                  transient,
                )
              }
              onRemove={removeElement}
            />
          )}
        />
        <p className="text-sm text-muted-foreground">{t(readOnly ? "faces.canvas.readOnly" : "faces.canvas.hint")}</p>
      </CardContent>
    </Card>
  );

  return (
    <>
      <PageHeader description={t("faces.hint")}>
        <OptionSelect
          value={form.id}
          onValueChange={(id) => void select(id)}
          ariaLabel={t("faces.select")}
          className="min-w-72"
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
      </PageHeader>

      <div className={cn("grid gap-6", !wide && "xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]")}>
        {wide && <div className="sticky top-16 z-20 min-w-0">{previewCard}</div>}
        <div className="flex min-w-0 flex-col gap-4">
          {form.builtIn && (
            <Alert>
              <Info />
              <AlertDescription>{t("faces.builtIn.hint")}</AlertDescription>
            </Alert>
          )}
          <Section title={t("faces.section.face")}>
            <FieldGroup className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("faces.name")}>
                <Input value={form.name} disabled={readOnly} onChange={(e) => change({ name: e.target.value })} />
              </FormField>
              <FormField label={t("faces.id")}>
                <div className="flex items-center gap-2">
                  <code className="font-mono text-sm">{form.id}</code>
                  <StatusBadge tone={form.builtIn ? "off" : "ok"}>{t(form.builtIn ? "settings.designs.builtIn" : "settings.designs.custom")}</StatusBadge>
                </div>
              </FormField>
            </FieldGroup>
          </Section>

          <Section title={t("faces.section.layout")} description={t("faces.section.layout.hint")}>
            <FieldGroup className="grid gap-4 sm:grid-cols-4">
              {(["left", "top", "right", "bottom"] as const).map((side) => (
                <FormField key={side} label={t(`faces.margin.${side}`)}>
                  <Input
                    type="number"
                    min={0}
                    value={form.margin[side]}
                    disabled={readOnly}
                    onChange={(e) => change({ margin: { ...form.margin, [side]: Math.max(0, int(e.target.value)) } })}
                  />
                </FormField>
              ))}
              <FormField label={t("faces.rowGap")} hint={t("faces.rowGap.hint")} className="sm:col-span-2">
                <Input
                  type="number"
                  min={0}
                  placeholder={t("faces.rowGap.design")}
                  value={form.rowGap ?? ""}
                  disabled={readOnly}
                  onChange={(e) => change({ rowGap: e.target.value === "" ? null : Math.max(0, int(e.target.value)) })}
                />
              </FormField>
              <FormField label={t("faces.rowGroup")}>
                <Input
                  type="number"
                  min={0}
                  value={form.rowGroup}
                  disabled={readOnly}
                  onChange={(e) => change({ rowGroup: Math.max(0, int(e.target.value)) })}
                />
              </FormField>
              <FormField label={t("faces.groupGap")}>
                <Input
                  type="number"
                  min={0}
                  value={form.groupGap}
                  disabled={readOnly}
                  onChange={(e) => change({ groupGap: Math.max(0, int(e.target.value)) })}
                />
              </FormField>
              <FormField label={t("faces.moduleWidth")} hint={t("faces.moduleWidth.hint")} className="sm:col-span-2">
                <Input
                  type="number"
                  min={0}
                  value={form.moduleWidth}
                  disabled={readOnly}
                  onChange={(e) => change({ moduleWidth: Math.max(0, int(e.target.value)) })}
                />
              </FormField>
            </FieldGroup>
          </Section>

          <Section title={t("faces.section.elements")} description={t("faces.section.elements.hint")}>
            <div className="flex flex-col gap-3">
              {form.elements.map((item, i) => (
                <FaceElementCard
                  key={i}
                  element={item}
                  index={i}
                  count={form.elements.length}
                  readOnly={readOnly}
                  selected={current === i}
                  onSelect={() => setElement(i)}
                  onChange={(changed) => setElements(form.elements.map((e, j) => (j === i ? changed : e)))}
                  onMove={(to) => moveElement(i, to)}
                  onRemove={() => removeElement(i)}
                />
              ))}
              {!readOnly && (
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" onClick={() => add("rect")}>
                    <Square />
                    {t("faces.add.rect")}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => add("text")}>
                    <Type />
                    {t("faces.add.text")}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => add("clock")}>
                    <Clock />
                    {t("faces.add.clock")}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => add("symbol")}>
                    <ArrowUpRight />
                    {t("faces.add.symbol")}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => add("image")}>
                    <ImageIcon />
                    {t("faces.add.image")}
                  </Button>
                </div>
              )}
            </div>
          </Section>
        </div>

        {!wide && <div className="min-w-0">{previewCard}</div>}
      </div>

      {!readOnly && (
        <div className="sticky bottom-0 z-10 -mx-1 mt-6 flex flex-col gap-3 border-t bg-background/95 px-1 py-3 backdrop-blur">
          {save.error && (
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription className="whitespace-pre-line">{errorText(save.error)}</AlertDescription>
            </Alert>
          )}
          <div className="flex flex-wrap items-center justify-end gap-2">
            <span className="mr-auto text-sm text-muted-foreground">{dirty && t("settings.unsaved")}</span>
            <Button
              variant="outline"
              disabled={!dirty || save.isPending}
              onClick={() => {
                history.reset(null);
                save.reset();
              }}
            >
              <Undo2 />
              {t("settings.discard")}
            </Button>
            <Button disabled={!dirty || save.isPending} onClick={() => save.mutate({ path: { id: form.id }, body: form })}>
              {save.isPending ? <Spinner /> : <Save />}
              {t("common.save")}
            </Button>
          </div>
        </div>
      )}

      {creating && (
        <NewFaceDialog
          face={form}
          existing={faces.map((f) => f.id)}
          onClose={() => setCreating(false)}
          onCreated={(id) => {
            setCreating(false);
            history.reset(null);
            setElement(null);
            setSelected(id);
          }}
        />
      )}
    </>
  );
}
