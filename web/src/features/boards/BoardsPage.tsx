// Prehlad tabul: stav liniek, zive zobrazenie, stav komunikacie, poruchy; pridanie tabule a import z ELEN Monitora.
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileInput, LayoutGrid, Maximize, Plus } from "lucide-react";
import { getMonitorOptions, PERMISSIONS, useCan, useSimulatorState, type SetBoardRequest } from "@/api";
import { Button } from "@/components/ui/button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { ALL_LINES } from "@/features/lines/line-status";
import { boardConfig, boardKind, defaultProfile } from "@/lib/boards";
import { t } from "@/i18n";
import { BoardCard } from "./BoardCard";
import { BoardDialog } from "./BoardDialog";
import { LineStrip } from "./LineStrip";
import { MonitorImportDialog } from "./MonitorImportDialog";

function newBoard(line: number, manufacturer: string): SetBoardRequest {
  return {
    line,
    address: 1,
    manufacturer,
    name: "",
    design: null,
    fault: { kind: "none", delayMs: 0, status: 0 },
    secondAddress: null,
    profile: defaultProfile(manufacturer),
  };
}

export function BoardsPage() {
  const live = useSimulatorState();
  const can = useCan();
  const edit = can(PERMISSIONS.lines);
  const control = can(PERMISSIONS.boards);
  const [filter, setFilter] = useState(ALL_LINES);
  const [editing, setEditing] = useState<{ config: SetBoardRequest; isNew: boolean } | null>(null);
  const monitor = useQuery(getMonitorOptions()).data;
  const [importing, setImporting] = useState(false);

  const boards = live.boards.filter((b) => filter === ALL_LINES || b.line === filter);
  // nova tabula patri na linku vybranu v pase liniek, inak na prvu
  const targetLine = live.lines.find((l) => l.number === filter) ?? live.lines[0];
  // typy panelov ELEN Monitora su tabule ELEN - import ma zmysel len pre linku s tabulami ELEN
  const elenLines = live.lines.filter((l) => boardKind(l.manufacturer) === "elen");

  return (
    <>
      <div className="mb-4 flex flex-wrap items-start gap-2">
        <LineStrip lines={live.lines} filter={filter} onFilter={setFilter} />
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <Button variant="outline" render={<a href="#/wall" />}>
            <Maximize />
            {t("nav.wall")}
          </Button>
          {edit && monitor?.available && monitor.projects.length > 0 && elenLines.length > 0 && (
            <Button variant="outline" onClick={() => setImporting(true)}>
              <FileInput />
              {t("monitor.import")}
            </Button>
          )}
          {edit && (
            <Button
              disabled={!targetLine}
              onClick={() => targetLine && setEditing({ config: newBoard(targetLine.number, targetLine.manufacturer), isNew: true })}
            >
              <Plus />
              {t("boards.add")}
            </Button>
          )}
        </div>
      </div>

      {boards.length === 0 ? (
        <Empty className="border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <LayoutGrid />
            </EmptyMedia>
            <EmptyTitle>{t("boards.empty.title")}</EmptyTitle>
            <EmptyDescription>{t("boards.empty.text")}</EmptyDescription>
          </EmptyHeader>
          {live.lines.length > 0 && (
            <EmptyContent>
              {live.lines.map((l) => (
                <code key={l.number} className="rounded bg-muted px-2 py-1 font-mono text-xs">
                  TableClass={l.tableClass} · TablePort={l.inissTablePort}
                </code>
              ))}
            </EmptyContent>
          )}
        </Empty>
      ) : (
        <div className="grid grid-cols-[repeat(auto-fill,minmax(min(100%,560px),1fr))] gap-4">
          {boards.map((b) => (
            <BoardCard key={b.id} board={b} edit={edit} control={control} onEdit={() => setEditing({ config: boardConfig(b), isNew: false })} />
          ))}
        </div>
      )}

      {editing && <BoardDialog initial={editing.config} isNew={editing.isNew} onClose={() => setEditing(null)} />}
      {importing && monitor && <MonitorImportDialog monitor={monitor} lines={elenLines} initialLine={targetLine?.number} onClose={() => setImporting(false)} />}
    </>
  );
}
