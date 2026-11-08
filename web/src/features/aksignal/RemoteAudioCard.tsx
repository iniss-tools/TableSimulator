// Rozhlas vzdialeneho INISSu: fronty hlaseni (vypnuta, volna, hra), prave hrajuce hlasenie s prerusenim a posledne hlasenia.
import { toast } from "sonner";
import { breakBoardAnnouncement, errorText, type BoardDto, type RemoteQueueDto } from "@/api";
import { StatusBadge } from "@/components/common/status-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { locale, t } from "@/i18n";
import { AnnouncementList, PlayingAnnouncement } from "./AnnouncementList";
import { queueLabel } from "./queue-label";
import { queueState } from "./remote-audio";

const time = (value: string | null | undefined) => (value ? new Date(value).toLocaleTimeString(locale()) : "–");

const TONE = { off: "bad", idle: "off", playing: "ok" } as const;

function Queue({ board, queue, control }: { board: BoardDto; queue: RemoteQueueDto; control: boolean }) {
  const state = queueState(queue);
  const stop = () =>
    void breakBoardAnnouncement({ path: { id: board.id }, query: { queue: queue.name }, throwOnError: true }).catch((e: unknown) => toast.error(errorText(e)));
  return (
    <div className="grid grid-cols-[minmax(6rem,auto)_minmax(0,1fr)_auto] items-start gap-x-3 gap-y-1 text-sm">
      <div className="flex flex-col items-start gap-1">
        <span className="font-medium">{queueLabel(queue.name)}</span>
        <StatusBadge tone={TONE[state]}>{t(`remoteaudio.queue.${state}`)}</StatusBadge>
      </div>
      {queue.playing ? <PlayingAnnouncement announcement={queue.playing} /> : <span />}
      {queue.playing && control ? (
        <Button variant="outline" size="sm" title={t("remoteaudio.break.hint")} onClick={stop}>
          {t("remoteaudio.break")}
        </Button>
      ) : (
        <span />
      )}
    </div>
  );
}

/** Karta rozhlasu vzdialeneho INISSu; `control` = prerusenie hlasenia. */
export function RemoteAudioCard({ board, control }: { board: BoardDto; control: boolean }) {
  const state = board.display.remoteAudio;
  const queues = state?.queues ?? [];
  return (
    <Card>
      <CardHeader>
        <CardTitle>{board.name || t("remoteaudio.unnamed", { address: board.address })}</CardTitle>
        <CardDescription>
          {t("remoteaudio.where", { line: board.line })} · {t("aksignal.lastQuery", { time: time(state?.lastQueryAt) })}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-5">
        {board.poweredOff && (
          <div>
            <StatusBadge tone="bad">{t("board.poweredOff")}</StatusBadge>
          </div>
        )}
        <section className="flex flex-col gap-3">
          <h3 className="text-sm font-medium">{t("remoteaudio.queues")}</h3>
          {queues.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t("remoteaudio.queues.none")}</p>
          ) : (
            queues.map((queue) => <Queue key={queue.name} board={board} queue={queue} control={control} />)
          )}
        </section>
        <section className="flex flex-col gap-2">
          <h3 className="text-sm font-medium">{t("remoteaudio.announcements")}</h3>
          <AnnouncementList announcements={state?.announcements ?? []} queues={queues.length > 1} />
        </section>
      </CardContent>
    </Card>
  );
}
