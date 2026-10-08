// Aktualny cas, obnovovany po intervale (hodiny, striedanie a blikanie textov, beziaca dlzka).
import { useEffect, useState } from "react";

export function useNow(interval: number): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), interval);
    return () => clearInterval(timer);
  }, [interval]);
  return now;
}
