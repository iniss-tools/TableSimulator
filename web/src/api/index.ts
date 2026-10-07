// API servera pre rozhranie: generovany klient (typy DTO, funkcie, dotazy a mutacie TanStack Query) a rucne casti -
// interceptory a chyby (client.ts), zivy stav cez WebSocket (live.ts), opravnenia a spolocne dotazy.
// Renderer (src/render) odtialto neimportuje - len typy z ./generated a ./auth (bez i18n a prehliadaca).
export { ApiError, errorText } from "./client";
export * from "./generated";
export * from "./generated/@tanstack/react-query.gen";
export { accessKey, authHeaders, deviceLink, forgetAccessKey, socketUrl } from "./auth";
export { manufacturersFor, useCan, useMe, useSimulatorState, type SimulatorState } from "./hooks";
export { onDesignChanged, openSocket, useConnection } from "./live";
export type { LiveMessage } from "./messages";
export { can, PERMISSIONS, ROLES, type Permission, type Role } from "./permissions";
export { queryClient } from "./query";
