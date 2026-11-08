// Prehravanie novych hlaseni podla ziveho stavu simulatora, kym obsluha zvuk nestlmi.
import { useEffect, useRef } from "react";
import { useSimulatorState } from "@/api";
import { soundChanges, soundSteps } from "./announcement-sound";
import { playAnnouncement, stopAllAnnouncements, stopAnnouncement, unlockAudio } from "./sound-player";
import { useSoundEnabled } from "./sound-setting";

/** Hlasenia zacate po nacitani stranky a po zapnuti zvuku; zastavene v INISSe prestanu hrat aj v prehliadaci. */
export function useAnnouncementSound() {
  const { loaded, boards } = useSimulatorState();
  const enabled = useSoundEnabled();
  const previous = useRef<Set<string> | null>(null);

  useEffect(() => {
    if (!loaded) return;
    const changes = soundChanges(previous.current, boards);
    previous.current = changes.playing;
    for (const key of changes.stop) stopAnnouncement(key);
    if (enabled) for (const { key, announcement } of changes.start) void playAnnouncement(key, soundSteps(announcement));
  }, [loaded, boards, enabled]);

  // zapnuty zvuk z minulej navstevy prehliadac pusti az po prvej akcii obsluhy na stranke
  useEffect(() => {
    if (!enabled) {
      stopAllAnnouncements();
      return;
    }
    const options = { capture: true };
    document.addEventListener("pointerdown", unlockAudio, options);
    document.addEventListener("keydown", unlockAudio, options);
    return () => {
      document.removeEventListener("pointerdown", unlockAudio, options);
      document.removeEventListener("keydown", unlockAudio, options);
    };
  }, [enabled]);
}
