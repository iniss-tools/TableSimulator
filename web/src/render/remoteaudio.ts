// Rozhlas vzdialeneho INISSu: riadok kazdej fronty hlaseni s kontrolkou (hra, volna, vypnuta), textom hlasenia
// a priebehom. Texty dodava volajuci (i18n).

/** Sirka platna pri mierke 1. */
export const REMOTE_AUDIO_WIDTH = 360;

/** Najviac riadkov front na platne. */
export const REMOTE_AUDIO_MAX_ROWS = 4;

const HEADER = 40;
const ROW = 36;
const PADDING = 10;

/** Fronta na platne. */
export interface RemoteAudioRow {
  state: "off" | "idle" | "playing";
  /** Meno fronty. */
  name: string;
  /** Co hra alebo stav fronty. */
  text: string;
  /** Priebeh hlasenia 0-1; null = nehra alebo bez dlzky. */
  progress: number | null;
}

export interface RemoteAudioPanel {
  /** Hlavny stav, napr. "Hlási – 1 fronta". */
  title: string;
  rows: RemoteAudioRow[];
}

/** Vyska platna pri mierke 1 podla poctu front. */
export const remoteAudioHeight = (rows: number) => HEADER + Math.max(1, Math.min(REMOTE_AUDIO_MAX_ROWS, rows)) * ROW + PADDING;

const LAMP: Record<RemoteAudioRow["state"], string> = { playing: "#45e463", idle: "#45484e", off: "#ff4a3a" };

/** Nakresli rozhlas do platna v mierke (body platna na bod pri mierke 1). */
export function drawRemoteAudio(ctx: CanvasRenderingContext2D, panel: RemoteAudioPanel, scale: number) {
  const rows = panel.rows.slice(0, REMOTE_AUDIO_MAX_ROWS);
  const height = remoteAudioHeight(rows.length);
  ctx.save();
  ctx.setTransform(scale, 0, 0, scale, 0, 0);
  ctx.clearRect(0, 0, REMOTE_AUDIO_WIDTH, height);

  // skrinka
  ctx.fillStyle = "#1b1d21";
  ctx.beginPath();
  ctx.roundRect(0, 0, REMOTE_AUDIO_WIDTH, height, 8);
  ctx.fill();
  ctx.strokeStyle = "#33363c";
  ctx.lineWidth = 2;
  ctx.stroke();

  const playing = rows.some((r) => r.state === "playing");
  ctx.textAlign = "left";
  ctx.textBaseline = "alphabetic";
  ctx.fillStyle = playing ? "#e8fbe9" : "#c9ccd1";
  ctx.font = "bold 16px system-ui, sans-serif";
  ctx.fillText(panel.title, 14, 26, REMOTE_AUDIO_WIDTH - 28);

  rows.forEach((row, i) => {
    const top = HEADER + i * ROW;
    // kontrolka
    ctx.beginPath();
    ctx.arc(20, top + 12, 6, 0, Math.PI * 2);
    ctx.fillStyle = LAMP[row.state];
    ctx.fill();
    // meno fronty a text
    ctx.fillStyle = "#e1e3e6";
    ctx.font = "bold 12px system-ui, sans-serif";
    ctx.fillText(row.name, 34, top + 16, 90);
    ctx.fillStyle = row.state === "playing" ? "#d6f5d9" : "#8d929a";
    ctx.font = "12px ui-monospace, monospace";
    ctx.fillText(row.text, 130, top + 16, REMOTE_AUDIO_WIDTH - 144);
    // priebeh
    if (row.progress !== null) {
      ctx.fillStyle = "#33363c";
      ctx.fillRect(34, top + 24, REMOTE_AUDIO_WIDTH - 48, 4);
      ctx.fillStyle = "#45e463";
      ctx.fillRect(34, top + 24, (REMOTE_AUDIO_WIDTH - 48) * Math.min(1, Math.max(0, row.progress)), 4);
    }
  });
  ctx.restore();
}
