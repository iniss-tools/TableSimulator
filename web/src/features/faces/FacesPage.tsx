// Editor predloh ciel tabul: plochy, texty a hodiny okolo matice s nahladom na tabuli simulatora alebo ukazkovej.
// Vstavane predlohy sa daju len skopirovat; vlastne su v databaze servera a tabule ich pouzivaju podla kluca.
import { useMemo, useState, type ReactNode } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import { CircleAlert, Clock, CopyPlus, Info, Save, Square, Trash2, Type, Undo2 } from "lucide-react";
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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Empty, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { BoardView } from "@/features/boards/BoardView";
import { SAMPLE_BOARD, SAMPLE_BOARD_ID } from "@/features/designs/sample-board";
import { faceVariables } from "@/render/face";
import { t } from "@/i18n";
import { FaceElementCard } from "./FaceElementCard";
import { newElement, previewBoard, variableLabel } from "./faces";
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

export function FacesPage() {
  const live = useSimulatorState();
  const faces = useQuery(getFacesOptions()).data;
  const [selected, setSelected] = useState<string | null>(null);
  const [draft, setDraft] = useState<BoardFaceDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [preview, setPreview] = useState(SAMPLE_BOARD_ID);
  const [values, setValues] = useState(SAMPLE_VALUES);

  const base = faces?.find((f) => f.id === selected) ?? faces?.[0];
  const form = draft ?? base;
  const dirty = draft !== null && base !== undefined && JSON.stringify(draft) !== JSON.stringify(base);
  const readOnly = form?.builtIn ?? true;

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

  const liveBoard = live.boards.find((b) => b.id === preview);
  const board = useMemo(() => {
    const source = liveBoard ?? SAMPLE_BOARD;
    return previewBoard({ ...source.profile, faceValues: values }, source.manufacturer, source.name, liveBoard);
  }, [liveBoard, values]);

  const select = async (id: string) => {
    if (dirty && !(await confirmAction({ title: t("settings.leave.title"), description: t("faces.leave.text"), confirmLabel: t("settings.leave.confirm") })))
      return;
    setDraft(null);
    save.reset();
    setSelected(id);
  };

  const remove = async () => {
    if (!form || form.builtIn) return;
    if (!(await confirmAction({ title: t("faces.delete.confirm", { name: form.name }), destructive: true }))) return;
    try {
      await deleteFace({ path: { id: form.id }, throwOnError: true });
      setDraft(null);
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

  const change = (patch: Partial<BoardFaceDto>) => setDraft({ ...form, ...patch });
  const setElements = (elements: FaceElementDto[]) => change({ elements });
  const add = (kind: FaceElementKind) => setElements([...form.elements, newElement(kind)]);
  const variables = faceVariables(form);

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

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
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
            </FieldGroup>
          </Section>

          <Section title={t("faces.section.elements")} description={t("faces.section.elements.hint")}>
            <div className="flex flex-col gap-3">
              {form.elements.map((element, i) => (
                <FaceElementCard
                  key={i}
                  element={element}
                  index={i}
                  count={form.elements.length}
                  readOnly={readOnly}
                  onChange={(changed) => setElements(form.elements.map((e, j) => (j === i ? changed : e)))}
                  onMove={(to) => {
                    const moved = [...form.elements];
                    const [item] = moved.splice(i, 1);
                    moved.splice(to, 0, item);
                    setElements(moved);
                  }}
                  onRemove={() => setElements(form.elements.filter((_, j) => j !== i))}
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
                </div>
              )}
            </div>
          </Section>
        </div>

        <div className="min-w-0">
          <Card className="xl:sticky xl:top-16">
            <CardHeader className="border-b">
              <CardTitle>{t("designs.preview")}</CardTitle>
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
              <BoardView board={board} face={form} />
            </CardContent>
          </Card>
        </div>
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
                setDraft(null);
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
            setDraft(null);
            setSelected(id);
          }}
        />
      )}
    </>
  );
}
