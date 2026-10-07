// Skopirovanie textu do schranky s potvrdenim (ikona sa na chvilu zmeni).
import { useState } from "react";
import { Check, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";

export function CopyButton({ text, label }: { text: string; label?: string }) {
  const [done, setDone] = useState(false);
  return (
    <Button
      type="button"
      variant={label ? "outline" : "ghost"}
      size={label ? "default" : "icon-sm"}
      title={done ? t("common.copied") : t("common.copy")}
      onClick={() =>
        navigator.clipboard?.writeText(text).then(() => {
          setDone(true);
          setTimeout(() => setDone(false), 1500);
        })
      }
    >
      {done ? <Check /> : <Copy />}
      {label}
    </Button>
  );
}
