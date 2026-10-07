// Cache dat zo servera (TanStack Query). Zivy stav drzi aktualny WebSocket (live.ts), preto sa sam neobnovuje.
import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "./client";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: Infinity,
      refetchOnWindowFocus: false,
      // chyby servera (4xx) sa neopakuju - opakuje sa len nedostupny server
      retry: (failures, error) => failures < 3 && (!(error instanceof ApiError) || error.status === 0 || error.status >= 500),
    },
  },
});
