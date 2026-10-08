// kiosk z plochy: ?view= na adresu za # este pred smerovacom
import "./app/start-view";
// ponuka instalacie prichadza hned po nacitani - zachytit skor, nez sa otvori kiosk
import "./app/install";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { queryClient } from "./api";
import { router } from "./app/router";
import { ConfirmHost } from "./components/common/confirm-host";
import { Toaster } from "./components/ui/sonner";
import { TooltipProvider } from "./components/ui/tooltip";
import { getLanguage } from "./i18n";
// tema hned pri starte - aj pre kiosk a prihlasenie
import "./lib/theme";
import "./index.css";

document.documentElement.lang = getLanguage();

// po aktualizacii servera otvorena stranka nenajde stare casti rozhrania - nacita sa znova (najviac raz za minutu)
addEventListener("vite:preloadError", (event) => {
  try {
    const last = Number(sessionStorage.getItem("reloadedAt") ?? 0);
    if (Date.now() - last < 60_000) return;
    sessionStorage.setItem("reloadedAt", String(Date.now()));
  } catch {
    // ulozisko nedostupne - nacita sa aj tak
  }
  event.preventDefault();
  location.reload();
});
// rozhranie v pamati prehliadaca - kiosk sa nacita aj pocas restartu servera (len HTTPS alebo tento pocitac)
if (import.meta.env.PROD && "serviceWorker" in navigator && isSecureContext) {
  void navigator.serviceWorker.register("/sw.js").catch(() => undefined);
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>
        <RouterProvider router={router} />
        <ConfirmHost />
        <Toaster position="top-right" richColors closeButton />
      </TooltipProvider>
    </QueryClientProvider>
  </StrictMode>,
);
