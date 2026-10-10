// Novy pouzivatel (meno, rola, heslo) alebo uprava existujuceho (meno, rola, vypnutie).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { createUserMutation, errorText, updateUserMutation, type UserDto } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { CheckField, FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";
import { refreshRoles, roleSummary, useRoles } from "@/features/roles/roles";
import { refreshUsers } from "./users";

export function UserDialog({ initial, isNew, onClose }: { initial: UserDto; isNew: boolean; onClose: () => void }) {
  const [user, setUser] = useState(initial);
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const [mismatch, setMismatch] = useState(false);
  const roles = useRoles();
  const saved = () => {
    void refreshUsers();
    // pocty pouzivatelov pri rolach
    void refreshRoles();
    onClose();
  };
  const create = useMutation({ ...createUserMutation(), onSuccess: saved });
  const update = useMutation({ ...updateUserMutation(), onSuccess: saved });
  const failure = create.error ?? update.error;

  const save = () => {
    setMismatch(isNew && password !== again);
    if (isNew && password !== again) return;
    if (isNew) create.mutate({ body: { userName: user.userName, displayName: user.displayName, role: user.role, password } });
    else update.mutate({ path: { userName: user.userName }, body: { displayName: user.displayName, role: user.role, disabled: user.disabled } });
  };

  return (
    <FormDialog
      title={isNew ? t("dialog.user.new") : t("dialog.user.edit", { name: user.userName })}
      onClose={onClose}
      onSubmit={save}
      error={mismatch ? t("auth.passwordMismatch") : failure && errorText(failure)}
      busy={create.isPending || update.isPending}
    >
      <FieldGroup>
        <FormField label={t("auth.userName")} hint={isNew ? t("users.userName.hint") : undefined}>
          <Input value={user.userName} disabled={!isNew} autoComplete="off" onChange={(e) => setUser({ ...user, userName: e.target.value })} />
        </FormField>
        <FormField label={t("auth.displayName")}>
          <Input value={user.displayName} onChange={(e) => setUser({ ...user, displayName: e.target.value })} />
        </FormField>
        <FormField label={t("users.role")} hint={roleSummary(roles.find(user.role))}>
          <OptionSelect<string> value={user.role} onValueChange={(role) => setUser({ ...user, role })} options={roles.options} className="w-full" />
        </FormField>
        {isNew ? (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label={t("auth.password")}>
              <Input type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
            </FormField>
            <FormField label={t("auth.passwordAgain")}>
              <Input type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
            </FormField>
          </div>
        ) : (
          <CheckField label={t("users.disabled.field")} checked={user.disabled} onCheckedChange={(disabled) => setUser({ ...user, disabled })} />
        )}
      </FieldGroup>
    </FormDialog>
  );
}
