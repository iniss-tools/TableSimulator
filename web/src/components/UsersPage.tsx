// Sprava pouzivatelov a audit (kto co urobil) - len pre spravcu.
import { useCallback, useEffect, useState } from "react";
import { api, ApiError, PERMISSIONS, ROLES, type AuditEntry, type Role, type UserInfo } from "../api";
import { getLanguage, hasText, t, useLanguage, type TextKey } from "../i18n";
import { useCan, useLive } from "../store";
import { Dialog, Field } from "./Dialog";
import { KeysTab } from "./KeysTab";

/** Datum a cas v jazyku obsluhy (miestny cas prehliadaca). */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "";
  return new Date(value).toLocaleString(getLanguage() === "cs" ? "cs-CZ" : "sk-SK", { dateStyle: "short", timeStyle: "medium" });
}

const errorText = (e: unknown) => (e instanceof ApiError ? e.message : String(e));

/** Akcie auditu - mena prikazov servera. */
const ACTIONS = [
  "Login",
  "Logout",
  "Setup",
  "ChangePassword",
  "CreateUser",
  "UpdateUser",
  "SetUserPassword",
  "UnlockUser",
  "DeleteUser",
  "CreateAccessKey",
  "DeleteAccessKey",
  "UpdateSettings",
  "SetLine",
  "DeleteLine",
  "SetBoard",
  "DeleteBoard",
  "ClearBoard",
  "SetBoardFault",
  "ClearLog",
  "ImportStation",
  "ImportMonitorProject",
  "StartRecording",
  "StopRecording",
  "UploadRecording",
  "DeleteRecording",
  "PlayRecording",
  "StopReplay",
  "SetVirtualBoard",
  "DeleteVirtualBoard",
  "ImportInissView",
  "SetVirtualLayout",
  "DeleteVirtualLayout",
  "ResetAdmin",
];

function actionText(action: string): string {
  const key = `audit.action.${action}`;
  return hasText(key) ? t(key) : action;
}

function UserDialog({ initial, isNew, onClose }: { initial: UserInfo; isNew: boolean; onClose: () => void }) {
  const [user, setUser] = useState(initial);
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const save = async () => {
    if (isNew && password !== again) {
      setError(t("auth.passwordMismatch"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      if (isNew) await api.createUser({ userName: user.userName, displayName: user.displayName, role: user.role, password });
      else await api.updateUser(user.userName, { displayName: user.displayName, role: user.role, disabled: user.disabled });
      onClose();
    } catch (e) {
      setError(errorText(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog title={isNew ? t("dialog.user.new") : t("dialog.user.edit", { name: user.userName })} onClose={onClose} onSubmit={save} error={error} busy={busy}>
      <div className="form-grid">
        <Field label={t("auth.userName")} hint={isNew ? t("users.userName.hint") : undefined}>
          <input value={user.userName} disabled={!isNew} autoComplete="off" onChange={(e) => setUser({ ...user, userName: e.target.value })} />
        </Field>
        <Field label={t("auth.displayName")}>
          <input value={user.displayName} onChange={(e) => setUser({ ...user, displayName: e.target.value })} />
        </Field>
        <Field label={t("users.role")} hint={t(`users.role.${user.role}.hint` as TextKey)} wide>
          <select value={user.role} onChange={(e) => setUser({ ...user, role: e.target.value as Role })}>
            {ROLES.map((r) => (
              <option key={r} value={r}>
                {t(`role.${r}` as TextKey)}
              </option>
            ))}
          </select>
        </Field>
        {isNew ? (
          <>
            <Field label={t("auth.password")}>
              <input type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
            </Field>
            <Field label={t("auth.passwordAgain")}>
              <input type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
            </Field>
          </>
        ) : (
          <label className="check field-wide">
            <input type="checkbox" checked={user.disabled} onChange={(e) => setUser({ ...user, disabled: e.target.checked })} />
            {t("users.disabled.field")}
          </label>
        )}
      </div>
    </Dialog>
  );
}

/** Nove heslo pouzivatela (spravca) alebo vlastne heslo (s overenim sucasneho). */
export function PasswordDialog({ userName, onClose }: { userName: string | null; onClose: (changed: boolean) => void }) {
  const own = userName === null;
  const [current, setCurrent] = useState("");
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const save = async () => {
    if (password !== again) {
      setError(t("auth.passwordMismatch"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      if (own) await api.changePassword(current, password);
      else await api.setUserPassword(userName, password);
      onClose(true);
    } catch (e) {
      setError(errorText(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog
      title={own ? t("dialog.password.own") : t("dialog.password.user", { name: userName })}
      onClose={() => onClose(false)}
      onSubmit={save}
      error={error}
      busy={busy}
    >
      <div className="form-grid">
        {own && (
          <Field label={t("users.currentPassword")} wide>
            <input type="password" autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} />
          </Field>
        )}
        <Field label={t("users.newPassword")} hint={own ? undefined : t("users.newPassword.hint")}>
          <input type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </Field>
        <Field label={t("auth.passwordAgain")}>
          <input type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
        </Field>
      </div>
    </Dialog>
  );
}

function UsersTab() {
  const live = useLive();
  const [users, setUsers] = useState<UserInfo[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState<{ user: UserInfo; isNew: boolean } | null>(null);
  const [password, setPassword] = useState<string | null>(null);

  const load = useCallback(() => {
    api
      .users()
      .then((u) => {
        setUsers(u);
        setError(null);
      })
      .catch((e) => setError(errorText(e)));
  }, []);
  useEffect(load, [load]);

  const act = (action: Promise<unknown>) =>
    action
      .then(load)
      .catch((e) => setError(errorText(e)));
  const self = (u: UserInfo) => live.me?.loginRequired === true && live.me.userName?.toLowerCase() === u.userName.toLowerCase();

  return (
    <>
      <div className="toolbar">
        <p className="hint">{t("users.hint")}</p>
        <span className="spacer" />
        <button
          onClick={() =>
            setEditing({ isNew: true, user: { userName: "", displayName: "", role: "viewer", disabled: false, lockedUntil: null, createdAt: "", lastLoginAt: null } })
          }
        >
          {t("users.add")}
        </button>
      </div>
      {!live.me?.loginRequired && <p className="banner-warn">{t("users.loginOff")}</p>}
      {error && <p className="error">{error}</p>}
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{t("auth.userName")}</th>
              <th>{t("users.role")}</th>
              <th>{t("users.state")}</th>
              <th>{t("users.created")}</th>
              <th>{t("users.lastLogin")}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.userName}>
                <td>
                  <code>{u.userName}</code>
                  {u.displayName && u.displayName !== u.userName && <div className="muted small">{u.displayName}</div>}
                  {self(u) && <div className="muted small">{t("users.you")}</div>}
                </td>
                <td>{t(`role.${u.role}` as TextKey)}</td>
                <td>
                  {u.disabled ? (
                    <span className="status status-stopped">{t("users.disabled")}</span>
                  ) : u.lockedUntil ? (
                    <span className="status status-error">{t("users.locked", { time: formatDateTime(u.lockedUntil) })}</span>
                  ) : (
                    <span className="status status-connected">{t("users.active")}</span>
                  )}
                </td>
                <td className="nowrap">{formatDateTime(u.createdAt)}</td>
                <td className="nowrap">{u.lastLoginAt ? formatDateTime(u.lastLoginAt) : <span className="muted">{t("users.never")}</span>}</td>
                <td className="nowrap">
                  <button className="icon-button" title={t("common.edit")} onClick={() => setEditing({ user: u, isNew: false })}>
                    ✎
                  </button>
                  <button className="secondary" onClick={() => setPassword(u.userName)}>
                    {t("users.setPassword")}
                  </button>
                  {u.lockedUntil && (
                    <button className="secondary" onClick={() => act(api.unlockUser(u.userName))}>
                      {t("users.unlock")}
                    </button>
                  )}
                  {!self(u) && (
                    <button
                      className="icon-button"
                      title={t("common.delete")}
                      onClick={() => confirm(t("users.delete.confirm", { name: u.userName })) && act(api.deleteUser(u.userName))}
                    >
                      ✕
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {editing && (
        <UserDialog
          initial={editing.user}
          isNew={editing.isNew}
          onClose={() => {
            setEditing(null);
            load();
          }}
        />
      )}
      {password && (
        <PasswordDialog
          userName={password}
          onClose={() => {
            setPassword(null);
            load();
          }}
        />
      )}
    </>
  );
}

const PAGE = 100;

function AuditTab() {
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [user, setUser] = useState("");
  const [action, setAction] = useState("");
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(
    (before?: number) => {
      api
        .audit({ before, user: user.trim() || undefined, action: action || undefined, limit: PAGE })
        .then((page) => {
          setEntries((old) => (before ? [...old, ...page.entries] : page.entries));
          setHasMore(page.hasMore);
          setError(null);
        })
        .catch((e) => setError(errorText(e)));
    },
    [user, action],
  );
  useEffect(() => load(), [load]);

  return (
    <>
      <div className="toolbar">
        <p className="hint">{t("audit.hint")}</p>
        <span className="spacer" />
        <input className="audit-filter" placeholder={t("audit.filter.user")} value={user} onChange={(e) => setUser(e.target.value)} />
        <select value={action} onChange={(e) => setAction(e.target.value)}>
          <option value="">{t("audit.filter.allActions")}</option>
          {ACTIONS.map((a) => (
            <option key={a} value={a}>
              {actionText(a)}
            </option>
          ))}
        </select>
        <button className="secondary" onClick={() => load()}>
          {t("audit.refresh")}
        </button>
      </div>
      {error && <p className="error">{error}</p>}
      {entries.length === 0 ? (
        <p className="empty">{t("audit.empty")}</p>
      ) : (
        <div className="table-wrap">
          <table className="audit">
            <thead>
              <tr>
                <th>{t("audit.col.time")}</th>
                <th>{t("audit.col.user")}</th>
                <th>{t("audit.col.action")}</th>
                <th>{t("audit.col.target")}</th>
                <th>{t("audit.col.result")}</th>
                <th>{t("audit.col.address")}</th>
                <th>{t("audit.col.details")}</th>
              </tr>
            </thead>
            <tbody>
              {entries.map((e) => {
                const errorKey = e.error ? `error.${e.error}` : "";
                return (
                  <tr key={e.id} className={e.success ? undefined : "audit-failed"}>
                    <td className="nowrap">{formatDateTime(e.time)}</td>
                    <td>
                      {e.user === "-" ? (
                        <span className="muted">{t("audit.anonymous")}</span>
                      ) : e.user === "(console)" ? (
                        <span className="muted">{t("audit.console")}</span>
                      ) : (
                        <code>{e.user}</code>
                      )}
                    </td>
                    <td>{actionText(e.action)}</td>
                    <td>{e.target}</td>
                    <td>{e.success ? <span className="status status-connected">{t("audit.ok")}</span> : <span className="status status-error">{hasText(errorKey) ? t(errorKey) : e.error}</span>}</td>
                    <td className="muted small">{e.address}</td>
                    <td>
                      {e.details && (
                        <details>
                          <summary>{t("audit.show")}</summary>
                          <code className="audit-details">{e.details}</code>
                        </details>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      {hasMore && (
        <button className="secondary" onClick={() => load(entries[entries.length - 1]?.id)}>
          {t("audit.more")}
        </button>
      )}
    </>
  );
}

export function UsersPage({ tab }: { tab: "users" | "audit" | "keys" }) {
  useLanguage();
  const can = useCan();
  const showUsers = can(PERMISSIONS.users);
  const showAudit = can(PERMISSIONS.audit);
  const active = tab === "audit" && showAudit ? "audit" : showUsers ? (tab === "keys" ? "keys" : "users") : "audit";
  return (
    <section>
      <div className="toolbar">
        <h2>{t("users.title")}</h2>
        <nav className="tabs">
          {showUsers && (
            <a href="#/users" className={active === "users" ? "active" : ""}>
              {t("users.tab.users")}
            </a>
          )}
          {showUsers && (
            <a href="#/keys" className={active === "keys" ? "active" : ""}>
              {t("users.tab.keys")}
            </a>
          )}
          {showAudit && (
            <a href="#/audit" className={active === "audit" ? "active" : ""}>
              {t("users.tab.audit")}
            </a>
          )}
        </nav>
      </div>
      {active === "users" ? <UsersTab /> : active === "keys" ? <KeysTab /> : <AuditTab />}
    </section>
  );
}
