// Volby zobrazenia editora ciel (obrysy, mriezka, prichytavanie, vodiace ciary) - pamatane v prehliadaci.

export interface FaceView {
  /** Obrysy nevybranych prvkov. */
  outlines: boolean;
  /** Mriezka nad tabulou. */
  grid: boolean;
  /** Krok mriezky v bodoch tabule. */
  gridStep: number;
  /** Prichytavanie k mriezke. */
  snapGrid: boolean;
  /** Vodiace ciary a prichytavanie k prvkom a strukture tabule. */
  guides: boolean;
}

export const DEFAULT_VIEW: FaceView = { outlines: true, grid: false, gridStep: 1, snapGrid: false, guides: true };

/** Kroky mriezky v ponuke (8 = bunka znaku LCD). */
export const GRID_STEPS = [1, 2, 4, 5, 8, 10, 16];

const KEY = "tablesimulator.faces.view";

/** Ulozene volby; chybajuce alebo nepouzitelne = predvolene. */
export function loadView(): FaceView {
  try {
    const stored = JSON.parse(localStorage.getItem(KEY) ?? "null") as Partial<FaceView> | null;
    const view = { ...DEFAULT_VIEW, ...stored };
    return { ...view, gridStep: GRID_STEPS.includes(view.gridStep) ? view.gridStep : DEFAULT_VIEW.gridStep };
  } catch {
    return DEFAULT_VIEW;
  }
}

export function saveView(view: FaceView) {
  try {
    localStorage.setItem(KEY, JSON.stringify(view));
  } catch {
    // ulozisko prehliadaca nie je - volby plati len do zatvorenia stranky
  }
}
