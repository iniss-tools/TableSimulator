// Opravnenia a roly (rovnake nazvy ako politiky a roly servera - TableSimulator.Application.Auth).
import type { MeDto } from "./generated/types.gen";

export const PERMISSIONS = {
  view: "simulator.view",
  log: "log.view",
  edit: "simulator.edit",
  recordings: "recordings.manage",
  settings: "settings.manage",
  users: "users.manage",
  audit: "audit.view",
} as const;

export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];

export type Role = "admin" | "operator" | "viewer";

/** Roly od najvyssej. */
export const ROLES: Role[] = ["admin", "operator", "viewer"];

/** Ci prihlaseny pouzivatel ma opravnenie. */
export function can(me: MeDto | null | undefined, permission: Permission): boolean {
  return me?.permissions.includes(permission) ?? false;
}
