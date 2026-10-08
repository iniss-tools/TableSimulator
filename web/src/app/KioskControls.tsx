// Ovladanie kiosku - ukaze sa na chvilu po pohybe mysi alebo dotyku: cela obrazovka a nainstalovanie na plochu.
import { Download, Maximize, Minimize } from "lucide-react";
import { cn } from "@/lib/utils";
import { t } from "@/i18n";
import { useInstall } from "./install";
import { toggleFullscreen, useFullscreen } from "./kiosk";

export function KioskControls({ visible }: { visible: boolean }) {
  const fullscreen = useFullscreen();
  const install = useInstall();
  const button = "flex items-center gap-1.5 rounded-md bg-black/70 px-3 py-2 text-sm text-white ring-1 ring-white/20 hover:bg-black/90";
  return (
    <div className={cn("fixed top-3 right-3 z-10 flex gap-2 transition-opacity duration-300", visible ? "opacity-100" : "pointer-events-none opacity-0")}>
      {install && (
        <button type="button" className={button} onClick={install}>
          <Download className="size-4" />
          {t("kiosk.install")}
        </button>
      )}
      <button type="button" className={button} onClick={toggleFullscreen}>
        {fullscreen ? <Minimize className="size-4" /> : <Maximize className="size-4" />}
        {t(fullscreen ? "kiosk.exitFullscreen" : "kiosk.fullscreen")}
      </button>
    </div>
  );
}
