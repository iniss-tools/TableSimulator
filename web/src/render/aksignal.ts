// Ustredna AK Signal: lampa svetla nastupista, rezim a kontrolky stavu (automatika, rucne, tma, porucha, rozhlas,
// ticho servera). Texty dodava volajuci (i18n).

/** Rozmer platna pri mierke 1. */
export const AKSIGNAL_WIDTH = 360;
export const AKSIGNAL_HEIGHT = 128;

/** Co ustredna ukazuje. */
export interface AkSignalPanel {
  lightOn: boolean;
  auto: boolean;
  manual: boolean;
  dark: boolean;
  fault: boolean;
  /** Hra rozhlas (aspon jedno hlasenie). */
  audio: boolean;
  /** Server sa ustredne dlhsie nepyta. */
  silent: boolean;
}

export interface AkSignalTexts {
  /** Hlavny stav, napr. "Svieti – automatika". */
  title: string;
  /** Druhy riadok, napr. "tma · stav 0D". */
  detail: string;
  /** Popisy kontroliek v poradi automatika, rucne, tma, porucha, rozhlas, server. */
  labels: [string, string, string, string, string, string];
}

/** Nakresli ustrednu do platna v mierke (body platna na bod pri mierke 1). */
export function drawAkSignal(ctx: CanvasRenderingContext2D, panel: AkSignalPanel, texts: AkSignalTexts, scale: number) {
  ctx.save();
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  ctx.clearRect(0, 0, AKSIGNAL_WIDTH, AKSIGNAL_HEIGHT);

  // skrinka
  ctx.fillStyle = "#1b1d21";
  ctx.beginPath();
  ctx.roundRect(0, 0, AKSIGNAL_WIDTH, AKSIGNAL_HEIGHT, 8);
  ctx.fill();
  ctx.strokeStyle = "#33363c";
  ctx.lineWidth = 2;
  ctx.stroke();

  // lampa svetla
  const cx = 52;
  const cy = 50;
  if (panel.lightOn) {
    const glow = ctx.createRadialGradient(cx, cy, 8, cx, cy, 44);
    glow.addColorStop(0, "rgba(255, 214, 74, 0.6)");
    glow.addColorStop(1, "rgba(255, 214, 74, 0)");
    ctx.fillStyle = glow;
    ctx.fillRect(cx - 44, cy - 44, 88, 88);
  }
  ctx.beginPath();
  ctx.arc(cx, cy, 24, 0, Math.PI * 2);
  ctx.fillStyle = panel.lightOn ? "#ffd64a" : "#2a2d31";
  ctx.fill();
  ctx.lineWidth = 3;
  ctx.strokeStyle = panel.fault ? "#ff4a3a" : panel.lightOn ? "#fff3c2" : "#45484e";
  ctx.stroke();

  // texty
  ctx.textAlign = "left";
  ctx.textBaseline = "alphabetic";
  ctx.fillStyle = panel.lightOn ? "#fff8e1" : "#c9ccd1";
  ctx.font = "bold 18px system-ui, sans-serif";
  ctx.fillText(texts.title, 96, 44, AKSIGNAL_WIDTH - 108);
  ctx.fillStyle = "#8d929a";
  ctx.font = "13px ui-monospace, monospace";
  ctx.fillText(texts.detail, 96, 66, AKSIGNAL_WIDTH - 108);

  // kontrolky
  const lamps: { lit: boolean; color: string }[] = [
    { lit: panel.auto, color: "#ffc21a" },
    { lit: panel.manual, color: "#ffc21a" },
    { lit: panel.dark, color: "#7aa7ff" },
    { lit: panel.fault, color: "#ff4a3a" },
    { lit: panel.audio, color: "#45e463" },
    { lit: panel.silent, color: "#ff4a3a" },
  ];
  const step = (AKSIGNAL_WIDTH - 24) / lamps.length;
  ctx.font = "9px system-ui, sans-serif";
  ctx.textAlign = "center";
  lamps.forEach((lamp, i) => {
    const x = 12 + step * i + step / 2;
    ctx.beginPath();
    ctx.arc(x, 98, 5, 0, Math.PI * 2);
    ctx.fillStyle = lamp.lit ? lamp.color : "#33363c";
    ctx.fill();
    ctx.fillStyle = lamp.lit ? "#e1e3e6" : "#6c7178";
    ctx.fillText(texts.labels[i], x, 118, step - 2);
  });
  ctx.restore();
}
