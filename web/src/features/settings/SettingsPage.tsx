// Nastavenie servera po castiach (priecinky, siet a pristup, HTTPS, databaza) s jednym ulozenim; kym obsluha nic
// nezmenila, formular ukazuje aktualne nastavenie servera.
import { useState, type ReactNode } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useBlocker } from "@tanstack/react-router";
import {
  BellRing,
  CircleAlert,
  Database,
  DatabaseBackup,
  FolderOpen,
  Network,
  Save,
  Server,
  ShieldCheck,
  TrainFront,
  Undo2,
  type LucideIcon,
} from "lucide-react";
import { toast } from "sonner";
import {
  errorText,
  getDesignsOptions,
  getFontsOptions,
  getServerInfoOptions,
  getHttpsStatusQueryKey,
  getStateQueryKey,
  PERMISSIONS,
  queryClient,
  updateSettingsMutation,
  useCan,
  useSimulatorState,
  type Permission,
  type SettingsDto,
} from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { CheckField, FormField } from "@/components/common/form-field";
import { PageHeader } from "@/components/common/page-header";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";
import { clearFontCache } from "@/render/fonts";
import { t, type TextKey } from "@/i18n";
import { alertsRequest } from "./alerts";
import { AlertsSection } from "./AlertsSection";
import { BackupSection } from "./backup-section";
import { DatabaseSection } from "./database-section";
import { DesignsFound, FontsFound } from "./found-lists";
import { HttpsSection } from "./HttpsSection";
import { ServerSection } from "./server-section";
import { useRestartServer } from "./use-restart-server";
import { RestartBadge } from "./restart-badge";

type SectionId = "folders" | "iniss" | "network" | "https" | "alerts" | "server" | "database" | "backup";

// cast vidi len ten, kto ma jej opravnenie (nastavenie servera alebo zaloha a restart)
const SECTIONS: { id: SectionId; label: TextKey; icon: LucideIcon; permission: Permission | Permission[] }[] = [
  { id: "folders", label: "settings.folders", icon: FolderOpen, permission: PERMISSIONS.settings },
  { id: "iniss", label: "settings.iniss", icon: TrainFront, permission: PERMISSIONS.settings },
  { id: "network", label: "settings.network", icon: Network, permission: PERMISSIONS.settings },
  { id: "https", label: "settings.https", icon: ShieldCheck, permission: PERMISSIONS.settings },
  { id: "alerts", label: "settings.alerts", icon: BellRing, permission: PERMISSIONS.settings },
  { id: "server", label: "settings.server", icon: Server, permission: [PERMISSIONS.settings, PERMISSIONS.server] },
  { id: "database", label: "database.section", icon: Database, permission: PERMISSIONS.settings },
  { id: "backup", label: "settings.backup", icon: DatabaseBackup, permission: PERMISSIONS.server },
];

function Section({ id, description, children }: { id: SectionId; description: ReactNode; children: ReactNode }) {
  const { label, icon: Icon, permission } = SECTIONS.find((s) => s.id === id)!;
  const allowed = useCan()(permission);
  if (!allowed) return null;
  return (
    <Card id={`settings-${id}`} className="scroll-mt-16">
      <CardHeader className="border-b">
        <CardTitle className="flex items-center gap-2">
          <Icon className="size-4 text-muted-foreground" />
          {t(label)}
        </CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent>
        <FieldGroup>{children}</FieldGroup>
      </CardContent>
    </Card>
  );
}

/** Popis s riadkom navyse pod nim (co server v priecinku nasiel). */
function Hint({ text, found }: { text: string; found: ReactNode }) {
  return (
    <>
      {text}
      <span className="mt-1 block text-foreground/80">{found}</span>
    </>
  );
}

/** Label s oznacenim, ze sa zmena prejavi az po restarte. */
function AfterRestart({ children }: { children: ReactNode }) {
  return (
    <>
      {children}
      <RestartBadge />
    </>
  );
}

// porovnanie s ulozenym nastavenim - prazdne riadky priecinkov vynecha az server
function normalized(settings: SettingsDto): string {
  return JSON.stringify({ ...settings, virtualFolders: settings.virtualFolders.map((f) => f.trim()).filter(Boolean) });
}

export function SettingsPage() {
  const live = useSimulatorState();
  const fonts = useQuery(getFontsOptions()).data ?? [];
  const designs = useQuery(getDesignsOptions()).data ?? [];
  // rozpracovane nastavenie; null = ukazuje sa nastavenie servera
  const [draft, setDraft] = useState<SettingsDto | null>(null);
  // nove heslo certifikatu; null = nechat ulozene
  const [password, setPassword] = useState<string | null>(null);
  // nove heslo SMTP oznameni; null = nechat ulozene
  const [smtpPassword, setSmtpPassword] = useState<string | null>(null);
  // sluzbu Windows sa da po ulozeni hned restartovat
  const can = useCan();
  // restart z webu len pri sluzbe Windows a s opravnenim
  const service = useQuery(getServerInfoOptions()).data?.service === true && can(PERMISSIONS.server);
  const restart = useRestartServer();

  const update = useMutation({
    ...updateSettingsMutation(),
    onSuccess: async (result) => {
      clearFontCache();
      // formular sa vrati k nastaveniu servera az po jeho nacitani (bez mihnutia starych hodnot)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: getStateQueryKey() }),
        queryClient.invalidateQueries({ queryKey: getHttpsStatusQueryKey() }),
      ]);
      setDraft(null);
      setPassword(null);
      setSmtpPassword(null);
      if (result.restart)
        toast.info(t(service ? "settings.restart.service" : "settings.restart"), {
          duration: 10_000,
          action: service ? { label: t("server.restart"), onClick: () => void restart.run() } : undefined,
        });
      else toast.success(t("settings.saved"));
    },
  });

  const saved = live.settings;
  const form = draft ?? saved;
  const dirty = form !== null && saved !== null && (password !== null || smtpPassword !== null || normalized(form) !== normalized(saved));

  useBlocker({
    shouldBlockFn: async () =>
      dirty && !(await confirmAction({ title: t("settings.leave.title"), description: t("settings.leave.text"), confirmLabel: t("settings.leave.confirm") })),
    enableBeforeUnload: () => dirty,
  });

  if (!form) return null;

  const change = (patch: Partial<SettingsDto>) => setDraft({ ...form, ...patch });
  const discard = () => {
    setDraft(null);
    setPassword(null);
    setSmtpPassword(null);
    update.reset();
  };
  const scrollTo = (id: SectionId) => document.getElementById(`settings-${id}`)?.scrollIntoView({ behavior: "smooth", block: "start" });

  return (
    <>
      <PageHeader description={t("settings.hint")} />
      <div className="grid gap-6 lg:grid-cols-[11rem_minmax(0,48rem)]">
        <nav aria-label={t("nav.settings")} className="hidden lg:block">
          <ul className="sticky top-16 flex flex-col gap-0.5">
            {SECTIONS.filter((s) => can(s.permission)).map(({ id, label, icon: Icon }) => (
              <li key={id}>
                <Button variant="ghost" size="sm" className="w-full justify-start text-muted-foreground hover:text-foreground" onClick={() => scrollTo(id)}>
                  <Icon />
                  {t(label)}
                </Button>
              </li>
            ))}
          </ul>
        </nav>
        <form
          className="flex min-w-0 flex-col gap-6"
          onSubmit={(e) => {
            e.preventDefault();
            update.mutate({ body: { ...form, httpsPassword: password, alerts: alertsRequest(form.alerts, smtpPassword) } });
          }}
        >
          <Section id="folders" description={t("settings.folders.hint")}>
            <FormField
              label={t("settings.fontsFolder")}
              hint={<Hint text={t("settings.fontsFolder.hint")} found={<FontsFound fonts={fonts} folder={saved?.fontsFolder ?? ""} />} />}
            >
              <Input value={form.fontsFolder} onChange={(e) => change({ fontsFolder: e.target.value })} />
            </FormField>
            <FormField label={t("settings.monitorFolder")} hint={t("settings.monitorFolder.hint")}>
              <Input value={form.monitorFolder} onChange={(e) => change({ monitorFolder: e.target.value })} />
            </FormField>
            <FormField
              label={<AfterRestart>{t("settings.designsFolder")}</AfterRestart>}
              hint={
                <Hint text={t("settings.designsFolder.hint", { folder: "%AppData%\\TableSimulator\\designs" })} found={<DesignsFound designs={designs} />} />
              }
            >
              <Input value={form.designsFolder} onChange={(e) => change({ designsFolder: e.target.value })} />
            </FormField>
            <FormField label={t("settings.virtualFolders")} hint={t("settings.virtualFolders.hint")}>
              <Textarea
                rows={3}
                value={form.virtualFolders.join("\n")}
                placeholder={"C:\\INISS\\Logy"}
                onChange={(e) => change({ virtualFolders: e.target.value.split(/\r?\n/) })}
              />
            </FormField>
            <FormField label={t("settings.inissViewFolder")} hint={t("settings.inissViewFolder.hint")}>
              <Input placeholder={"C:\\INISS\\INISSview"} value={form.inissViewFolder} onChange={(e) => change({ inissViewFolder: e.target.value })} />
            </FormField>
          </Section>

          <Section id="iniss" description={t("settings.iniss.hint")}>
            <FormField label={t("settings.inissProgram")} hint={t("settings.inissProgram.hint")}>
              <Input placeholder={"C:\\INISS\\INISS.exe"} value={form.inissProgram} onChange={(e) => change({ inissProgram: e.target.value })} />
            </FormField>
            <FormField label={t("settings.inissRegistry")} hint={t("settings.inissRegistry.hint")}>
              <Input className="max-w-64" value={form.inissRegistry} onChange={(e) => change({ inissRegistry: e.target.value })} />
            </FormField>
            <CheckField
              label={t("settings.inissElevated")}
              hint={t("settings.inissElevated.hint")}
              checked={form.inissElevated}
              onCheckedChange={(value) => change({ inissElevated: value })}
            />
          </Section>

          <Section id="network" description={t("settings.network.hint")}>
            <FormField label={<AfterRestart>{t("settings.httpPort")}</AfterRestart>}>
              <Input
                type="number"
                min={1}
                max={65535}
                className="max-w-32"
                value={form.httpPort}
                onChange={(e) => change({ httpPort: Number.parseInt(e.target.value, 10) || 0 })}
              />
            </FormField>
            <CheckField
              label={<AfterRestart>{t("settings.allowNetwork")}</AfterRestart>}
              checked={form.allowNetwork}
              onCheckedChange={(value) => change({ allowNetwork: value })}
            />
            <CheckField
              label={<AfterRestart>{t("settings.requireLogin")}</AfterRestart>}
              hint={t("settings.requireLogin.hint")}
              checked={form.requireLogin || form.allowNetwork}
              disabled={form.allowNetwork}
              onCheckedChange={(value) => change({ requireLogin: value })}
            />
            <FormField label={t("settings.connectHost")} hint={t("settings.connectHost.hint")}>
              <Input className="max-w-64" placeholder="127.0.0.1" value={form.connectHost} onChange={(e) => change({ connectHost: e.target.value })} />
            </FormField>
          </Section>

          <Section id="https" description={t("settings.https.hint")}>
            <HttpsSection form={form} change={change} password={password} setPassword={setPassword} />
          </Section>

          <Section id="alerts" description={t("settings.alerts.hint")}>
            <AlertsSection
              alerts={form.alerts}
              change={(patch) => change({ alerts: { ...form.alerts, ...patch } })}
              smtpPassword={smtpPassword}
              setSmtpPassword={setSmtpPassword}
            />
          </Section>

          <Section id="server" description={t("settings.server.hint")}>
            <ServerSection databasePath={form.databasePath} />
          </Section>

          <Section id="database" description={t("database.section.hint")}>
            <DatabaseSection />
          </Section>

          <Section id="backup" description={t("settings.backup.hint")}>
            <BackupSection />
          </Section>

          {/* lista ostava na spodku okna - aj chyba ulozenia je viditelna pri ktorejkolvek casti */}
          <div hidden={!can(PERMISSIONS.settings)} className="sticky bottom-0 z-10 -mx-1 flex flex-col gap-3 border-t bg-background/95 px-1 py-3 backdrop-blur">
            {update.error && (
              <Alert variant="destructive">
                <CircleAlert />
                <AlertDescription className="whitespace-pre-line">{errorText(update.error)}</AlertDescription>
              </Alert>
            )}
            <div className="flex flex-wrap items-center justify-end gap-2">
              <span className="mr-auto text-sm text-muted-foreground">{dirty && t("settings.unsaved")}</span>
              <Button type="button" variant="outline" disabled={!dirty || update.isPending} onClick={discard}>
                <Undo2 />
                {t("settings.discard")}
              </Button>
              <Button type="submit" disabled={!dirty || update.isPending}>
                {update.isPending ? <Spinner /> : <Save />}
                {t("common.save")}
              </Button>
            </div>
          </div>
        </form>
      </div>
    </>
  );
}
