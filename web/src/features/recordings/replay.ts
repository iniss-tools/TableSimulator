// Prehravanie zaznamu: rychlosti, poloha v case (dopocitana medzi spravami servera) a dlzka ako m:ss.
import type { ReplayStatusDto } from "@/api";

/** Rychlosti prehravania; 0 = bez cakania. */
export const SPEEDS = [1, 2, 5, 10, 0];

/**
 * Stav <c>next</c> je starsi nez <c>current</c> toho isteho prehravania (cas servera) - sprava o priebehu odoslana pred
 * posunom moze prist az po odpovedi na posun a nesmie vratit polohu spat.
 */
export function isOlderReplay(next: ReplayStatusDto | null | undefined, current: ReplayStatusDto | null | undefined): boolean {
  return !!next && !!current && next.name === current.name && Date.parse(next.at) < Date.parse(current.at);
}

/** Kde prehravanie je v case <c>now</c>: pocas prehravania sa poloha posuva s rychlostou, najviac po koniec zaznamu. */
export function livePosition(replay: ReplayStatusDto, now: number): number {
  if (replay.state !== "running" || replay.speed <= 0) return replay.position;
  const elapsed = Math.max(0, (now - Date.parse(replay.at)) / 1000);
  return Math.min(replay.duration, replay.position + elapsed * replay.speed);
}

/** Dlzka v sekundach ako m:ss alebo h:mm:ss. */
export function duration(seconds: number): string {
  const s = Math.max(0, Math.round(seconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const pad = (n: number) => String(n).padStart(2, "0");
  return h > 0 ? `${h}:${pad(m)}:${pad(s % 60)}` : `${m}:${pad(s % 60)}`;
}
