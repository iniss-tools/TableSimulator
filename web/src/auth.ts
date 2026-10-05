// Pristupovy kluc zariadenia a hlavicky poziadaviek - bez zavislosti (pouziva ho aj renderer).

/** Hlavicka proti CSRF - server bez nej odmietne kazdu zmenu. */
export const CSRF_HEADER = { "X-Requested-With": "TableSimulator" };

const KEY_PARAM = "key";
const KEY_STORAGE = "tableSimulator.accessKey";

/**
 * Pristupovy kluc zariadenia z odkazu (?key=...): ulozi sa do relacie karty a z adresy zmizne, aby nebol vidno.
 * Karta s klucom pristupuje ako zariadenie (pozorovatel), aj ked ma prehliadac prihlasenie cookie.
 */
function initAccessKey(): string | null {
  try {
    const url = new URL(location.href);
    const fromUrl = url.searchParams.get(KEY_PARAM);
    if (fromUrl) {
      sessionStorage.setItem(KEY_STORAGE, fromUrl);
      url.searchParams.delete(KEY_PARAM);
      history.replaceState(null, "", url.toString());
      return fromUrl;
    }
    return sessionStorage.getItem(KEY_STORAGE);
  } catch {
    return null;
  }
}

let key: string | null = initAccessKey();

/** Pristupovy kluc tejto karty; null = prihlasenie cookie. */
export function accessKey(): string | null {
  return key;
}

/** Zabudne kluc karty (napr. neplatny kluc pred prihlasenim heslom). */
export function forgetAccessKey() {
  key = null;
  try {
    sessionStorage.removeItem(KEY_STORAGE);
  } catch {
    // ulozisko nedostupne
  }
}

/** Hlavicky poziadavky na API - ochrana CSRF a pristupovy kluc. */
export function authHeaders(): Record<string, string> {
  return key ? { ...CSRF_HEADER, Authorization: `Bearer ${key}` } : CSRF_HEADER;
}

/** Adresa WebSocketu - prehliadac k nemu hlavicku neposle, kluc ide v parametri. */
export function socketUrl(withLog: boolean): string {
  const proto = location.protocol === "https:" ? "wss" : "ws";
  const query = new URLSearchParams();
  if (withLog) query.set("log", "1");
  if (key) query.set("access_token", key);
  const text = query.toString();
  return `${proto}://${location.host}/ws${text ? `?${text}` : ""}`;
}

/** Odkaz pre zobrazovacie zariadenie s klucom (hash = stranka, napr. #/virtual/3). */
export function deviceLink(secret: string, hash: string): string {
  return `${location.origin}/?${KEY_PARAM}=${encodeURIComponent(secret)}${hash}`;
}
