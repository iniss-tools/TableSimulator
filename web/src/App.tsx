// Hlavny ramec: navigacia, jazyk, tema a zobrazenie na celu obrazovku (#/view/<id>, #/wall).
import { useEffect, useState } from "react";
import { setLanguage, t, useLanguage, type Language } from "./i18n";
import { api, PERMISSIONS } from "./api";
import { can, loadMe, openSocket, useLive } from "./store";
import { AuthPage } from "./components/AuthPage";
import { BoardsPage } from "./components/BoardsPage";
import { BoardView } from "./components/BoardView";
import { LinesPage } from "./components/LinesPage";
import { LogPage } from "./components/LogPage";
import { RecordingsPage } from "./components/RecordingsPage";
import { SettingsPage } from "./components/SettingsPage";
import { VirtualKiosk } from "./components/VirtualKiosk";
import { VirtualPage } from "./components/VirtualPage";

function useHash(): string {
  const [hash, setHash] = useState(location.hash);
  useEffect(() => {
    const onChange = () => setHash(location.hash);
    addEventListener("hashchange", onChange);
    // adresa sa mohla zmenit skor, nez sa posluchac zaregistroval
    onChange();
    return () => removeEventListener("hashchange", onChange);
  }, []);
  return hash;
}

type Theme = "light" | "dark" | "auto";

function useTheme(): [Theme, (t: Theme) => void] {
  const [theme, setTheme] = useState<Theme>(() => {
    try {
      return (localStorage.getItem("theme") as Theme) || "auto";
    } catch {
      return "auto";
    }
  });
  useEffect(() => {
    if (theme === "auto") document.documentElement.removeAttribute("data-theme");
    else document.documentElement.setAttribute("data-theme", theme);
    try {
      localStorage.setItem("theme", theme);
    } catch {
      // ulozisko nedostupne
    }
  }, [theme]);
  return [theme, setTheme];
}

/** Tabula alebo vsetky tabule bez ovladania - pre televizor, tablet, druhu obrazovku. */
function Kiosk({ boardId }: { boardId: string | null }) {
  const live = useLive();
  const boards = boardId ? live.boards.filter((b) => b.id === boardId) : live.boards;
  const fullscreen = () => {
    if (document.fullscreenElement) document.exitFullscreen();
    else document.documentElement.requestFullscreen?.().catch(() => undefined);
  };
  return (
    <div className={boardId ? "kiosk kiosk-single" : "kiosk"} onDoubleClick={fullscreen}>
      {boards.map((b) => (
        <BoardView key={b.id} board={b} fit={boardId ? "contain" : "width"} />
      ))}
      {!live.connected && <div className="kiosk-offline">{t("server.offline")}</div>}
    </div>
  );
}

export function App() {
  const hash = useHash();
  const language = useLanguage();
  const [theme, setTheme] = useTheme();
  const live = useLive();

  const authenticated = live.me?.authenticated ?? false;
  useEffect(() => {
    loadMe().catch(() => undefined);
  }, []);
  useEffect(() => (authenticated ? openSocket() : undefined), [authenticated]);
  useEffect(() => {
    document.title = t("app.title");
  }, [language]);

  // kym sa nezisti pouzivatel, nic; neprihlaseny dostane prihlasenie (aj na adrese kiosku - po prihlaseni tam ostane)
  if (!live.me) return null;
  if (live.me.loginRequired && !live.me.authenticated) return <AuthPage setup={live.me.setupRequired} />;

  const view = hash.match(/^#\/view\/(.+)$/);
  if (view) return <Kiosk boardId={decodeURIComponent(view[1])} />;
  if (hash === "#/wall") return <Kiosk boardId={null} />;
  const virtual = hash.match(/^#\/virtual\/(\d+)$/);
  if (virtual) return <VirtualKiosk id={Number(virtual[1])} />;

  const page = hash.replace(/^#\//, "") || "boards";
  const nav: [string, string, string][] = [
    ["boards", t("nav.boards"), PERMISSIONS.view],
    ["lines", t("nav.lines"), PERMISSIONS.view],
    ["log", t("nav.log"), PERMISSIONS.log],
    ["recordings", t("nav.recordings"), PERMISSIONS.view],
    ["virtual", t("nav.virtual"), PERMISSIONS.view],
    ["settings", t("nav.settings"), PERMISSIONS.settings],
  ];
  const allowed = nav.filter(([, , permission]) => can(live.me, permission));

  return (
    <div className="app">
      <header className="top">
        <span className="brand">
          <img src="/favicon.svg" alt="" width={22} height={22} />
          {t("app.title")}
        </span>
        <nav>
          {allowed.map(([id, label]) => (
            <a key={id} href={`#/${id}`} className={page === id ? "active" : ""}>
              {label}
            </a>
          ))}
        </nav>
        <span className="spacer" />
        {live.me.loginRequired && (
          <span className="user">
            <span title={live.me.userName ?? ""}>
              {live.me.displayName || live.me.userName} · {t(`role.${live.me.role ?? "viewer"}`)}
            </span>
            <button className="secondary" onClick={() => api.logout().finally(() => loadMe())}>
              {t("auth.logout")}
            </button>
          </span>
        )}
        <select aria-label={t("lang.label")} value={language} onChange={(e) => setLanguage(e.target.value as Language)}>
          <option value="sk">SK</option>
          <option value="cs">CZ</option>
        </select>
        <button className="icon-button" title={t("theme.toggle")} onClick={() => setTheme(theme === "dark" ? "light" : theme === "light" ? "auto" : "dark")}>
          {theme === "dark" ? "☾" : theme === "light" ? "☀" : "◐"}
        </button>
      </header>
      {!live.connected && live.loaded && <div className="banner">{t("server.offline")}</div>}
      <main>
        {page === "lines" ? (
          <LinesPage />
        ) : page === "log" ? (
          <LogPage />
        ) : page === "recordings" ? (
          <RecordingsPage />
        ) : page === "settings" ? (
          <SettingsPage />
        ) : page === "virtual" ? (
          <VirtualPage />
        ) : (
          <BoardsPage />
        )}
      </main>
    </div>
  );
}
