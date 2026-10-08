// Server este nebezi (zariadenie sa zaplo skor, restart servera) - stranka caka a pripoji sa sama.
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";

export function ServerWaiting() {
  return (
    <div role="status" className="flex min-h-dvh flex-col items-center justify-center gap-3 bg-background p-4 text-center text-muted-foreground">
      <Spinner className="size-6" />
      <p>{t("server.waiting")}</p>
    </div>
  );
}
