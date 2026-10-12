// Mierka platna editora ciel a klavesove skratky mimo poli formulara.

/** Najmensia a najvacsia mierka (CSS px na px prirodzenej velkosti tabule). */
export const MIN_ZOOM = 0.1;
export const MAX_ZOOM = 8;

/** Kroky mierky v percentach - priblizenie a oddialenie skace na najblizsi. */
const STEPS = [10, 25, 33, 50, 67, 75, 100, 125, 150, 200, 300, 400, 600, 800];

/** Mierka o krok vyssie (direction 1) alebo nizsie (-1) od aktualnej. */
export function zoomStep(current: number, direction: 1 | -1): number {
  const percent = current * 100;
  const next = direction > 0 ? STEPS.find((s) => s > percent + 0.5) : [...STEPS].reverse().find((s) => s < percent - 0.5);
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, (next ?? (direction > 0 ? STEPS[STEPS.length - 1] : STEPS[0])) / 100));
}

/** Klavesova skratka ide do pola formulara (tam patri jeho vlastne spat, medzernik a sipky), nie do editora. */
export function typing(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLElement &&
    (target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName) || target.getAttribute("role") === "combobox")
  );
}
