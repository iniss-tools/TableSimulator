// Pristupove kluce: zariadenia (odkaz s klucom, len pozeranie) a programy (GVDEditor, skripty).
import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  createAccessKeyMutation,
  deleteAccessKey,
  deviceLink,
  errorText,
  getAccessKeysOptions,
  getAccessKeysQueryKey,
  getHttpsStatusOptions,
  getVirtualOverviewOptions,
  queryClient,
  ROLES,
  useSimulatorState,
  type AccessKeyCreatedDto as AccessKeyCreated,
  type AccessKeyKind,
  type Role,
} from "../api";
import { t, type TextKey } from "../i18n";
import { Dialog, Field } from "./Dialog";
import { formatDateTime } from "./UsersPage";

const refreshKeys = () => queryClient.invalidateQueries({ queryKey: getAccessKeysQueryKey() });

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
  const create = useMutation({ ...createAccessKeyMutation(), onSuccess: onClose });
  const save = () => create.mutate({ body: { name, kind, role: kind === "device" ? "viewer" : role } });

  return (
    <Dialog
      title={t("dialog.key.new")}
      onClose={() => onClose(null)}
      onSubmit={save}
      error={create.error && errorText(create.error)}
      busy={create.isPending}
      submitLabel={t("keys.create")}
    >
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
  const live = useSimulatorState();
  const device = created.key.kind === "device";
  const virtual = useQuery({ ...getVirtualOverviewOptions(), enabled: device }).data?.boards ?? [];
  const https = useQuery({ ...getHttpsStatusOptions(), enabled: device }).data;
  const httpsPort = https?.enabled ? https.port : null;
  const [page, setPage] = useState("#/wall");

  // predvolene prva virtualna tabula
  const firstVirtual = virtual[0]?.id;
  useEffect(() => {
    if (firstVirtual !== undefined) setPage(`#/virtual/${firstVirtual}`);
  }, [firstVirtual]);

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
  const query = useQuery(getAccessKeysOptions());
  const keys = query.data ?? [];
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const error = deleteError ?? (query.error && errorText(query.error));
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState<AccessKeyCreated | null>(null);

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
                      onClick={() => confirm(t("keys.delete.confirm", { name: k.name })) && deleteAccessKey({ path: { id: k.id } }).then(() => { setDeleteError(null); return refreshKeys(); }).catch((e) => setDeleteError(errorText(e)))}
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
              refreshKeys();
            }
          }}
        />
      )}
      {created && <CreatedDialog created={created} onClose={() => setCreated(null)} />}
    </>
  );
}
