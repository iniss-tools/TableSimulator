// Karta tabule: zive zobrazenie, snimka do PNG, stav komunikacie, upozornenie na ineho vyrobcu a predstierana porucha.
import { useRef, type RefObject } from "react";
import { Camera, Copy, Download, Eraser, Maximize, Pencil, Trash2, TriangleAlert } from "lucide-react";
import { toast } from "sonner";
import { clearBoard, deleteBoard, errorText, setBoard, setBoardFault, type BoardDto, type BoardFaultDto, type FaultKind } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { OptionSelect } from "@/components/common/option-select";
import { StatusBadge } from "@/components/common/status-badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { boardConfig } from "@/lib/boards";
import { saveFile } from "@/lib/download";
import { fileStamp } from "@/lib/format";
import { cn } from "@/lib/utils";
import { useNow } from "@/hooks/use-now";
import { hasText, locale, t } from "@/i18n";
import { BoardView, type BoardViewHandle } from "./BoardView";

const FAULTS: FaultKind[] = ["none", "noReply", "nak", "delay", "garbage", "status"];

/** Poruchy, ktore tabula danej rodiny vie predstierat - stav hlasia len tabule so stavovou odpovedou. */
// stav (kod odpovede) maju len tabule s odpovedou na kazdu spravu - Elektrocas, listove, APEL a jednotky ELSVO
const STATUS_FAMILIES = ["chars", "flaps", "amplifier"];
const faultsFor = (board: BoardDto) => FAULTS.filter((f) => f !== "status" || STATUS_FAMILIES.includes(board.display.family));

const failed = (e: unknown) => toast.error(errorText(e));

/** INISS posiela tabuli spravy pre ineho vyrobcu - dovod a prepnutie na spravneho. */
function MismatchWarning({ board, edit }: { board: BoardDto; edit: boolean }) {
  const mismatch = board.mismatch!;
  const key = `mismatch.${mismatch.evidence}`;
  const suggested = mismatch.suggested.join(", ");
  return (
    <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
      <TriangleAlert />
      <AlertTitle>{t("mismatch.title")}</AlertTitle>
      <AlertDescription className="text-current">
        {hasText(key) && <p>{t(key, { suggested })}</p>}
        {edit && (
          <div className="mt-2 flex flex-wrap gap-2">
            {mismatch.suggested.map((m) => (
              <Button
                key={m}
                size="sm"
                variant="outline"
                onClick={() =>
                  setBoard({ path: { line: board.line, address: board.address }, body: { ...boardConfig(board), manufacturer: m }, throwOnError: true }).catch(
                    failed,
                  )
                }
              >
                {t("mismatch.switch", { manufacturer: m })}
              </Button>
            ))}
          </div>
        )}
      </AlertDescription>
    </Alert>
  );
}

/** Predstierana porucha: druh a pri oneskoreni cas, pri stavovej odpovedi stavove bity (hex). */
function FaultControl({ board, edit }: { board: BoardDto; edit: boolean }) {
  const fault = (body: BoardFaultDto) => setBoardFault({ path: { id: board.id }, body, throwOnError: true }).catch(failed);
  const setKind = (kind: FaultKind) =>
    fault({
      kind,
      delayMs: kind === "delay" ? Math.max(board.fault.delayMs, 500) : 0,
      // predvolene "chybna sprava", pri zosilnovaci "pripravena, ale nezopnuta"
      status: kind === "status" ? board.fault.status || (board.display.family === "amplifier" ? 0x80 : 0x01) : 0,
    });

  return (
    <div className="flex items-center gap-2">
      <span className="text-sm">{t("fault.label")}</span>
      <OptionSelect<FaultKind>
        value={board.fault.kind}
        onValueChange={setKind}
        disabled={!edit}
        size="sm"
        ariaLabel={t("fault.label")}
        options={faultsFor(board).map((f) => ({ value: f, label: t(`fault.${f}`) }))}
      />
      {board.fault.kind === "delay" && (
        <Input
          type="number"
          className="h-7 w-24"
          min={0}
          step={100}
          disabled={!edit}
          value={board.fault.delayMs}
          title={t("fault.delayMs")}
          aria-label={t("fault.delayMs")}
          onChange={(e) => fault({ kind: "delay", delayMs: Number.parseInt(e.target.value, 10) || 0, status: 0 })}
        />
      )}
      {board.fault.kind === "status" && (
        <Input
          className="h-7 w-16 font-mono"
          disabled={!edit}
          defaultValue={board.fault.status.toString(16).toUpperCase().padStart(2, "0")}
          title={t("fault.statusBits")}
          aria-label={t("fault.statusBits")}
          onBlur={(e) => {
            const status = Number.parseInt(e.target.value, 16);
            if (!Number.isNaN(status)) void fault({ kind: "status", delayMs: 0, status: status & 0xff });
          }}
        />
      )}
    </div>
  );
}

/** Snimka tabule: ulozit ako PNG, alebo (cez HTTPS a na tomto pocitaci) skopirovat do schranky - napr. do hlasenia chyby. */
function SnapshotMenu({ board, view }: { board: BoardDto; view: RefObject<BoardViewHandle | null> }) {
  const save = () =>
    view.current
      ?.snapshot()
      .then((blob) => saveFile(blob, `${t("board.snapshot.file", { id: board.id, time: fileStamp(new Date()) })}.png`))
      .catch(failed);
  const canCopy = typeof ClipboardItem !== "undefined" && isSecureContext;
  if (!canCopy)
    return (
      <Button variant="ghost" size="icon-sm" title={t("board.snapshot")} onClick={save}>
        <Camera />
      </Button>
    );

  // schranka dostane slub PNG hned pri kliknuti - prehliadac inak zapis bez kliknutia odmietne
  const copy = () => {
    const png = view.current?.snapshot();
    if (!png) return;
    navigator.clipboard
      .write([new ClipboardItem({ "image/png": png })])
      .then(() => toast.success(t("board.snapshot.copied")))
      .catch(failed);
  };
  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" title={t("board.snapshot")} />}>
        <Camera />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-48">
        <DropdownMenuItem onClick={save}>
          <Download />
          {t("board.snapshot.save")}
        </DropdownMenuItem>
        <DropdownMenuItem onClick={copy}>
          <Copy />
          {t("board.snapshot.copy")}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** Listova tabula: listy sa po zapise otacaju. */
function Rotating({ until }: { until: string }) {
  const now = useNow(500);
  return now < Date.parse(until) ? <span className="font-medium text-amber-700 dark:text-amber-300"> · {t("board.rotating")}</span> : null;
}

/** Zosilnovac: od kedy je zopnuty a ako dlho. */
function AmplifierOn({ since }: { since: string }) {
  const now = useNow(1000);
  const seconds = Math.max(0, Math.floor((now - Date.parse(since)) / 1000));
  return (
    <span className="font-medium text-green-700 dark:text-green-400">
      {" "}
      · {t("amplifier.since", { time: new Date(since).toLocaleTimeString(locale()), seconds })}
    </span>
  );
}

/** Hodiny tabule - idu od nastavenia dalej. */
function BoardClock({ clock, setAt }: { clock: string; setAt: string }) {
  const now = useNow(1000);
  return <> · {t("board.clock", { time: new Date(Date.parse(clock) + (now - Date.parse(setAt))).toLocaleString(locale()) })}</>;
}

/** Karta tabule; `edit` = uprava a mazanie (linky a tabule), `control` = poruchy a vymazanie obsahu. */
export function BoardCard({ board, onEdit, edit, control }: { board: BoardDto; onEdit: () => void; edit: boolean; control: boolean }) {
  const name = board.name || t(board.display.family === "amplifier" ? "amplifier.unnamed" : "board.unnamed", { address: board.address });
  const last = board.stats.lastFrameAt ? new Date(board.stats.lastFrameAt).toLocaleTimeString(locale()) : null;
  const d = board.display;
  const view = useRef<BoardViewHandle>(null);

  const remove = async () => {
    if (await confirmAction({ title: t("board.delete.confirm", { name }), destructive: true }))
      deleteBoard({ path: { id: board.id }, throwOnError: true }).catch(failed);
  };

  return (
    <Card className={cn("gap-3", board.fault.kind !== "none" && "ring-2 ring-red-500/50")}>
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div className="min-w-0">
          <CardTitle className="truncate">{name}</CardTitle>
          <div className="text-sm text-muted-foreground">
            {board.secondAddress
              ? t("board.lineAddress2", { line: board.line, address: board.address, second: board.secondAddress })
              : t("board.lineAddress", { line: board.line, address: board.address })}{" "}
            · {board.manufacturer}
            {board.autoCreated && <> · {t("board.auto")}</>}
          </div>
          {board.poweredOff && (
            <div className="mt-1">
              <StatusBadge tone="bad">{t("board.poweredOff")}</StatusBadge>
            </div>
          )}
        </div>
        <div className="flex shrink-0 items-center">
          <SnapshotMenu board={board} view={view} />
          <Button variant="ghost" size="icon-sm" title={t("board.fullscreen")} render={<a href={`#/view/${board.id}`} />}>
            <Maximize />
          </Button>
          {edit && (
            <Button variant="ghost" size="icon-sm" title={t("common.edit")} onClick={onEdit}>
              <Pencil />
            </Button>
          )}
          {control && (
            <Button
              variant="ghost"
              size="icon-sm"
              title={t("board.clear")}
              onClick={() => clearBoard({ path: { id: board.id }, throwOnError: true }).catch(failed)}
            >
              <Eraser />
            </Button>
          )}
          {edit && (
            <Button variant="ghost" size="icon-sm" title={t("common.delete")} onClick={remove}>
              <Trash2 />
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {board.mismatch && <MismatchWarning board={board} edit={edit} />}
        <BoardView ref={view} board={board} className={cn(board.poweredOff && "opacity-40")} />
        {!board.profile.fontFile && d.family === "elen" && <p className="text-xs text-muted-foreground">{t("board.approxFont")}</p>}
        {d.family === "flaps" && !board.profile.flaps && <p className="text-xs text-muted-foreground">{t("board.flaps.codes")}</p>}
      </CardContent>
      {/* stav tabule vzdy na spodku karty - karty v riadku mriezky maju rovnaku vysku */}
      <CardFooter className="mt-auto flex flex-wrap items-center justify-between gap-2">
        <span className="text-xs text-muted-foreground">
          {last ? t("board.lastFrame", { time: last }) : t("board.noFrames")} ·{" "}
          {t("board.stats", { frames: board.stats.frames, acks: board.stats.acks, naks: board.stats.naks })}
          {d.family !== "flaps" && d.family !== "amplifier" && <> · {d.brightness > 0 ? t("board.brightness", { value: d.brightness }) : t("board.dark")}</>}
          {d.rotatingUntil && <Rotating until={d.rotatingUntil} />}
          {d.amplifier?.onSince && <AmplifierOn since={d.amplifier.onSince} />}
          {d.clock && d.clockSetAt && <BoardClock clock={d.clock} setAt={d.clockSetAt} />}
        </span>
        <FaultControl board={board} edit={control} />
      </CardFooter>
    </Card>
  );
}
