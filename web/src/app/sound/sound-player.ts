// Prehravanie hlaseni v prehliadaci cez Web Audio: nahravky z banky INISSu (GET /api/sounds) po sebe s pauzami.
import { getSound } from "@/api";
import type { SoundStep } from "./announcement-sound";

/** Najviac zapamatanych nahravok (hlasenia opakuju rovnake slova). */
const CACHE_SIZE = 300;

interface Playing {
  sources: AudioBufferSourceNode[];
  cancelled: boolean;
}

let context: AudioContext | null = null;
const buffers = new Map<string, Promise<AudioBuffer | null>>();
const playing = new Map<string, Playing>();

/** Kontext zvuku; prehliadac ho pusti az po kliknuti alebo klavese obsluhy - volat z ich obsluhy. */
export function unlockAudio() {
  context ??= new AudioContext();
  if (context.state === "suspended") void context.resume();
}

function load(audio: AudioContext, name: string): Promise<AudioBuffer | null> {
  let buffer = buffers.get(name);
  if (!buffer) {
    buffer = getSound({ query: { name }, parseAs: "arrayBuffer" })
      .then(({ data }) => (data instanceof ArrayBuffer ? audio.decodeAudioData(data) : null))
      .catch(() => null)
      .then((decoded) => {
        // chybajucu nahravku skusit znova pri dalsom hlaseni (banka sa mohla zmenit)
        if (!decoded) buffers.delete(name);
        return decoded;
      });
    buffers.set(name, buffer);
    if (buffers.size > CACHE_SIZE) buffers.delete(buffers.keys().next().value!);
  }
  return buffer;
}

/**
 * Prehra hlasenie: nahravky po sebe s pauzami. Nahravku, ktora sa neda nacitat, vynecha. Bez povoleneho zvuku (obsluha
 * na stranke este neklikla) nehra nic - hlasenie by zaznelo az neskor.
 */
export async function playAnnouncement(key: string, steps: SoundStep[]) {
  unlockAudio();
  const audio = context!;
  if (audio.state !== "running" || steps.length === 0) return;
  stopAnnouncement(key);
  const entry: Playing = { sources: [], cancelled: false };
  playing.set(key, entry);

  const loaded = await Promise.all(steps.map((step) => load(audio, step.name)));
  if (entry.cancelled || audio.state !== "running") return;
  let at = audio.currentTime + 0.05;
  steps.forEach((step, i) => {
    at += step.beforeMs / 1000;
    const buffer = loaded[i];
    if (buffer) {
      const source = audio.createBufferSource();
      source.buffer = buffer;
      source.connect(audio.destination);
      source.start(at);
      entry.sources.push(source);
      at += buffer.duration;
    }
    at += step.afterMs / 1000;
  });

  const last = entry.sources.at(-1);
  if (!last) playing.delete(key);
  else
    last.onended = () => {
      if (playing.get(key) === entry) playing.delete(key);
    };
}

/** Zastavi hlasenie (INISS ho zastavil alebo zrusil). */
export function stopAnnouncement(key: string) {
  const entry = playing.get(key);
  if (!entry) return;
  entry.cancelled = true;
  for (const source of entry.sources) {
    try {
      source.stop();
    } catch {
      // este nezacala alebo uz dohrala
    }
  }
  playing.delete(key);
}

/** Zastavi vsetky hlasenia (stlmenie). */
export function stopAllAnnouncements() {
  for (const key of [...playing.keys()]) stopAnnouncement(key);
}
