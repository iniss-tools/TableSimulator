// Editor vzhladov tabul (design.json): body alebo listy podla druhu, plocha a ram, celo tabule so stylmi textov - s nahladom
// na tabuli simulatora alebo ukazkovej. Vstavany vzhlad sa ulozi ako vlastny s rovnakym id (prekryje ho); zmazanim sa
// vstavany vrati.
import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import { CircleAlert, CopyPlus, Info, RotateCcw, Save, Trash2, Undo2 } from "lucide-react";
import { toast } from "sonner";
import { deleteDesign, errorText, getDesignsOptions, getDesignsQueryKey, queryClient, setDesignMutation, useConnection, useSimulatorState } from "@/api";
import { ColorField } from "@/components/common/color-field";
import { confirmAction } from "@/components/common/confirm";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Spinner } from "@/components/ui/spinner";
import { DEFAULT_DESIGN_ID, loadDesign, rendererOf, type BoardDesign } from "@/render/design";
import { BoardView } from "@/features/boards/BoardView";
import { t } from "@/i18n";
import { FaceColorsSection, FlapsSection, NumberField, Section, SurfaceSection } from "./DesignParts";
import { NewDesignDialog } from "./NewDesignDialog";
import { SAMPLE_BOARD, SAMPLE_BOARD_ID, SAMPLE_FLAP_BOARD, SAMPLE_FLAP_BOARD_ID } from "./sample-board";

const COLOR_KEYS = ["0", "1", "2", "3", "mono"] as const;

export function DesignsPage() {
  const { designVersion } = useConnection();
  const live = useSimulatorState();
  const designs = useQuery(getDesignsOptions()).data ?? [];
  const [selected, setSelected] = useState(DEFAULT_DESIGN_ID);
  const [preview, setPreview] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  // rozpracovany vzhlad; null = ulozeny
  const [draft, setDraft] = useState<BoardDesign | null>(null);

  const base = useQuery({ queryKey: ["design-file", selected, designVersion], queryFn: () => loadDesign(selected, designVersion) }).data;
  const info = designs.find((d) => d.id === selected);
  const form = draft ?? base;
  const dirty = draft !== null && base !== undefined && JSON.stringify(draft) !== JSON.stringify(base);
  const flaps = form?.renderer === "flaps";

  const save = useMutation({
    ...setDesignMutation(),
    onSuccess: () => {
      toast.success(t("designs.saved"));
      void queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
    },
  });

  useBlocker({
    shouldBlockFn: async () =>
      dirty && !(await confirmAction({ title: t("settings.leave.title"), description: t("designs.leave.text"), confirmLabel: t("settings.leave.confirm") })),
    enableBeforeUnload: () => dirty,
  });

  // nahlad na tabuli s rovnakym druhom kreslenia (listovy vzhlad na listovej tabuli)
  const sample = flaps ? SAMPLE_FLAP_BOARD : SAMPLE_BOARD;
  const boards = useMemo(() => live.boards.filter((b) => rendererOf(b.display.family) === (flaps ? "flaps" : "led-matrix")), [live.boards, flaps]);
  const board = boards.find((b) => b.id === preview) ?? sample;

  const select = async (id: string) => {
    if (dirty && !(await confirmAction({ title: t("settings.leave.title"), description: t("designs.leave.text"), confirmLabel: t("settings.leave.confirm") })))
      return;
    setDraft(null);
    save.reset();
    setSelected(id);
  };

  const remove = async () => {
    if (!info || info.builtIn) return;
    const confirmed = await confirmAction({
      title: t(info.overrides ? "designs.revert.confirm" : "designs.delete.confirm", { name: info.name }),
      destructive: !info.overrides,
      confirmLabel: info.overrides ? t("designs.revert") : undefined,
    });
    if (!confirmed) return;
    try {
      await deleteDesign({ path: { id: info.id }, throwOnError: true });
      setDraft(null);
      if (!info.overrides) setSelected(DEFAULT_DESIGN_ID);
      void queryClient.invalidateQueries({ queryKey: getDesignsQueryKey() });
    } catch (e) {
      toast.error(errorText(e));
    }
  };

  if (!form) return <Skeleton className="h-96 w-full" />;
  const change = (patch: Partial<BoardDesign>) => setDraft({ ...form, ...patch });

  return (
    <>
      <PageHeader description={t("designs.hint")}>
        <OptionSelect
          value={selected}
          onValueChange={(id) => void select(id)}
          ariaLabel={t("designs.select")}
          className="min-w-60"
          options={designs.map((d) => ({ value: d.id, label: d.name }))}
        />
        <Button variant="outline" onClick={() => setCreating(true)}>
          <CopyPlus />
          {t("designs.new")}
        </Button>
        {info && !info.builtIn && (
          <Button variant="outline" onClick={() => void remove()}>
            {info.overrides ? <RotateCcw /> : <Trash2 />}
            {t(info.overrides ? "designs.revert" : "common.delete")}
          </Button>
        )}
      </PageHeader>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <div className="flex min-w-0 flex-col gap-4">
          <Section title={t("designs.section.design")}>
            <FormField label={t("designs.name")}>
              <Input value={form.name} onChange={(e) => change({ name: e.target.value })} />
            </FormField>
            <FormField label={t("designs.id")}>
              <div className="flex flex-wrap items-center gap-2">
                <code className="font-mono text-sm">{selected}</code>
                {info && (
                  <StatusBadge tone={info.builtIn ? "off" : "ok"}>
                    {t(info.builtIn ? "settings.designs.builtIn" : info.overrides ? "designs.overrides" : "settings.designs.custom")}
                  </StatusBadge>
                )}
                <StatusBadge tone="off">{t(flaps ? "designs.renderer.flaps" : "designs.renderer.dots")}</StatusBadge>
              </div>
            </FormField>
          </Section>

          {!flaps && (
            <>
              <Section title={t("designs.section.dots")}>
                <NumberField
                  label={t("designs.pitch")}
                  hint={t("designs.pitch.hint")}
                  value={form.pitch}
                  step={0.5}
                  min={1}
                  max={50}
                  onChange={(pitch) => change({ pitch })}
                />
                <NumberField
                  label={t("designs.dot")}
                  hint={t("designs.dot.hint")}
                  value={form.dot}
                  step={0.05}
                  min={0.1}
                  max={1}
                  onChange={(dot) => change({ dot })}
                />
                <FormField label={t("designs.shape")}>
                  <OptionSelect<BoardDesign["shape"]>
                    value={form.shape}
                    onValueChange={(shape) => change({ shape })}
                    options={(["circle", "square", "rounded"] as const).map((s) => ({ value: s, label: t(`designs.shape.${s}`) }))}
                  />
                </FormField>
                <NumberField
                  label={t("designs.glow")}
                  hint={t("designs.glow.hint")}
                  value={form.glow}
                  step={0.05}
                  min={0}
                  max={1}
                  onChange={(glow) => change({ glow })}
                />
                <ColorField
                  label={t("designs.off")}
                  value={form.off}
                  noneLabel={t("designs.off.none")}
                  fallback="#2b1a14"
                  onChange={(off) => change({ off })}
                />
              </Section>

              <Section title={t("designs.section.colors")}>
                {COLOR_KEYS.map((key) => (
                  <ColorField
                    key={key}
                    label={t(`designs.color.${key}`)}
                    value={form.colors[key] ?? "#ffffff"}
                    onChange={(value) => change({ colors: { ...form.colors, [key]: value ?? "#ffffff" } })}
                  />
                ))}
              </Section>
            </>
          )}

          {flaps && <FlapsSection form={form} change={change} />}
          <SurfaceSection form={form} change={change} />
          <FaceColorsSection form={form} change={change} />
        </div>

        <div className="min-w-0">
          <Card className="xl:sticky xl:top-16">
            <CardHeader className="border-b">
              <CardTitle>{t("designs.preview")}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <OptionSelect
                value={board.id}
                onValueChange={setPreview}
                ariaLabel={t("designs.preview.board")}
                options={[
                  { value: flaps ? SAMPLE_FLAP_BOARD_ID : SAMPLE_BOARD_ID, label: t("designs.preview.sample") },
                  ...boards.map((b) => ({ value: b.id, label: b.name ? `${b.name} (${b.id})` : b.id })),
                ]}
              />
              <BoardView board={board} design={form} />
              {info?.builtIn && (
                <Alert>
                  <Info />
                  <AlertDescription>{t("designs.builtIn.hint")}</AlertDescription>
                </Alert>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

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
          <Button disabled={!dirty || save.isPending} onClick={() => save.mutate({ path: { id: selected }, body: form })}>
            {save.isPending ? <Spinner /> : <Save />}
            {t("common.save")}
          </Button>
        </div>
      </div>

      {creating && (
        <NewDesignDialog
          design={form}
          existing={designs.map((d) => d.id)}
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
