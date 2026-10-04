// Modalne okno nad strankou (element <dialog>).
import { useEffect, useRef, type ReactNode } from "react";
import { t } from "../i18n";

interface Props {
  title: string;
  children: ReactNode;
  onClose: () => void;
  onSubmit: () => void;
  error?: string | null;
  busy?: boolean;
}

export function Dialog({ title, children, onClose, onSubmit, error, busy }: Props) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    ref.current?.showModal();
  }, []);

  return (
    <dialog ref={ref} className="dialog" onClose={onClose} onCancel={onClose}>
      <form
        method="dialog"
        onSubmit={(e) => {
          e.preventDefault();
          onSubmit();
        }}
      >
        <h2>{title}</h2>
        <div className="dialog-body">{children}</div>
        {error && <p className="error">{error}</p>}
        <div className="dialog-buttons">
          <button type="button" className="secondary" onClick={onClose}>
            {t("common.cancel")}
          </button>
          <button type="submit" disabled={busy}>
            {t("common.save")}
          </button>
        </div>
      </form>
    </dialog>
  );
}

interface FieldProps {
  label: string;
  hint?: string;
  children: ReactNode;
  wide?: boolean;
}

/** Popisok s prvkom formulara. */
export function Field({ label, hint, children, wide }: FieldProps) {
  return (
    <label className={wide ? "field field-wide" : "field"}>
      <span className="field-label">{label}</span>
      {children}
      {hint && <span className="field-hint">{hint}</span>}
    </label>
  );
}
