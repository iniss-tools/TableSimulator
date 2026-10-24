// Ktory server odpoveda: hlavicka X-TableSimulator-Server s odtlackom pocitaca a priecinka nastaveni (ServerIdentity
// na serveri). Na tej istej adrese moze po case bezat iny server, napr. sluzba namiesto programu v okne - stranka sa
// vtedy nacita znova, aby obsluha nepracovala s udajmi ineho servera v domneni, ze su povodne.
const header = "X-TableSimulator-Server";
const switchedKey = "serverSwitched";

let known: string | null = null;

/** Zapamata si server z prvej odpovede; odpoved ineho servera stranku nacita znova. */
export function noteServer(response: Response) {
  const id = response.headers.get(header);
  if (!id || id === known) return;
  if (known === null) {
    known = id;
    return;
  }
  try {
    sessionStorage.setItem(switchedKey, "1");
  } catch {
    // ulozisko nedostupne - nacita sa bez oznamenia
  }
  location.reload();
}

/** Stranka sa nacitala znova kvoli inemu serveru - vrati to len raz. */
export function takeServerSwitch(): boolean {
  try {
    const switched = sessionStorage.getItem(switchedKey) === "1";
    sessionStorage.removeItem(switchedKey);
    return switched;
  } catch {
    return false;
  }
}
