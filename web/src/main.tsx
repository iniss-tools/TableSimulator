import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { queryClient } from "./api";
import { router } from "./app/router";
import { TooltipProvider } from "./components/ui/tooltip";
import { getLanguage } from "./i18n";
// tema hned pri starte - aj pre kiosk a prihlasenie
import "./lib/theme";
import "./index.css";
import "./legacy.css";

document.documentElement.lang = getLanguage();
createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>
        <RouterProvider router={router} />
      </TooltipProvider>
    </QueryClientProvider>
  </StrictMode>,
);
