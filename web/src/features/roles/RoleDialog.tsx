// Nova alebo upravena rola: nazov (vstavana ho nema) a opravnenia po skupinach. Opravnenie, ktore prihlaseny sam
// nema, prideli nemoze; prehlad ma kazda rola.
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { createRoleMutation, errorText, isBuiltInRole, PERMISSIONS, updateRoleMutation, useMe, type Permission, type RoleDto } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { CheckField, FormField } from "@/components/common/form-field";
import { FieldGroup, FieldLegend, FieldSet } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t, type TextKey } from "@/i18n";
import { PERMISSION_GROUPS, permissionLabel, refreshRoles, roleLabel } from "./roles";

export function RoleDialog({ initial, isNew, onClose }: { initial: RoleDto; isNew: boolean; onClose: () => void }) {
  const me = useMe();
  const [name, setName] = useState(initial.displayName);
  const [permissions, setPermissions] = useState<string[]>(initial.permissions);
  const saved = () => {
    void refreshRoles();
    onClose();
  };
  const create = useMutation({ ...createRoleMutation(), onSuccess: saved });
  const update = useMutation({ ...updateRoleMutation(), onSuccess: saved });
  const failure = create.error ?? update.error;
  const builtIn = isBuiltInRole(initial.name);

  const toggle = (permission: Permission, on: boolean) =>
    setPermissions((list) => (on ? [...list.filter((p) => p !== permission), permission] : list.filter((p) => p !== permission)));
  const save = () => {
    const body = { displayName: name.trim(), permissions };
    if (isNew) create.mutate({ body });
    else update.mutate({ path: { name: initial.name }, body });
  };

  return (
    <FormDialog
      title={isNew ? t("dialog.role.new") : t("dialog.role.edit", { name: roleLabel(initial.name, initial.displayName) })}
      onClose={onClose}
      onSubmit={save}
      error={failure && errorText(failure)}
      busy={create.isPending || update.isPending}
      size="lg"
    >
      <FieldGroup>
        <FormField label={t("roles.name")} hint={builtIn ? t("roles.name.builtIn") : undefined}>
          <Input value={builtIn ? roleLabel(initial.name) : name} disabled={builtIn} maxLength={40} onChange={(e) => setName(e.target.value)} />
        </FormField>
        {PERMISSION_GROUPS.map((group) => (
          <FieldSet key={group.label}>
            <FieldLegend variant="label">{t(group.label)}</FieldLegend>
            <div className="grid gap-3 sm:grid-cols-2">
              {group.permissions.map((permission) => (
                <CheckField
                  key={permission}
                  label={permissionLabel(permission)}
                  hint={t(`permission.${permission}.hint` as TextKey)}
                  checked={permission === PERMISSIONS.view || permissions.includes(permission)}
                  // prehlad potrebuje kazda rola; cudzie opravnenie sa neda pridelit
                  disabled={permission === PERMISSIONS.view || !me?.permissions.includes(permission)}
                  onCheckedChange={(on) => toggle(permission, on)}
                />
              ))}
            </div>
          </FieldSet>
        ))}
      </FieldGroup>
    </FormDialog>
  );
}
