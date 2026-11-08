// Tlacidlo zvuku hlaseni v zahlavi - len ked ma stanica zariadenie s nahravkami (AK Signal UH, vzdialeny INISS).
import { Volume2, VolumeX } from "lucide-react";
import { useSimulatorState } from "@/api";
import { Button } from "@/components/ui/button";
import { t } from "@/i18n";
import { hasSound } from "./announcement-sound";
import { setSoundEnabled, useSoundEnabled } from "./sound-setting";

export function SoundToggle() {
  const { boards } = useSimulatorState();
  const enabled = useSoundEnabled();
  if (!boards.some(hasSound)) return null;

  return (
    <Button
      variant="ghost"
      size="icon-sm"
      aria-label={t("sound.label")}
      aria-pressed={enabled}
      title={t(enabled ? "sound.on" : "sound.off")}
      onClick={() => setSoundEnabled(!enabled)}
    >
      {enabled ? <Volume2 /> : <VolumeX className="text-muted-foreground" />}
    </Button>
  );
}
