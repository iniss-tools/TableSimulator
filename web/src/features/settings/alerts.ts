// Oznamenia o vypadkoch liniek: nastavenie z formulara na odoslanie serveru.
import type { AlertSettingsDto, AlertSettingsRequest } from "@/api";

/** Nastavenie oznameni na odoslanie - heslo SMTP null = nechat ulozene. */
export function alertsRequest(alerts: AlertSettingsDto, smtpPassword: string | null): AlertSettingsRequest {
  const { smtpPasswordSet: _, ...rest } = alerts;
  return { ...rest, smtpPassword };
}
