// Ram tabule spolocny pre body aj listy: plochy alebo kovovy profil s prechodom a lesklou hranou, stitok vyrobcu
// vpravo dole. Kresli sa v px platna (uz v mierke).
import type { BoardDesign } from "./design";

/** Farba #rrggbb svetlejsia (amount > 0, k bielej) alebo tmavsia (amount < 0, k ciernej); amount -1 az 1. */
export function shade(color: string, amount: number): string {
  const hex = /^#([0-9a-fA-F]{6})$/.exec(color);
  if (!hex) return color;
  const value = Number.parseInt(hex[1], 16);
  const channel = (shift: number) => {
    const c = (value >> shift) & 0xff;
    const mixed = amount >= 0 ? c + (255 - c) * amount : c * (1 + amount);
    return Math.max(0, Math.min(255, Math.round(mixed)))
      .toString(16)
      .padStart(2, "0");
  };
  return `#${channel(16)}${channel(8)}${channel(0)}`;
}

/** Ram okolo plochy w × h px; vnutro (plech) kresli volajuci. */
export function drawFrame(ctx: CanvasRenderingContext2D, w: number, h: number, frame: BoardDesign["frame"], scale: number) {
  const width = frame.width * scale;
  if (width <= 0) return;
  const radius = frame.radius * scale;
  if (frame.style === "metal") {
    // hlinikovy profil: svetly hore, tmavsi dole
    const gradient = ctx.createLinearGradient(0, 0, 0, h);
    gradient.addColorStop(0, shade(frame.color, 0.45));
    gradient.addColorStop(0.5, frame.color);
    gradient.addColorStop(1, shade(frame.color, -0.3));
    ctx.fillStyle = gradient;
  } else ctx.fillStyle = frame.color;
  ctx.beginPath();
  ctx.roundRect(0, 0, w, h, radius);
  ctx.fill();

  if (frame.style === "metal") {
    // lesk na vonkajsej hrane a tien na vnutornej
    const line = Math.max(1, width * 0.1);
    ctx.lineWidth = line;
    ctx.strokeStyle = shade(frame.color, 0.65);
    ctx.beginPath();
    ctx.roundRect(line / 2, line / 2, w - line, h - line, Math.max(0, radius - line / 2));
    ctx.stroke();
    ctx.strokeStyle = shade(frame.color, -0.5);
    ctx.strokeRect(width - line / 2, width - line / 2, w - 2 * width + line, h - 2 * width + line);
  }

  if (frame.badge) {
    const height = width * 0.62;
    ctx.font = `bold ${height * 0.7}px Arial, Helvetica, sans-serif`;
    ctx.letterSpacing = "0px";
    const text = ctx.measureText(frame.badge).width;
    const pill = text + height;
    const x = w - width * 3 - pill;
    const y = h - width + (width - height) / 2;
    ctx.fillStyle = "#1b1c1f";
    ctx.beginPath();
    ctx.roundRect(x, y, pill, height, height / 2);
    ctx.fill();
    ctx.fillStyle = "#e9eaec";
    ctx.textBaseline = "middle";
    ctx.textAlign = "center";
    ctx.fillText(frame.badge, x + pill / 2, y + height / 2);
    ctx.textAlign = "left";
  }
}
