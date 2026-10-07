// Karta tabule: zive zobrazenie, stav komunikacie, upozornenie na ineho vyrobcu a predstierana porucha.
import { Eraser, Maximize, Pencil, Trash2, TriangleAlert } from "lucide-react";
import { toast } from "sonner";
import { clearBoard, deleteBoard, errorText, setBoard, setBoardFault, type BoardDto, type BoardFaultDto, type FaultKind } from "@/api";
import { confirmAction } from "@/components/common/confirm";
import { OptionSelect } from "@/components/common/option-select";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { boardConfig } from "@/lib/boards";
import { cn } from "@/lib/utils";
import { hasText, locale, t } from "@/i18n";
import { BoardView } from "./BoardView";

const FAULTS: FaultKind[] = ["none", "noReply", "nak", "delay", "garbage", "status"];

/** Poruchy, ktore tabula danej rodiny vie predstierat - stav hlasia len tabule so stavovou odpovedou. */
const faultsFor = (board: BoardDto) => FAULTS.filter((f) => f !== "status" || board.display.family === "chars");

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
                  setBoard({ path: { line: board.line, address: board.address }, body: { ...boardConfig(board), manufacturer: m }, throwOnError: true }).catch(failed)
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
      // predvolene "chybna sprava"
      status: kind === "status" ? board.fault.status || 0x01 : 0,
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
            if (!Number.isNaN(status)) fault({ kind: "status", delayMs: 0, status: status & 0xff });
          }}
        />
      )}
    </div>
  );
}

export function BoardCard({ board, onEdit, edit }: { board: BoardDto; onEdit: () => void; edit: boolean }) {
  const name = board.name || t("board.unnamed", { address: board.address });
  const last = board.stats.lastFrameAt ? new Date(board.stats.lastFrameAt).toLocaleTimeString(locale()) : null;
  const d = board.display;
  // hodiny tabule idu od nastavenia dalej
  const clock = d.clock && d.clockSetAt ? new Date(Date.parse(d.clock) + (Date.now() - Date.parse(d.clockSetAt))).toLocaleString(locale()) : null;

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
        </div>
        <div className="flex shrink-0 items-center">
          <Button variant="ghost" size="icon-sm" title={t("board.fullscreen")} render={<a href={`#/view/${board.id}`} />}>
            <Maximize />
          </Button>
          {edit && (
            <>
              <Button variant="ghost" size="icon-sm" title={t("common.edit")} onClick={onEdit}>
                <Pencil />
              </Button>
              <Button variant="ghost" size="icon-sm" title={t("board.clear")} onClick={() => clearBoard({ path: { id: board.id }, throwOnError: true }).catch(failed)}>
                <Eraser />
              </Button>
              <Button variant="ghost" size="icon-sm" title={t("common.delete")} onClick={remove}>
                <Trash2 />
              </Button>
            </>
          )}
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {board.mismatch && <MismatchWarning board={board} edit={edit} />}
        <BoardView board={board} />
        {!board.profile.fontFile && <p className="text-xs text-muted-foreground">{t("board.approxFont")}</p>}
      </CardContent>
      <CardFooter className="flex flex-wrap items-center justify-between gap-2">
        <span className="text-xs text-muted-foreground">
          {last ? t("board.lastFrame", { time: last }) : t("board.noFrames")} · {t("board.stats", { frames: board.stats.frames, acks: board.stats.acks, naks: board.stats.naks })} ·{" "}
          {d.brightness > 0 ? t("board.brightness", { value: d.brightness }) : t("board.dark")}
          {clock && <> · {t("board.clock", { time: clock })}</>}
        </span>
        <FaultControl board={board} edit={edit} />
      </CardFooter>
    </Card>
  );
}
