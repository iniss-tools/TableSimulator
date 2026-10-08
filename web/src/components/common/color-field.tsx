// Farba #rrggbb: vyber farby prehliadaca a text; volitelne "bez farby" (null) so zaskrtavacim polom.
import { useId } from "react";
import { Checkbox } from "@/components/ui/checkbox";
import { Field, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

const HEX = /^#[0-9a-fA-F]{6}$/;

interface ColorFieldProps {
  label: string;
  value: string | null;
  onChange: (value: string | null) => void;
  /** Text volby bez farby (null); bez neho je farba povinna. */
  noneLabel?: string;
  /** Farba, ktora sa nastavi po zruseni volby bez farby. */
  fallback?: string;
  className?: string;
}

export function ColorField({ label, value, onChange, noneLabel, fallback = "#000000", className }: ColorFieldProps) {
  const id = useId();
  const none = value === null;
  return (
    <Field className={cn("gap-1.5", className)}>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <div className="flex items-center gap-2">
        <input
          type="color"
          aria-label={label}
          className="h-8 w-10 shrink-0 cursor-pointer rounded-md border bg-transparent p-0.5 disabled:cursor-not-allowed disabled:opacity-40"
          value={value && HEX.test(value) ? value : fallback}
          disabled={none}
          onChange={(e) => onChange(e.target.value)}
        />
        <Input
          id={id}
          className="max-w-28 font-mono"
          value={value ?? ""}
          disabled={none}
          spellCheck={false}
          aria-invalid={value !== null && !HEX.test(value)}
          onChange={(e) => onChange(e.target.value.trim())}
        />
        {noneLabel && (
          <label className="flex items-center gap-1.5 text-sm text-muted-foreground">
            <Checkbox checked={none} onCheckedChange={(checked) => onChange(checked === true ? null : fallback)} />
            {noneLabel}
          </label>
        )}
      </div>
    </Field>
  );
}
