// Generovany klient API: hlavicky poziadaviek (CSRF, pristupovy kluc), chyby ako ApiError s textom podla kodu
// a reakcia na 401 (prihlasenie vyprsalo alebo pouzivatela vypli).
import { hasText, t } from "../i18n";
import { authHeaders } from "./auth";
import { client } from "./generated/client.gen";
import type { ApiProblem } from "./generated/types.gen";

/** Chyba volania API s textom pre obsluhu. */
export class ApiError extends Error {
  constructor(
    message: string,
    /** Stav HTTP; 0 = server neodpovedal. */
    readonly status: number,
    /** Kod chyby zo servera (ProblemDetails.code). */
    readonly code: string | null,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/** Text chyby pre obsluhu (aj pre chyby mimo API). */
export function errorText(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

let unauthorized: (() => void) | null = null;

/** Co urobit, ked server odpovie 401 mimo prihlasovania. */
export function onUnauthorized(handler: () => void) {
  unauthorized = handler;
}

/** Text chyby v jazyku obsluhy podla kodu; neznamy kod = popis zo servera. */
function describe(code: string | null | undefined, description: string | null | undefined, fallback: string): string {
  const key = `error.${code}`;
  return code && hasText(key) ? t(key) : description || fallback;
}

/** Chyba z odpovede servera - pri neplatnych vstupoch vsetky chyby pod sebou. */
export function problemError(response: Response, body: unknown): ApiError {
  const fallback =
    response.status === 401 ? t("error.auth.required") : response.status === 403 ? t("error.auth.forbidden") : `${response.status} ${response.statusText}`;
  const problem = body !== null && typeof body === "object" ? (body as ApiProblem) : null;
  const code = problem?.code ?? null;
  if (problem?.errors?.length) return new ApiError(problem.errors.map((e) => describe(e.code, e.description, fallback)).join("\n"), response.status, code);
  return new ApiError(describe(code, problem?.detail, fallback), response.status, code);
}

client.interceptors.request.use((request) => {
  for (const [name, value] of Object.entries(authHeaders())) request.headers.set(name, value);
  return request;
});

client.interceptors.error.use((error, response, request) => {
  if (!response) return new ApiError(t("server.offline"), 0, null);
  if (response.status === 401 && request && !new URL(request.url).pathname.startsWith("/api/auth/")) unauthorized?.();
  return problemError(response, error);
});

export { client };
