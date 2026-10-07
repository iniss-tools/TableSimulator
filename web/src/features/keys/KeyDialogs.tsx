// Novy pristupovy kluc a jeho zobrazenie - tajna hodnota len raz, pre zariadenie aj odkaz na vybranu stranku.
import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { TriangleAlert } from "lucide-react";
import {
  createAccessKeyMutation,
  deviceLink,
  errorText,
  getHttpsStatusOptions,
  getVirtualOverviewOptions,
  ROLES,
  useSimulatorState,
  type AccessKeyCreatedDto,
  type AccessKeyKind,
  type Role,
} from "@/api";
import { CopyButton } from "@/components/common/copy-button";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t, type TextKey } from "@/i18n";

/** Text na skopirovanie: pole len na citanie (oznaci sa cele) a tlacidlo. */
function CopyField({ value, id }: { value: string; id?: string }) {
  return (
    <div className="flex gap-2">
      <Input id={id} readOnly value={value} className="font-mono" onFocus={(e) => e.target.select()} />
      <CopyButton text={value} label={t("common.copy")} />
    </div>
  );
}

export function NewKeyDialog({ onClose }: { onClose: (created: AccessKeyCreatedDto | null) => void }) {
  const [name, setName] = useState("");
  const [kind, setKind] = useState<AccessKeyKind>("device");
  const [role, setRole] = useState<Role>("operator");
  const create = useMutation({ ...createAccessKeyMutation(), onSuccess: onClose });
  // zariadenie smie vzdy len pozerat
  const effectiveRole = kind === "device" ? "viewer" : role;

  return (
    <FormDialog
      title={t("dialog.key.new")}
      onClose={() => onClose(null)}
      onSubmit={() => create.mutate({ body: { name, kind, role: effectiveRole } })}
      error={create.error && errorText(create.error)}
      busy={create.isPending}
      submitLabel={t("keys.create")}
    >
      <FieldGroup>
        <FormField label={t("keys.name")} hint={t("keys.name.hint")}>
          <Input value={name} onChange={(e) => setName(e.target.value)} />
        </FormField>
        <FormField label={t("keys.kind")} hint={t(`keys.kind.${kind}.hint` as TextKey)}>
          <OptionSelect<AccessKeyKind>
            value={kind}
            onValueChange={setKind}
            options={[
              { value: "device", label: t("keys.kind.device") },
              { value: "api", label: t("keys.kind.api") },
            ]}
            className="w-full"
          />
        </FormField>
        <FormField label={t("users.role")} hint={kind === "device" ? t("keys.role.device") : t(`users.role.${role}.hint` as TextKey)}>
          <OptionSelect<Role>
            value={effectiveRole}
            onValueChange={setRole}
            disabled={kind === "device"}
            options={ROLES.map((r) => ({ value: r, label: t(`role.${r}`) }))}
            className="w-full"
          />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}

export function CreatedKeyDialog({ created, onClose }: { created: AccessKeyCreatedDto; onClose: () => void }) {
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

  const pages = [
    ...virtual.map((b) => ({ value: `#/virtual/${b.id}`, label: t("keys.page.virtual", { id: b.id, name: b.name }) })),
    { value: "#/wall", label: t("keys.page.wall") },
    ...live.boards.map((b) => ({ value: `#/view/${b.id}`, label: t("keys.page.board", { name: b.name ? `${b.name} (${b.id})` : b.id }) })),
  ];

  return (
    <FormDialog title={t("dialog.key.created", { name: created.key.name })} onClose={onClose} onSubmit={onClose} submitLabel={t("common.close")} noCancel size="lg">
      <Alert className="border-amber-500/40 text-amber-800 dark:text-amber-300">
        <TriangleAlert />
        <AlertDescription className="text-current">{t("keys.secretOnce")}</AlertDescription>
      </Alert>
      <FieldGroup>
        <FormField label={t("keys.secret")}>
          <CopyField value={created.secret} />
        </FormField>
        {device ? (
          <>
            <FormField label={t("keys.page")} hint={t("keys.page.hint")}>
              <OptionSelect value={page} onValueChange={setPage} options={pages} className="w-full" />
            </FormField>
            <FormField label={t("keys.link")}>
              <CopyField value={deviceLink(created.secret, page, httpsPort)} />
            </FormField>
          </>
        ) : (
          <p className="text-sm text-muted-foreground">{t("keys.api.hint")}</p>
        )}
      </FieldGroup>
    </FormDialog>
  );
}
