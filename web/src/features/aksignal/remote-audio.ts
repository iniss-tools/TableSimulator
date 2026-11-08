// Rozhlas vzdialeneho INISSu: stav fronty a jej popis pre kartu a stranku.
import type { RemoteQueueDto } from "@/api";

/** Stav fronty: vypnuta, volna alebo hra. */
export type QueueState = "off" | "idle" | "playing";

export const queueState = (queue: RemoteQueueDto): QueueState => (queue.off ? "off" : queue.playing ? "playing" : "idle");

/** Co hlasenie hlasi - titulok, inak nahravky. */
export const announcementText = (a: { title?: string | null; sounds: string[] }) => a.title || a.sounds.join(" · ");
