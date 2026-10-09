// Spolocne pre testy: spravy ELEN ako ich posiela INISS (TCP na port linky), linky cez API, chyby stranky, prihlasenie.
import { connect } from "node:net";
import { expect, type Page } from "@playwright/test";

/** Kontrolny sucet ELEN (CRC-16 0xA001, nizsi bajt). */
function crc(data: number[]): number {
  let c = 0;
  for (const b of data) {
    c ^= b;
    for (let i = 0; i < 8; i++) c = c & 1 ? (c >> 1) ^ 0xa001 : c >> 1;
  }
  return c & 0xff;
}

/** Sprava ELEN: SOH, adresa, prikaz, parametre, EOT a sucet v dvoch bajtoch s hornym bitom. */
export function elenFrame(address: number, command: number, params: number[] = []): number[] {
  const body = [0x01, 0x80 | address, 0x80 | command, ...params, 0x04];
  const sum = crc(body);
  return [...body, 0x80 | (sum >> 4), 0x80 | (sum & 0x0f)];
}

/** Riadok textu tabule ELEN (prikaz 1) v pisme 0x52 od zaciatku riadku; text len ASCII. */
export function elenRow(address: number, row: number, text: string): number[] {
  const content = [0x10, 0x80, 0x1b, 0x52, ...Array.from(text, (ch) => ch.charCodeAt(0))];
  return elenFrame(address, 1, [0x80 | row, ...content]);
}

/** Kontrolny sucet sprav vzdialeneho INISSu: zvysok delenia polynomom 0x1021 bez nasobenia x^16. */
function remoteCrc(data: number[]): number {
  let c = 0;
  for (const b of data) {
    const high = c >> 8;
    let t = high << 8;
    for (let i = 0; i < 8; i++) t = t & 0x8000 ? ((t << 1) ^ 0x1021) & 0xffff : (t << 1) & 0xffff;
    c = (((c << 8) | b) & 0xffff) ^ t;
  }
  return c;
}

const int32 = (value: number) => [value & 0xff, (value >> 8) & 0xff, (value >> 16) & 0xff, (value >>> 24) & 0xff];

/**
 * Sprava riadiaceho INISSu na linke vzdialeneho INISSu (trieda 128) so zapisom na tabulu: kod vyrobcu, adresa a sprava
 * tabule - jej kontrolny sucet riadiaci INISS nevyplni (posledne dva bajty ELEN, posledny Elektrocas).
 */
export function remoteTableMessage(seq: number, manufacturer: number, address: number, frame: number[], checksumBytes: number): number[] {
  const command = [...frame.slice(0, frame.length - checksumBytes), ...new Array<number>(checksumBytes).fill(0)];
  const chunk = (tag: string, data: number[]) => [...Array.from(tag, (ch) => ch.charCodeAt(0)).reverse(), ...int32(data.length), ...data];
  const body = [...chunk("TCLS", int32(manufacturer)), ...chunk("TADR", int32(address)), ...chunk("TCMD", command)];
  const message = [0x53, ...int32(body.length + 11), ...int32(seq), ...body];
  const sum = remoteCrc(message);
  return [...message, sum & 0xff, sum >> 8];
}

/** Posle spravy na linku simulatora ako INISS a vrati odpovede tabule (bajt po kazdej sprave). */
export async function sendToLine(port: number, frames: number[][]): Promise<number[]> {
  const socket = connect(port, "127.0.0.1");
  await new Promise<void>((resolve, reject) => socket.once("connect", resolve).once("error", reject));
  const replies: number[] = [];
  socket.on("data", (data: Buffer) => replies.push(...data));
  for (const frame of frames) {
    const before = replies.length;
    socket.write(Buffer.from(frame));
    // tabula odpovie (ACK 0x06) - dalsia sprava az po odpovedi, ako INISS
    for (let i = 0; i < 50 && replies.length === before; i++) await new Promise((r) => setTimeout(r, 20));
  }
  socket.end();
  return replies;
}

/** Zmeny cez API ako web (ochrana proti CSRF). */
export const API_HEADERS = { "X-Requested-With": "TableSimulator" };

/** Linka ELEN na TCP porte 47000 + cislo (bez okna - test sa venuje inemu). */
export async function createLine(page: Page, number: number, name: string) {
  const response = await page.request.put(`/api/lines/${number}`, {
    headers: API_HEADERS,
    data: { number, tableClass: 4, endpoint: `TCP://0.0.0.0:${47000 + number}`, name, mode: "display", autoLearn: true, manufacturer: "ELEN", mirror: null },
  });
  expect(response.ok(), await response.text()).toBe(true);
}

/** POST na API ako web; vrati odpoved ako JSON. */
export async function apiPost<T>(page: Page, url: string, data: unknown): Promise<T> {
  const response = await page.request.post(url, { headers: API_HEADERS, data });
  expect(response.ok(), await response.text()).toBe(true);
  return (await response.json()) as T;
}

/** Odstrani linku aj s tabulami. */
export async function deleteLine(page: Page, number: number) {
  await page.request.delete(`/api/lines/${number}`, { headers: API_HEADERS });
}

/** Zbiera chyby JavaScriptu a konzoly (okrem padov WebSocketu pri starte a zastaveni servera). */
export function collectErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  page.on("console", (m) => {
    if (m.type() === "error" && !m.text().includes("WebSocket")) errors.push(m.text());
  });
  return errors;
}

/** Vybrana polozka bocneho menu (odkaz s textom). */
export async function openPage(page: Page, label: string) {
  await page.getByRole("link", { name: label, exact: true }).first().click();
  await expect(page.locator("header h1")).toHaveText(label);
}
