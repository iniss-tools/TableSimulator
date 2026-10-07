// Linky a tabule: nastavenie z aktualneho stavu (na upravu) a predvoleny profil novej tabule.
import type { BoardDto, BoardProfileDto, LineDto, SetBoardRequest, SetLineRequest } from "../api/generated/types.gen";

/** Vyrobcovia znakovych tabul Elektrocas (rodina chars). */
export const CHAR_MANUFACTURERS = ["LCD", "LCD1", "ERP"];

/** Nastavenie tabule z jej aktualneho stavu. */
export function boardConfig(board: BoardDto): SetBoardRequest {
  return {
    line: board.line,
    address: board.address,
    manufacturer: board.manufacturer,
    name: board.name,
    profile: { ...board.profile },
    design: board.design,
    fault: board.fault,
    secondAddress: board.secondAddress,
  };
}

/** Profil novej tabule podla vyrobcu (zodpoveda BoardProfile.DefaultFor). */
export function defaultProfile(manufacturer: string): BoardProfileDto {
  const upper = manufacturer.toUpperCase();
  const size =
    upper === "ELEN16" || upper === "ELEN16KAM" || upper === "ELEKON"
      ? { width: 384, rows: 4, rowHeight: 16 }
      : CHAR_MANUFACTURERS.includes(upper)
        ? { width: 320, rows: 4, rowHeight: 16 }
        : { width: 192, rows: 2, rowHeight: 10 };
  return {
    ...size,
    fontFile: null,
    fontMap: null,
    alternationMs: 2000,
    blinkOnMs: 1000,
    blinkOffMs: 500,
    scrollSpeed: 60,
    movingFont: 0x52,
    autoSize: false,
    monochrome: false,
    swapRedGreen: false,
    header: null,
  };
}

/** Nastavenie linky z jej aktualneho stavu. */
export function lineConfig(line: LineDto): SetLineRequest {
  return {
    number: line.number,
    tableClass: line.tableClass,
    endpoint: line.endpoint,
    name: line.name,
    mode: line.mode,
    autoLearn: line.autoLearn,
    manufacturer: line.manufacturer,
    mirror: line.mirror,
  };
}
