// Spravy INISSu pre ukazkovu stanicu: poloha kurzora ELEN (spravu sklada e2e/helpers), Elektrocas (LCD1, FERS), APEL
// a ELSVO.
// Text vo Windows-1250 ako ho posiela INISS.

const CP1250: Record<string, number> = {
  Š: 0x8a,
  Ť: 0x8d,
  Ž: 0x8e,
  š: 0x9a,
  ť: 0x9d,
  ž: 0x9e,
  Ľ: 0xbc,
  ľ: 0xbe,
  Á: 0xc1,
  Ä: 0xc4,
  Č: 0xc8,
  É: 0xc9,
  Í: 0xcd,
  Ď: 0xcf,
  Ň: 0xd2,
  Ó: 0xd3,
  Ô: 0xd4,
  Ú: 0xda,
  Ý: 0xdd,
  á: 0xe1,
  ä: 0xe4,
  č: 0xe8,
  é: 0xe9,
  í: 0xed,
  ď: 0xef,
  ň: 0xf2,
  ó: 0xf3,
  ô: 0xf4,
  ú: 0xfa,
  ý: 0xfd,
  ĺ: 0xe5,
  ŕ: 0xe0,
  "–": 0x96,
};

/** Text vo Windows-1250 (slovenske znaky, inak ASCII). */
export function cp1250(text: string): number[] {
  return Array.from(text, (ch) => CP1250[ch] ?? (ch.charCodeAt(0) < 0x80 ? ch.charCodeAt(0) : 0x3f));
}

/** Poloha kurzora ELEN na bod x riadku: kod 10-13 (vyssie bity) a nizsich 7 bitov s hornym bitom. */
export function elenAt(x: number): number[] {
  return [0x10 + (x >> 7), 0x80 | (x & 0x7f)];
}

/** Sprava Elektrocasu: adresa, dlzka, odosielatel (ComputerAddr 0), prikaz, parametre, sucet. */
export function elektrocasFrame(address: number, command: number, params: number[] = []): number[] {
  const body = [address, params.length + 5, 0, command, ...params];
  return [...body, body.reduce((a, b) => a + b, 0) & 0xff];
}

/** Riadok tabule LCD1 po usekoch textu s vlastnym cislom pisma (INISS ho posiela pri kazdom znaku). */
export function lcd1Parts(address: number, row: number, parts: [string, number][]): number[] {
  const chars = parts.flatMap(([text]) => cp1250(text));
  const fonts = parts.flatMap(([text, font]) => cp1250(text).map(() => font));
  return elektrocasFrame(address, 0x16, [row, 0, 1, 0, ...chars, ...fonts]);
}

/** Zapis riadku listovej tabule FERS (prikaz 10): kody listov po poziciach. */
export function fersRow(address: number, row: number, codes: string): number[] {
  return elektrocasFrame(address, 0x10, [row, 0, ...cp1250(codes)]);
}

/** Sprava APEL: DLE STX, adresa, zaznamy (prikaz, dlzka, udaje), XOR bez adresy, DLE ETX; DLE v tele zdvojene. */
export function apelFrame(address: number, records: [number, number[]][]): number[] {
  const body = [address, ...records.flatMap(([command, data]) => [command, data.length, ...data])];
  body.push(body.slice(1).reduce((a, b) => a ^ b, 0));
  return [0x10, 0x02, ...body.flatMap((b) => (b === 0x10 ? [0x10, 0x10] : [b])), 0x10, 0x03];
}

/** Prikaz jednotke ELSVO: STX, adresa, kod, ETX a doplnok suctu do nuly. */
export function elsvoFrame(address: number, code: number): number[] {
  const raw = [0x02, address, code, 0x03];
  return [...raw, -raw.reduce((a, b) => a + b, 0) & 0xff];
}
