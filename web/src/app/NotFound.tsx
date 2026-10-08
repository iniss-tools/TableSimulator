// Neznama adresa (aj mimo ramu, napr. preklep v odkaze zariadenia) - text a navrat na uvodnu stranku.
import { Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";

export function NotFound() {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4 bg-background p-4 text-center">
      <p className="text-lg font-medium">{t("nav.notFound")}</p>
      <Button nativeButton={false} render={<Link to="/" />}>
        {t("nav.notFound.back")}
      </Button>
    </div>
  );
}
