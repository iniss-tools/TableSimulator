// Pristupove kluce: zariadenia (odkaz s klucom, len pozeranie) a programy (GVDEditor, skripty).
import { useCallback, useEffect, useState } from "react";
import { api, ApiError, deviceLink, ROLES, type AccessKeyCreated, type AccessKeyInfo, type AccessKeyKind, type Role, type VirtualBoard } from "../api";
import { t, type TextKey } from "../i18n";
import { useLive } from "../store";
import { Dialog, Field } from "./Dialog";
import { formatDateTime } from "./UsersPage";

const errorText = (e: unknown) => (e instanceof ApiError ? e.message : String(e));

function CopyField({ value }: { value: string }) {
  const [done, setDone] = useState(false);
  return (
    <span className="copy-field">
      <input readOnly value={value} onFocus={(e) => e.target.select()} />
      <button
        type="button"
        className="secondary"
        onClick={() =>
          navigator.clipboard?.writeText(value).then(() => {
            setDone(true);
            setTimeout(() => setDone(false), 1500);
          })
        }
      >
        {done ? t("common.copied") : t("common.copy")}
      </button>
    </span>
  );
}

function NewKeyDialog({ onClose }: { onClose: (created: AccessKeyCreated | null) => void }) {
  const [name, setName] = useState("");
  const [kind, setKind] = useState<AccessKeyKind>("device");
  const [role, setRole] = useState<Role>("operator");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const save = async () => {
    setBusy(true);
    setError(null);
    try {
      onClose(await api.createKey(name, kind, kind === "device" ? "viewer" : role));
    } catch (e) {
      setError(errorText(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog title={t("dialog.key.new")} onClose={() => onClose(null)} onSubmit={save} error={error} busy={busy} submitLabel={t("keys.create")}>
      <div className="form-grid">
        <Field label={t("keys.name")} hint={t("keys.name.hint")} wide>
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </Field>
        <Field label={t("keys.kind")} hint={t(`keys.kind.${kind}.hint` as TextKey)} wide>
          <select value={kind} onChange={(e) => setKind(e.target.value as AccessKeyKind)}>
            <option value="device">{t("keys.kind.device")}</option>
            <option value="api">{t("keys.kind.api")}</option>
          </select>
        </Field>
        <Field label={t("users.role")} hint={kind === "device" ? t("keys.role.device") : t(`users.role.${role}.hint` as TextKey)} wide>
          <select value={kind === "device" ? "viewer" : role} disabled={kind === "device"} onChange={(e) => setRole(e.target.value as Role)}>
            {ROLES.map((r) => (
              <option key={r} value={r}>
                {t(`role.${r}` as TextKey)}
              </option>
            ))}
          </select>
        </Field>
      </div>
    </Dialog>
  );
}

/** Novy kluc: tajna hodnota (len teraz) a pre zariadenie odkaz na vybranu stranku. */
function CreatedDialog({ created, onClose }: { created: AccessKeyCreated; onClose: () => void }) {
  const live = useLive();
  const [virtual, setVirtual] = useState<VirtualBoard[]>([]);
  const [page, setPage] = useState("#/wall");
  const [httpsPort, setHttpsPort] = useState<number | null>(null);
  const device = created.key.kind === "device";

  useEffect(() => {
    if (device)
      api
        .virtual()
        .then((v) => {
          setVirtual(v.boards);
          if (v.boards.length > 0) setPage(`#/virtual/${v.boards[0].id}`);
        })
        .catch(() => undefined);
    if (device)
      api
        .httpsStatus()
        .then((s) => setHttpsPort(s.enabled ? s.port : null))
        .catch(() => undefined);
  }, [device]);

  return (
    <Dialog title={t("dialog.key.created", { name: created.key.name })} onClose={onClose} onSubmit={onClose} submitLabel={t("common.close")} noCancel>
      <p className="banner-warn">{t("keys.secretOnce")}</p>
      <Field label={t("keys.secret")} wide>
        <CopyField value={created.secret} />
      </Field>
      {device ? (
        <>
          <Field label={t("keys.page")} hint={t("keys.page.hint")} wide>
            <select value={page} onChange={(e) => setPage(e.target.value)}>
              {virtual.map((b) => (
                <option key={b.id} value={`#/virtual/${b.id}`}>
                  {t("keys.page.virtual", { id: b.id, name: b.name })}
                </option>
              ))}
              <option value="#/wall">{t("keys.page.wall")}</option>
              {live.boards.map((b) => (
                <option key={b.id} value={`#/view/${b.id}`}>
                  {t("keys.page.board", { name: b.name ? `${b.name} (${b.id})` : b.id })}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t("keys.link")} wide>
            <CopyField value={deviceLink(created.secret, page, httpsPort)} />
          </Field>
        </>
      ) : (
        <p className="hint">{t("keys.api.hint")}</p>
      )}
    </Dialog>
  );
}

export function KeysTab() {
  const [keys, setKeys] = useState<AccessKeyInfo[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState<AccessKeyCreated | null>(null);

  const load = useCallback(() => {
    api
      .keys()
      .then((k) => {
        setKeys(k);
        setError(null);
      })
      .catch((e) => setError(errorText(e)));
  }, []);
  useEffect(load, [load]);

  return (
    <>
      <div className="toolbar">
        <p className="hint">{t("keys.hint")}</p>
        <span className="spacer" />
        <button onClick={() => setCreating(true)}>{t("keys.add")}</button>
      </div>
      {error && <p className="error">{error}</p>}
      {keys.length === 0 ? (
        <p className="empty">{t("keys.empty")}</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{t("keys.name")}</th>
                <th>{t("keys.kind")}</th>
                <th>{t("users.role")}</th>
                <th>{t("keys.prefix")}</th>
                <th>{t("keys.created")}</th>
                <th>{t("keys.lastUsed")}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k.id}>
                  <td>{k.name}</td>
                  <td>{t(`keys.kind.${k.kind}` as TextKey)}</td>
                  <td>{t(`role.${k.role}` as TextKey)}</td>
                  <td>
                    <code>{k.prefix}…</code>
                  </td>
                  <td className="nowrap">
                    {formatDateTime(k.createdAt)}
                    <div className="muted small">{k.createdBy}</div>
                  </td>
                  <td className="nowrap">{k.lastUsedAt ? formatDateTime(k.lastUsedAt) : <span className="muted">{t("users.never")}</span>}</td>
                  <td>
                    <button
                      className="icon-button"
                      title={t("common.delete")}
                      onClick={() => confirm(t("keys.delete.confirm", { name: k.name })) && api.deleteKey(k.id).then(load).catch((e) => setError(errorText(e)))}
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {creating && (
        <NewKeyDialog
          onClose={(result) => {
            setCreating(false);
            if (result) {
              setCreated(result);
              load();
            }
          }}
        />
      )}
      {created && <CreatedDialog created={created} onClose={() => setCreated(null)} />}
    </>
  );
}
