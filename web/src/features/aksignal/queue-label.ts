// Meno fronty vzdialeneho INISSu pre obsluhu (texty i18n - oddelene od cistych funkcii v remote-audio.ts).
import { t } from "@/i18n";

/** Meno fronty; jedina fronta na linke meno nema. */
export const queueLabel = (name: string | null | undefined) => (name ? name : t("remoteaudio.queue.single"));
