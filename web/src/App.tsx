// Hlavny ramec: navigacia, jazyk, tema a zobrazenie na celu obrazovku (#/view/<id>, #/wall).
import { useEffect, useRef, useState } from "react";
import { setLanguage, t, useLanguage, type Language, type TextKey } from "./i18n";
import { can, forgetAccessKey, logout, openSocket, PERMISSIONS, queryClient, useConnection, useMe, useSimulatorState, type MeDto, type Permission } from "./api";
import { AuthPage } from "./components/AuthPage";
import { BoardsPage } from "./components/BoardsPage";
import { BoardView } from "./components/BoardView";
import { LinesPage } from "./components/LinesPage";
import { LogPage } from "./components/LogPage";
import { RecordingsPage } from "./components/RecordingsPage";
import { SettingsPage } from "./components/SettingsPage";
import { PasswordDialog, UsersPage } from "./components/UsersPage";
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
  const live = useSimulatorState();
  const { connected } = useConnection();
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
      {!connected && <div className="kiosk-offline">{t("server.offline")}</div>}
    </div>
  );
}

/** Odhlasenie - cache patri predchadzajucemu pouzivatelovi, zahodi sa a zisti sa znova, kto je prihlaseny. */
function signOut() {
  logout().finally(() => queryClient.resetQueries());
}

export function App() {
  const hash = useHash();
  const language = useLanguage();
  // tema plati aj pre kiosk a prihlasenie
  const theme = useTheme();
  const me = useMe();

  const authenticated = me?.authenticated ?? false;
  useEffect(() => (authenticated ? openSocket() : undefined), [authenticated]);
  useEffect(() => {
    document.title = t("app.title");
  }, [language]);

  // kym sa nezisti pouzivatel, nic; neprihlaseny dostane prihlasenie (aj na adrese kiosku - po prihlaseni tam ostane)
  if (!me) return null;
  if (me.loginRequired && !me.authenticated) return <AuthPage setup={me.setupRequired} />;

  const view = hash.match(/^#\/view\/(.+)$/);
  if (view) return <Kiosk boardId={decodeURIComponent(view[1])} />;
  if (hash === "#/wall") return <Kiosk boardId={null} />;
  const virtual = hash.match(/^#\/virtual\/(\d+)$/);
  if (virtual) return <VirtualKiosk id={Number(virtual[1])} />;

  return <Shell me={me} hash={hash} theme={theme} />;
}

function Shell({ me, hash, theme: [theme, setTheme] }: { me: MeDto; hash: string; theme: [Theme, (t: Theme) => void] }) {
  const language = useLanguage();
  const live = useSimulatorState();
  const { connected } = useConnection();
  const [password, setPassword] = useState(false);
  const userMenu = useRef<HTMLDetailsElement>(null);
  const [passwordDone, setPasswordDone] = useState(false);

  const page = hash.replace(/^#\//, "") || "boards";
  const nav: [string, string, Permission][] = [
    ["boards", t("nav.boards"), PERMISSIONS.view],
    ["lines", t("nav.lines"), PERMISSIONS.view],
    ["log", t("nav.log"), PERMISSIONS.log],
    ["recordings", t("nav.recordings"), PERMISSIONS.view],
    ["virtual", t("nav.virtual"), PERMISSIONS.view],
    ["settings", t("nav.settings"), PERMISSIONS.settings],
  ];
  const allowed = nav.filter(([, , permission]) => can(me, permission));
  // pouzivatelia a audit su jedna stranka so zalozkami
  if (can(me, PERMISSIONS.users) || can(me, PERMISSIONS.audit)) allowed.push(["users", t("nav.users"), PERMISSIONS.users]);

  return (
    <div className="app">
      <header className="top">
        <span className="brand">
          <img src="/favicon.svg" alt="" width={22} height={22} />
          {t("app.title")}
        </span>
        <nav>
          {allowed.map(([id, label]) => (
            <a key={id} href={`#/${id}`} className={page === id || (id === "users" && (page === "audit" || page === "keys")) || (id === "virtual" && page.startsWith("archive/")) ? "active" : ""}>
              {label}
            </a>
          ))}
        </nav>
        <span className="spacer" />
        {me.loginRequired && (
          <details className="user-menu" ref={userMenu}>
            <summary title={me.userName ?? ""}>
              {me.displayName || me.userName} · {t(`role.${me.role ?? "viewer"}` as TextKey)}
            </summary>
            <div className="user-menu-items">
              {me.accessKey ? (
                <>
                  <span className="muted small">{t("keys.thisTab")}</span>
                  <button
                    className="secondary"
                    onClick={() => {
                      forgetAccessKey();
                      location.reload();
                    }}
                  >
                    {t("keys.forget")}
                  </button>
                </>
              ) : (
                <>
                  {passwordDone && <span className="muted small">{t("users.passwordChanged")}</span>}
                  <button
                    className="secondary"
                    onClick={() => {
                      userMenu.current?.removeAttribute("open");
                      setPassword(true);
                    }}
                  >
                    {t("users.ownPassword")}
                  </button>
                  <button className="secondary" onClick={signOut}>
                    {t("auth.logout")}
                  </button>
                </>
              )}
            </div>
          </details>
        )}
        <select aria-label={t("lang.label")} value={language} onChange={(e) => setLanguage(e.target.value as Language)}>
          <option value="sk">SK</option>
          <option value="cs">CZ</option>
        </select>
        <button className="icon-button" title={t("theme.toggle")} onClick={() => setTheme(theme === "dark" ? "light" : theme === "light" ? "auto" : "dark")}>
          {theme === "dark" ? "☾" : theme === "light" ? "☀" : "◐"}
        </button>
      </header>
      {!connected && live.loaded && <div className="banner">{t("server.offline")}</div>}
      <main>
        {page === "lines" ? (
          <LinesPage />
        ) : page === "log" ? (
          <LogPage />
        ) : page === "recordings" ? (
          <RecordingsPage />
        ) : page === "settings" ? (
          <SettingsPage />
        ) : page === "virtual" || page.startsWith("archive/") ? (
          <VirtualPage tab={page === "archive/reports" ? "reports" : page === "archive/tables" ? "tables" : "boards"} />
        ) : page === "users" || page === "audit" || page === "keys" ? (
          <UsersPage tab={page} />
        ) : (
          <BoardsPage />
        )}
      </main>
      {password && (
        <PasswordDialog
          userName={null}
          onClose={(changed) => {
            setPassword(false);
            setPasswordDone(changed);
          }}
        />
      )}
    </div>
  );
}
