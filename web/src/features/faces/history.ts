// Historia uprav predlohy: krok spat a znova. Tahanie v nahlade je jeden krok - prva zmena tahania ulozi krok,
// dalsie ho len prepisuju (transient).
import { useCallback, useState } from "react";

/** Najviac krokov spat. */
const LIMIT = 200;

interface State<T> {
  past: T[];
  present: T;
  future: T[];
}

export interface History<T> {
  value: T;
  canUndo: boolean;
  canRedo: boolean;
  /** Nova hodnota; transient = pokracovanie predosleho kroku (tahanie), nie novy krok. */
  set: (value: T, transient?: boolean) => void;
  undo: () => void;
  redo: () => void;
  /** Zacne odznova bez krokov (ina predloha, ulozenie, zahodenie). */
  reset: (value: T) => void;
}

export function useHistory<T>(initial: T): History<T> {
  const [state, setState] = useState<State<T>>({ past: [], present: initial, future: [] });
  const set = useCallback(
    (value: T, transient = false) =>
      setState((s) => (transient ? { ...s, present: value } : { past: [...s.past, s.present].slice(-LIMIT), present: value, future: [] })),
    [],
  );
  const undo = useCallback(
    () => setState((s) => (s.past.length ? { past: s.past.slice(0, -1), present: s.past[s.past.length - 1], future: [s.present, ...s.future] } : s)),
    [],
  );
  const redo = useCallback(
    () => setState((s) => (s.future.length ? { past: [...s.past, s.present], present: s.future[0], future: s.future.slice(1) } : s)),
    [],
  );
  const reset = useCallback((value: T) => setState({ past: [], present: value, future: [] }), []);
  return { value: state.present, canUndo: state.past.length > 0, canRedo: state.future.length > 0, set, undo, redo, reset };
}
