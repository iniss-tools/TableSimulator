// Polia formulara nad shadcn Field: popis spojeny s prvkom (id), napoveda pod nim; zaskrtavacie pole s popisom vedla.
import { cloneElement, useId, type ReactElement, type ReactNode } from "react";
import { cn } from "@/lib/utils";
import { Checkbox } from "@/components/ui/checkbox";
import { Field, FieldContent, FieldDescription, FieldLabel } from "@/components/ui/field";

interface FormFieldProps {
  label: ReactNode;
  hint?: ReactNode;
  /** Jeden prvok formulara - dostane id (ak ho nema), aby ho popis oznacil. */
  children: ReactElement<{ id?: string }>;
  className?: string;
}

export function FormField({ label, hint, children, className }: FormFieldProps) {
  const generated = useId();
  const id = children.props.id ?? generated;
  return (
    <Field className={className}>
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      {cloneElement(children, { id })}
      {hint && <FieldDescription>{hint}</FieldDescription>}
    </Field>
  );
}

interface CheckFieldProps {
  label: ReactNode;
  hint?: ReactNode;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  className?: string;
}

/** Zaskrtavacie pole s popisom (a napovedou) vpravo. */
export function CheckField({ label, hint, checked, onCheckedChange, disabled, className }: CheckFieldProps) {
  const id = useId();
  return (
    <Field orientation="horizontal" className={cn("items-start", className)} data-disabled={disabled || undefined}>
      <Checkbox id={id} checked={checked} onCheckedChange={(value) => onCheckedChange(value === true)} disabled={disabled} />
      <FieldContent>
        <FieldLabel htmlFor={id} className="font-normal">
          {label}
        </FieldLabel>
        {hint && <FieldDescription>{hint}</FieldDescription>}
      </FieldContent>
    </Field>
  );
}
