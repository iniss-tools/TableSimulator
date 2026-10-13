// Udaje zobrazenia tabule pre aplikaciu MCP Apps - vysledok nastroja board_view servera (Mcp/AppTools.cs).
import type { BoardDto, BoardFaceDto, FontFileDto } from "@/api";
import { completeDesign, designForFamily, type BoardDesign } from "@/render/design";
import { resolveFace } from "@/render/face";
import { primeFontSet } from "@/render/fonts";

/** Vysledok board_view (kluce camelCase ako ostatne vysledky nastrojov). */
export interface BoardViewData {
  changed: boolean;
  board: BoardDto;
  /** Obsah design.json pouziteho vzhladu; null = predvoleny renderera. */
  design: Partial<BoardDesign> | null;
  /** Predloha cela tabule; null = bez predlohy (moze mat hlavicku z profilu). */
  face: BoardFaceDto | null;
  /** Subor pisiem ELEN - len ked ho aplikacia este nema (knownFontFile). */
  font: FontFileDto | null;
}

/** Co BoardView potrebuje - vzhlad aj celo dane, nic sa nestahuje (ramec MCP Apps nesmie na siet). */
export interface BoardViewState {
  board: BoardDto;
  design: BoardDesign;
  face: BoardFaceDto | null;
}

/** Stav zobrazenia z vysledku board_view; pismo vlozi do pamate renderera. */
export function viewState(data: BoardViewData): BoardViewState {
  if (data.font) primeFontSet(data.font);
  const { board } = data;
  return {
    board,
    design: designForFamily(completeDesign(data.design ?? {}), board.display.family),
    face: resolveFace(board.profile.face, board.profile.header, data.face ? [data.face] : []),
  };
}
