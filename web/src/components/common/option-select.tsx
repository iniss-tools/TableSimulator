// Vyber jednej z moznosti (Select z Base UI) - hodnota moze byt text aj cislo, popis sa zobrazi aj vo vybranom poli.
import { useMemo, type ReactNode } from "react";
import { cn } from "@/lib/utils";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export interface Option<T extends string | number> {
  value: T;
  label: ReactNode;
  disabled?: boolean;
}

interface OptionSelectProps<T extends string | number> {
  value: T | null;
  onValueChange: (value: T) => void;
  options: Option<T>[];
  id?: string;
  disabled?: boolean;
  placeholder?: ReactNode;
  size?: "sm" | "default";
  ariaLabel?: string;
  className?: string;
}

export function OptionSelect<T extends string | number>({
  value,
  onValueChange,
  options,
  id,
  disabled,
  placeholder,
  size,
  ariaLabel,
  className,
}: OptionSelectProps<T>) {
  // Base UI podla items ukaze popis vybranej hodnoty
  const items = useMemo(() => options.map((o) => ({ value: o.value, label: o.label })), [options]);
  return (
    <Select<T> value={value} onValueChange={(v) => v !== null && onValueChange(v)} items={items} disabled={disabled}>
      <SelectTrigger id={id} size={size} aria-label={ariaLabel} className={cn("min-w-20", className)}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={String(o.value)} value={o.value} disabled={o.disabled}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
