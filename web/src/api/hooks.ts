// Spolocne dotazy rozhrania: prihlaseny pouzivatel a zivy stav simulatora (linky, tabule, nastavenie).
import { useQuery } from "@tanstack/react-query";
import { getMeOptions, getStateOptions } from "./generated/@tanstack/react-query.gen";
import type { BoardDto, LineDto, MeDto, SettingsDto } from "./generated/types.gen";
import { can, type Permission } from "./permissions";

/** Prihlaseny pouzivatel a rezim prihlasenia; undefined = este sa nezistilo (alebo server nebezi). */
export function useMe(): MeDto | undefined {
  return useQuery(getMeOptions()).data;
}

/** Opravnenia prihlaseneho pouzivatela pre komponenty. */
export function useCan(): (permission: Permission) => boolean {
  const me = useMe();
  return (permission) => can(me, permission);
}

export interface SimulatorState {
  /** Stav sa uz nacital. */
  loaded: boolean;
  lines: LineDto[];
  boards: BoardDto[];
  supportedClasses: number[];
  /** Vyrobcovia tabul podla triedy linky (kluc = trieda); prvy je predvoleny. */
  manufacturers: Record<string, string[]>;
  settings: SettingsDto | null;
}

/** Zivy stav simulatora - prve nacitanie cez /api/state, zmeny prinasa WebSocket (live.ts). */
export function useSimulatorState(): SimulatorState {
  const { data } = useQuery(getStateOptions());
  return {
    loaded: data !== undefined,
    lines: data?.lines ?? [],
    boards: data?.boards ?? [],
    supportedClasses: data?.supportedClasses ?? [],
    manufacturers: data?.manufacturers ?? {},
    settings: data?.settings ?? null,
  };
}

/** Vyrobcovia tabul na linke danej triedy. */
export function manufacturersFor(state: SimulatorState, tableClass: number | undefined): string[] {
  return (tableClass !== undefined && state.manufacturers[String(tableClass)]) || [];
}
