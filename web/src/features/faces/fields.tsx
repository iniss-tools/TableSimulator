// Polia panelov editora ciel: sekcia panelu, cislo v bodoch a farba (predvolena, farba vzhladu alebo vlastna).
import type { ReactNode } from "react";
import { FormField } from "@/components/common/form-field";
import { OptionSelect } from "@/components/common/option-select";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { t, type TextKey } from "@/i18n";
import { FACE_COLOR_TOKENS } from "./faces";

const HEX = /^#[0-9a-fA-F]{6}$/;
/** Vlastna farba #rrggbb vo vybere farieb. */
const CUSTOM = "#";

/** Sekcia panelu s nadpisom; polia v dvoch stlpcoch. */
export function PanelSection({ title, children, className }: { title: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={cn("flex flex-col gap-2 border-t pt-3 first:border-t-0 first:pt-0", className)}>
      <h3 className="text-xs font-medium text-muted-foreground">{title}</h3>
      <div className="grid grid-cols-2 gap-x-2 gap-y-3">{children}</div>
    </section>
  );
}

export function NumberField({
  label,
  value,
  onChange,
  disabled,
  step = 0.5,
  min,
  hint,
  className,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
  disabled: boolean;
  step?: number;
  min?: number;
  hint?: ReactNode;
  className?: string;
}) {
  return (
    <FormField label={label} hint={hint} className={className}>
      <Input
        type="number"
        step={step}
        min={min}
        value={value}
        disabled={disabled}
        onChange={(e) => {
          const parsed = Number.parseFloat(e.target.value);
          onChange(Number.isFinite(parsed) ? parsed : 0);
        }}
      />
    </FormField>
  );
}

/** Farba: predvolena (prazdna), nazov farby vzhladu alebo vlastna #rrggbb. */
export function ColorField({
  label,
  value,
  emptyLabel,
  onChange,
  disabled,
  className = "col-span-2",
}: {
  label: string;
  value: string;
  emptyLabel: string;
  onChange: (value: string) => void;
  disabled: boolean;
  className?: string;
}) {
  const custom = value.startsWith("#");
  return (
    <FormField label={label} className={className}>
      <div className="flex items-center gap-2">
        <OptionSelect
          value={custom ? CUSTOM : value}
          disabled={disabled}
          onValueChange={(v) => onChange(v === CUSTOM ? "#ffffff" : v)}
          options={[
            { value: "", label: emptyLabel },
            ...FACE_COLOR_TOKENS.map((token) => ({ value: token, label: t(`face.color.${token}` as TextKey) })),
            { value: CUSTOM, label: t("face.color.custom") },
          ]}
          className="min-w-0 flex-1"
        />
        {custom && (
          <input
            type="color"
            aria-label={label}
            className="h-8 w-10 shrink-0 cursor-pointer rounded-md border bg-transparent p-0.5"
            value={HEX.test(value) ? value : "#ffffff"}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value)}
          />
        )}
      </div>
    </FormField>
  );
}
