// Nove heslo pouzivatela (spravca) alebo vlastne heslo (s overenim sucasneho).
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { changePasswordMutation, errorText, setUserPasswordMutation } from "@/api";
import { FormDialog } from "@/components/common/form-dialog";
import { FormField } from "@/components/common/form-field";
import { FieldGroup } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { t } from "@/i18n";

/** userName null = vlastne heslo prihlaseneho. */
export function PasswordDialog({ userName, onClose }: { userName: string | null; onClose: (changed: boolean) => void }) {
  const own = userName === null;
  const [current, setCurrent] = useState("");
  const [password, setPassword] = useState("");
  const [again, setAgain] = useState("");
  const [mismatch, setMismatch] = useState(false);
  const changeOwn = useMutation({ ...changePasswordMutation(), onSuccess: () => onClose(true) });
  const changeUser = useMutation({ ...setUserPasswordMutation(), onSuccess: () => onClose(true) });
  const failure = changeOwn.error ?? changeUser.error;

  const save = () => {
    setMismatch(password !== again);
    if (password !== again) return;
    if (own) changeOwn.mutate({ body: { currentPassword: current, newPassword: password } });
    else changeUser.mutate({ path: { userName }, body: { password } });
  };

  return (
    <FormDialog
      title={own ? t("dialog.password.own") : t("dialog.password.user", { name: userName })}
      description={own ? undefined : t("users.newPassword.hint")}
      onClose={() => onClose(false)}
      onSubmit={save}
      error={mismatch ? t("auth.passwordMismatch") : failure && errorText(failure)}
      busy={changeOwn.isPending || changeUser.isPending}
    >
      <FieldGroup>
        {own && (
          <FormField label={t("users.currentPassword")}>
            <Input type="password" autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} />
          </FormField>
        )}
        <FormField label={t("users.newPassword")}>
          <Input type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </FormField>
        <FormField label={t("auth.passwordAgain")}>
          <Input type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
        </FormField>
      </FieldGroup>
    </FormDialog>
  );
}
