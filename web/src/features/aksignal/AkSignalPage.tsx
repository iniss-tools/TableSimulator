// Ustredne AK Signal servera INISS2 (svetla nastupist, rozhlas a hlasenia, snimac tmy a stavy ustredne) a rozhlas
// vzdialeneho INISSu 3.39 (fronty a hlasenia).
import { useState } from "react";
import { Lightbulb } from "lucide-react";
import { toast } from "sonner";
import {
  errorText,
  PERMISSIONS,
  setBoardAkSignal,
  useCan,
  useSimulatorState,
  type AkSignalConditionsDto,
  type AkSignalDto,
  type BoardDto,
  type TwilightMode,
} from "@/api";
import { CheckField, FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { PageHeader } from "@/components/common/page-header";
import { StatusBadge } from "@/components/common/status-badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { Input } from "@/components/ui/input";
import { useNow } from "@/hooks/use-now";
import { locale, t, type TextKey } from "@/i18n";
import { AmplifiersCard } from "./AmplifiersCard";
import { AnnouncementList, PlayingAnnouncement } from "./AnnouncementList";
import { HistoryTimeline } from "./HistoryTimeline";
import { RemoteAudioCard } from "./RemoteAudioCard";
import { akSignalProfile, isUh, lightMode, playingOn, settingsRequest } from "./aksignal";

const MODES: TwilightMode[] = ["sun", "dark", "light"];

/** Stavy ustredne v poradi na stranke; posledne dve ma len AKSIGNALUH. */
const CONDITIONS: { key: keyof AkSignalConditionsDto; uhOnly?: boolean }[] = [
  { key: "lightsFault" },
  { key: "highPriority" },
  { key: "outputPowerError" },
  { key: "ampSupplyMissing" },
  { key: "ampCommError", uhOnly: true },
  { key: "bankNotSynced", uhOnly: true },
];

const failed = (e: unknown) => toast.error(errorText(e));

const time = (value: string | null | undefined) => (value ? new Date(value).toLocaleTimeString(locale()) : "–");

/** Cas zmeny snimaca - na minuty. */
const minutes = (value: string) => new Date(value).toLocaleTimeString(locale(), { hour: "2-digit", minute: "2-digit" });

function StateBadges({ state }: { state: AkSignalDto }) {
  // porucha z nastavenia aj zo scenara - tak, ako ustredna odpoveda
  const fault = state.conditions.lightsFault;
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <StatusBadge tone={state.lightOn ? "ok" : "off"}>{t(state.lightOn ? "aksignal.on" : "aksignal.off")}</StatusBadge>
      <StatusBadge tone={lightMode(state) === "off" ? "off" : "wait"}>{t(`aksignal.mode.${lightMode(state)}`)}</StatusBadge>
      <StatusBadge tone="off">{t(state.dark ? "aksignal.dark" : "aksignal.day")}</StatusBadge>
      {fault && <StatusBadge tone="bad">{t("aksignal.condition.lightsFault")}</StatusBadge>}
      {state.serverSilent && <StatusBadge tone="bad">{t("aksignal.silent")}</StatusBadge>}
      {state.scenario && <StatusBadge tone="wait">{t("aksignal.scenario")}</StatusBadge>}
    </div>
  );
}

/** Snimac tmy: podla slnka s posunom, alebo natrvalo tma ci svetlo. */
function SensorControl({ board, state, control }: { board: BoardDto; state: AkSignalDto; control: boolean }) {
  const profile = akSignalProfile(board);
  const [offset, setOffset] = useState(String(profile.sensor.offsetMinutes));
  const save = (sensor: typeof profile.sensor) =>
    setBoardAkSignal({ path: { id: board.id }, body: settingsRequest(profile, { sensor }), throwOnError: true }).catch(failed);
  const commitOffset = () => {
    const value = Number.parseInt(offset, 10);
    if (Number.isFinite(value) && value !== profile.sensor.offsetMinutes) void save({ ...profile.sensor, offsetMinutes: value });
    else setOffset(String(profile.sensor.offsetMinutes));
  };
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      <FormField label={t("aksignal.sensor")}>
        <OptionSelect<TwilightMode>
          ariaLabel={t("aksignal.sensor")}
          value={profile.sensor.mode}
          onValueChange={(mode) => void save({ ...profile.sensor, mode })}
          options={MODES.map((mode) => ({ value: mode, label: t(`aksignal.sensor.${mode}`) }))}
          disabled={!control}
          className="w-full"
        />
      </FormField>
      {profile.sensor.mode === "sun" && (
        <FormField label={t("aksignal.sensor.offset")} hint={t("aksignal.sensor.offset.hint")}>
          <Input
            type="number"
            min={-240}
            max={240}
            value={offset}
            disabled={!control}
            onChange={(e) => setOffset(e.target.value)}
            onBlur={commitOffset}
            onKeyDown={(e) => e.key === "Enter" && commitOffset()}
          />
        </FormField>
      )}
      {state.scenario?.sensor && (
        <p className="text-xs text-muted-foreground sm:col-span-2">{t("aksignal.sensor.scenario", { mode: t(`aksignal.sensor.${state.scenario.sensor}`) })}</p>
      )}
      {state.darkChangesAt && (
        <p className="text-xs text-muted-foreground sm:col-span-2">
          {t(state.dark ? "aksignal.sensor.lightAt" : "aksignal.sensor.darkAt", { time: minutes(state.darkChangesAt) })}
        </p>
      )}
    </div>
  );
}

/** Stavy ustredne, ktore server cita, ale nemeni; stav pridany beziacim scenarom ma poznamku. */
function ConditionsControl({ board, state, control }: { board: BoardDto; state: AkSignalDto | null; control: boolean }) {
  const profile = akSignalProfile(board);
  const uh = isUh(board);
  const set = (key: keyof AkSignalConditionsDto, value: boolean) =>
    void setBoardAkSignal({
      path: { id: board.id },
      body: settingsRequest(profile, { conditions: { ...profile.conditions, [key]: value } }),
      throwOnError: true,
    }).catch(failed);
  return (
    <div className="grid gap-2 sm:grid-cols-2">
      {CONDITIONS.filter((c) => uh || !c.uhOnly).map((c) => (
        <CheckField
          key={c.key}
          label={t(`aksignal.condition.${c.key}` as TextKey)}
          checked={profile.conditions[c.key]}
          hint={state?.scenario?.conditions[c.key] && !profile.conditions[c.key] ? t("aksignal.fromScenario") : undefined}
          onCheckedChange={(value) => set(c.key, value)}
          disabled={!control}
        />
      ))}
    </div>
  );
}

/** Rozhlas: pri AKSIGNAL len zapnutie s hodnotou, pri AKSIGNALUH kanaly 1 a 2 so zvukmi a zosuladenie banky. */
function Audio({ board, state }: { board: BoardDto; state: AkSignalDto }) {
  const now = useNow(1000);
  const profile = akSignalProfile(board);
  if (!isUh(board)) {
    const on = playingOn(state, 0);
    return (
      <p className="text-sm">
        {on ? t("aksignal.audio.on", { value: on.value ?? "–", time: time(on.startedAt) }) : t("aksignal.audio.off")}
        <span className="text-xs text-muted-foreground"> · {t("aksignal.audio.state", { state: state.audioState })}</span>
      </p>
    );
  }

  const banks = Object.entries(state.banks);
  const syncing = state.syncUntil !== null && Date.parse(state.syncUntil) > now;
  return (
    <div className="flex flex-col gap-3">
      {[1, 2].map((channel) => {
        const on = playingOn(state, channel);
        const lines = profile.audio.filter((a) => a.channel === channel).map((a) => a.name);
        return (
          <div key={channel} className="grid grid-cols-[auto_minmax(0,1fr)] items-start gap-x-3 text-sm">
            <span className="font-medium">{t("aksignal.channel", { channel })}</span>
            {on ? <PlayingAnnouncement announcement={on} /> : <span className="text-muted-foreground">{t("aksignal.channel.idle")}</span>}
            {lines.length > 0 && <span className="col-start-2 text-xs text-muted-foreground">{lines.join(", ")}</span>}
          </div>
        );
      })}
      <p className="text-xs text-muted-foreground">
        {t("aksignal.audio.state", { state: state.audioState || "0" })}
        {state.syncedAt && <> · {t("aksignal.banks", { time: time(state.syncedAt), banks: banks.map(([lang, md5]) => `${lang} ${md5}`).join(", ") || "–" })}</>}
        {syncing && <> · {t("aksignal.syncing")}</>}
      </p>
    </div>
  );
}

/** `lastReceived` = posledna sprava na linke - bezny dotaz servera stav ustredne nemeni, do prehliadaca ide len stav linky. */
function ControllerCard({ board, control, lastReceived }: { board: BoardDto; control: boolean; lastReceived: string | null }) {
  const state = board.display.akSignal;
  const profile = akSignalProfile(board);
  const name = board.name || t("aksignal.unnamed", { address: board.address });
  return (
    <Card>
      <CardHeader>
        <CardTitle>{name}</CardTitle>
        <CardDescription>
          {isUh(board) ? t("aksignal.where.uh", { line: board.line }) : t("aksignal.where", { line: board.line, address: board.address })}
          {" · "}
          {t("aksignal.lastQuery", { time: time(lastReceived ?? state?.lightQueryAt ?? state?.audioQueryAt) })}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-5">
        {state && <StateBadges state={state} />}
        <section className="flex flex-col gap-2">
          <h3 className="text-sm font-medium">{t("aksignal.lights")}</h3>
          {profile.lights.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t("aksignal.lights.none")}</p>
          ) : (
            <ul className="flex flex-col gap-0.5 text-sm">
              {profile.lights.map((light) => (
                <li key={`${light.station}/${light.name}`}>
                  {light.name}
                  <span className="text-xs text-muted-foreground">
                    {" "}
                    · {t("aksignal.light.detail", { station: light.station, before: light.beforeArrival, after: light.afterDeparture, minOff: light.minOff })}
                  </span>
                </li>
              ))}
            </ul>
          )}
          {state && <SensorControl key={`${profile.sensor.mode}/${profile.sensor.offsetMinutes}`} board={board} state={state} control={control} />}
        </section>
        <section className="flex flex-col gap-2">
          <h3 className="text-sm font-medium">{t("aksignal.audio")}</h3>
          {state && <Audio board={board} state={state} />}
          {state && <AnnouncementList announcements={state.announcements} />}
        </section>
        <section className="flex flex-col gap-2">
          <h3 className="text-sm font-medium">{t("aksignal.conditions")}</h3>
          <ConditionsControl board={board} state={state} control={control} />
        </section>
      </CardContent>
    </Card>
  );
}

export function AkSignalPage() {
  const live = useSimulatorState();
  const can = useCan();
  const control = can(PERMISSIONS.boards);
  const controllers = live.boards.filter((b) => b.display.family === "aksignal");
  const remotes = live.boards.filter((b) => b.display.family === "remoteaudio");
  const amplifiers = live.boards.filter((b) => b.display.family === "amplifier");
  const devices = [...controllers, ...remotes];
  return (
    <>
      <PageHeader description={t("aksignal.page.hint")} />
      {live.loaded && devices.length === 0 && amplifiers.length === 0 ? (
        <Empty>
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <Lightbulb />
            </EmptyMedia>
            <EmptyTitle>{t("aksignal.empty")}</EmptyTitle>
            <EmptyDescription>{t("aksignal.empty.hint")}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="grid items-start gap-4 xl:grid-cols-2">
          {controllers.map((board) => (
            <ControllerCard
              key={board.id}
              board={board}
              control={control}
              lastReceived={live.lines.find((l) => l.number === board.line)?.lastReceivedAt ?? null}
            />
          ))}
          {remotes.map((board) => (
            <RemoteAudioCard key={board.id} board={board} control={control} />
          ))}
          {amplifiers.length > 0 && <AmplifiersCard boards={amplifiers} />}
        </div>
      )}
      {devices.length > 0 && (
        <div className="mt-4">
          <HistoryTimeline boards={devices} />
        </div>
      )}
    </>
  );
}
