// Spinacia jednotka zosilnovaca ELSVO: kontrolka zopnutia, pasmo a bity stavu, ktorym jednotka odpoveda INISSu.
// Texty dodava volajuci (i18n), mena bitov su nazvy z logu INISSu.
import type { AmplifierDto } from "../api/generated/types.gen";

/** Rozmer platna pri mierke 1. */
export const AMPLIFIER_WIDTH = 360;
export const AMPLIFIER_HEIGHT = 128;

/** Bity stavu zlava doprava (najvyssi prvy) - nazvy ako v logu INISSu; 40 nema nazov. */
export const AMPLIFIER_BITS: { bit: number; name: string }[] = [
  { bit: 0x80, name: "Ready" },
  { bit: 0x40, name: "40" },
  { bit: 0x20, name: "TransferErr" },
  { bit: 0x10, name: "Hlaseni" },
  { bit: 0x08, name: "Break" },
  { bit: 0x04, name: "HwErr" },
  { bit: 0x02, name: "OP5" },
  { bit: 0x01, name: "Tlf" },
];

/** Bity, ktore pri rozsvieteni znamenaju chybu (cervena kontrolka). */
const ERROR_BITS = 0x20 | 0x04;

/** Nastavene bity stavu. */
export const amplifierBits = (state: number) => AMPLIFIER_BITS.filter((b) => (state & b.bit) !== 0).map((b) => b.name);

export interface AmplifierTexts {
  /** Hlavny stav, napr. "Zopnutý – 1. pásmo". */
  title: string;
  /** Druhy riadok, napr. stav jednotky hex. */
  detail: string;
}

/** Nakresli jednotku do platna v mierke (body platna na bod pri mierke 1). */
export function drawAmplifier(ctx: CanvasRenderingContext2D, amplifier: AmplifierDto, texts: AmplifierTexts, scale: number) {
  ctx.save();
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  ctx.clearRect(0, 0, AMPLIFIER_WIDTH, AMPLIFIER_HEIGHT);

  // skrinka
  ctx.fillStyle = "#1b1d21";
  ctx.beginPath();
  ctx.roundRect(0, 0, AMPLIFIER_WIDTH, AMPLIFIER_HEIGHT, 8);
  ctx.fill();
  ctx.strokeStyle = "#33363c";
  ctx.lineWidth = 2;
  ctx.stroke();

  // kontrolka zopnutia
  const on = amplifier.band > 0;
  const cx = 52;
  const cy = 50;
  if (on) {
    const glow = ctx.createRadialGradient(cx, cy, 8, cx, cy, 40);
    glow.addColorStop(0, "rgba(80, 255, 110, 0.55)");
    glow.addColorStop(1, "rgba(80, 255, 110, 0)");
    ctx.fillStyle = glow;
    ctx.fillRect(cx - 40, cy - 40, 80, 80);
  }
  ctx.beginPath();
  ctx.arc(cx, cy, 24, 0, Math.PI * 2);
  ctx.fillStyle = on ? "#45e463" : "#2a2d31";
  ctx.fill();
  ctx.lineWidth = 3;
  ctx.strokeStyle = on ? "#b8ffc4" : "#45484e";
  ctx.stroke();
  if (on) {
    ctx.fillStyle = "#0d3a17";
    ctx.font = "bold 20px system-ui, sans-serif";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(String(amplifier.band), cx, cy + 1);
  }

  // texty
  ctx.textAlign = "left";
  ctx.textBaseline = "alphabetic";
  ctx.fillStyle = on ? "#e9ffe9" : "#c9ccd1";
  ctx.font = "bold 18px system-ui, sans-serif";
  ctx.fillText(texts.title, 96, 44, AMPLIFIER_WIDTH - 108);
  ctx.fillStyle = "#8d929a";
  ctx.font = "13px ui-monospace, monospace";
  ctx.fillText(texts.detail, 96, 66, AMPLIFIER_WIDTH - 108);

  // bity stavu
  const step = (AMPLIFIER_WIDTH - 24) / AMPLIFIER_BITS.length;
  ctx.font = "9px system-ui, sans-serif";
  ctx.textAlign = "center";
  AMPLIFIER_BITS.forEach((b, i) => {
    const x = 12 + step * i + step / 2;
    const lit = (amplifier.state & b.bit) !== 0;
    ctx.beginPath();
    ctx.arc(x, 98, 5, 0, Math.PI * 2);
    ctx.fillStyle = lit ? ((b.bit & ERROR_BITS) !== 0 ? "#ff4a3a" : "#ffc21a") : "#33363c";
    ctx.fill();
    ctx.fillStyle = lit ? "#e1e3e6" : "#6c7178";
    ctx.fillText(b.name, x, 118, step - 2);
  });
  ctx.restore();
}
