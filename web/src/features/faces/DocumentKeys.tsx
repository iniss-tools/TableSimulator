// Klavesy na celej stranke editora: posluchac na dokumente vola vzdy aktualnu obsluhu (tu z posledneho vykreslenia).
import { useEffect, useRef } from "react";

export function DocumentKeys({ onKeyDown }: { onKeyDown: (e: KeyboardEvent) => void }) {
  const handler = useRef(onKeyDown);
  useEffect(() => {
    handler.current = onKeyDown;
  });
  useEffect(() => {
    const listener = (e: KeyboardEvent) => handler.current(e);
    document.addEventListener("keydown", listener);
    return () => document.removeEventListener("keydown", listener);
  }, []);
  return null;
}
