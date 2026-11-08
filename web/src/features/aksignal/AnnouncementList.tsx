// Hlasenia zariadeni (ustredna AK Signal, rozhlas vzdialeneho INISSu): prave hrajuce s priebehom a posledne hlasenia.
import type { AnnouncementDto } from "@/api";
import { Progress } from "@/components/ui/progress";
import { useNow } from "@/hooks/use-now";
import { locale, t } from "@/i18n";
import { elapsed, progress, recentAnnouncements } from "./aksignal";
import { queueLabel } from "./queue-label";

const time = (value: string | null | undefined) => (value ? new Date(value).toLocaleTimeString(locale()) : "–");

/** Prave hrajuce hlasenie: titulok, nahravky, priebeh a cas od zaciatku. */
export function PlayingAnnouncement({ announcement }: { announcement: AnnouncementDto }) {
  const now = useNow(250);
  const done = progress(announcement, now);
  return (
    <div className="flex min-w-0 flex-col gap-1">
      {announcement.title && <span className="truncate text-sm">{announcement.title}</span>}
      <span className="truncate font-mono text-xs">{announcement.sounds.join(" · ")}</span>
      {done !== null && <Progress value={Math.round(done * 100)} aria-label={t("aksignal.progress")} />}
      <span className="text-xs text-muted-foreground">
        {t("aksignal.playing.since", { time: time(announcement.startedAt), seconds: Math.floor(elapsed(announcement, now) / 1000) })}
        {announcement.circuit && <> · {t("remoteaudio.circuit", { circuit: announcement.circuit })}</>}
      </span>
    </div>
  );
}

/** Poslednych `count` hlaseni od najnovsieho; pri vzdialenom INISSe aj fronta (ak ich je viac). */
export function AnnouncementList({ announcements, count = 8, queues = false }: { announcements: AnnouncementDto[]; count?: number; queues?: boolean }) {
  const items = recentAnnouncements({ announcements }, count);
  if (items.length === 0) return <p className="text-sm text-muted-foreground">{t("aksignal.announcements.none")}</p>;
  return (
    <ul className="flex flex-col gap-1 text-xs">
      {items.map((a) => (
        <li key={a.id} className="flex flex-wrap gap-x-2">
          <span className="text-muted-foreground tabular-nums">{time(a.startedAt)}</span>
          {a.channel > 0 && <span>{t("aksignal.channel", { channel: a.channel })}</span>}
          {queues && a.queue !== null && <span>{queueLabel(a.queue)}</span>}
          <span className="min-w-0 flex-1 truncate font-mono" title={a.sounds.join(" · ")}>
            {a.title ?? (a.sounds.length > 0 ? a.sounds.join(" · ") : t("aksignal.audio.value", { value: a.value ?? "–" }))}
          </span>
          <span className={a.result === "cancelled" ? "text-red-700 dark:text-red-400" : "text-muted-foreground"}>
            {t(a.queue !== null && a.result === "cancelled" ? "remoteaudio.result.break" : `aksignal.result.${a.result}`)}
          </span>
        </li>
      ))}
    </ul>
  );
}
