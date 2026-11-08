// Tlacidlo presmerovania liniek servera INISS2 na stranke Linky - len ked je v nastaveni server INISS2.
import { useQuery } from "@tanstack/react-query";
import { Waypoints } from "lucide-react";
import { getInissRedirectOptions } from "@/api";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { t } from "@/i18n";
import { useInissRedirect } from "./iniss-redirect";

export function InissRedirectButton() {
  // instalacia sa mohla zmenit (nastavenia, iny program) - pri kazdom otvoreni stranky znova
  const status = useQuery({ ...getInissRedirectOptions(), staleTime: 0 }).data;
  const redirect = useInissRedirect();
  if (!status?.available) return null;

  return (
    <Button
      variant="outline"
      disabled={redirect.pending}
      onClick={() => void redirect.toggle(status.redirected)}
      title={t(status.redirected ? "lines.redirect.undoHint" : "lines.redirect.hint")}
    >
      {redirect.pending ? <Spinner /> : <Waypoints />}
      {t(status.redirected ? "iniss.redirect.undo" : "lines.redirect")}
    </Button>
  );
}
