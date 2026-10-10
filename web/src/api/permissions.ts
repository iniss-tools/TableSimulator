// Opravnenia a vstavane roly (rovnake kluce ako politiky a roly servera - TableSimulator.Application.Auth).
import type { MeDto } from "./generated/types.gen";

export const PERMISSIONS = {
  view: "simulator.view",
  log: "log.view",
  archive: "archive.view",
  statistics: "statistics.view",
  audit: "audit.view",
  lines: "lines.manage",
  boards: "boards.control",
  scenarios: "scenarios.manage",
  designs: "designs.manage",
  recordings: "recordings.manage",
  virtual: "virtual.manage",
  signage: "signage.manage",
  settings: "settings.manage",
  server: "server.manage",
  users: "users.manage",
  keys: "keys.manage",
} as const;

export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];

/** Vsetky opravnenia v poradi servera. */
export const ALL_PERMISSIONS: Permission[] = Object.values(PERMISSIONS);

/** Vstavane roly od najvyssej - nazov im dava rozhranie podla jazyka. */
export const BUILT_IN_ROLES = ["admin", "operator", "viewer"] as const;

export type BuiltInRole = (typeof BUILT_IN_ROLES)[number];

export function isBuiltInRole(name: string | null | undefined): name is BuiltInRole {
  return BUILT_IN_ROLES.includes(name as BuiltInRole);
}

/** Ci prihlaseny pouzivatel ma opravnenie (pri zozname aspon jedno z nich). */
export function can(me: MeDto | null | undefined, permission: Permission | readonly Permission[]): boolean {
  const own = me?.permissions ?? [];
  return typeof permission === "string" ? own.includes(permission) : permission.some((p) => own.includes(p));
}
