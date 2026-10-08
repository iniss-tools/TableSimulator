// Spolocne pre stranku pouzivatelov a jej okna.
import { getUsersQueryKey, queryClient } from "@/api";

/** Zoznam pouzivatelov sa po zmene nacita znova. */
export const refreshUsers = () => queryClient.invalidateQueries({ queryKey: getUsersQueryKey() });
