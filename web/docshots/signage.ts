// Informacne panely ukazkovej stanice: spravy (tabula odchodov, texty, tabula simulatora), playlisty na den a rannu
// spicku, skupiny a panely. Panel 3 hra stale tu istu spravu (predvolena sprava skupiny) - snimka obrazovky panelu
// tak nezavisi od casu behu.
import type { APIRequestContext } from "@playwright/test";
import type { SetSignageGroupRequest, SetSignageMessageRequest, SetSignagePanelRequest, SetSignagePlaylistRequest } from "../src/api/generated/types.gen";

const message = (m: Partial<SetSignageMessageRequest> & Pick<SetSignageMessageRequest, "name" | "kind" | "duration">): SetSignageMessageRequest => ({
  note: "",
  virtualBoard: null,
  board: null,
  paragraphs: [],
  background: "#000000",
  media: null,
  url: null,
  ...m,
});

const MESSAGES: SetSignageMessageRequest[] = [
  message({ name: "Odchody – hala", kind: "virtual", duration: 30, virtualBoard: 1 }),
  message({
    name: "Výluka Dolné Mesto – Horná Ves",
    kind: "text",
    duration: 15,
    background: "#0b1f4a",
    note: "Platí do 16. 10.",
    paragraphs: [
      { text: "Výluka", color: "#ffcc00", size: "large", align: "center" },
      {
        text: "Od 12. 10. do 16. 10. jazdia vlaky medzi stanicami Dolné Mesto a Horná Ves náhradnou autobusovou dopravou.",
        color: "#ffffff",
        size: "medium",
        align: "center",
      },
      { text: "Autobusy odchádzajú zo zastávky pred staničnou budovou.", color: "#c8d4f0", size: "small", align: "center" },
    ],
  }),
  message({
    name: "Vitajte",
    kind: "text",
    duration: 10,
    background: "#102a1c",
    paragraphs: [
      { text: "Vitajte v stanici Dolné Mesto", color: "#ffffff", size: "large", align: "center" },
      { text: "Pokladne sú otvorené denne od 5:00 do 22:00.", color: "#b8e0c8", size: "medium", align: "center" },
    ],
  }),
  message({ name: "Nástupište 1", kind: "board", duration: 10, board: "2-3" }),
];

const PLAYLISTS: SetSignagePlaylistRequest[] = [
  {
    name: "Denný program",
    period: "daily",
    start: "2026-01-01",
    end: null,
    weekdays: [],
    from: 5 * 60,
    to: 22 * 60,
    items: [
      { message: 1, duration: null },
      { message: 2, duration: null },
      { message: 3, duration: null },
    ],
  },
  {
    name: "Ranná špička",
    period: "weekly",
    start: "2026-01-01",
    end: null,
    weekdays: [1, 2, 3, 4, 5],
    from: 6 * 60,
    to: 9 * 60,
    items: [
      { message: 1, duration: 40 },
      { message: 4, duration: null },
      { message: 2, duration: null },
    ],
  },
  { name: "Noc", period: "daily", start: "2026-01-01", end: null, weekdays: [], from: 0, to: 0, items: [{ message: 3, duration: 30 }] },
];

const GROUPS: SetSignageGroupRequest[] = [
  { name: "Hala", note: "Monitory v hale", playlists: [1, 2], defaultPlaylist: 3, defaultMessage: null },
  { name: "Podchod", note: "Počas výluky len oznam", playlists: [], defaultPlaylist: null, defaultMessage: 2 },
];

const PANELS: SetSignagePanelRequest[] = [
  { name: "Hala – pokladne", note: "Televízor nad pokladňami", group: 1 },
  { name: "Hala – vchod", note: "Monitor pri hlavnom vchode", group: 1 },
  { name: "Podchod", note: "Monitor pri výstupe na nástupištia", group: 2 },
  { name: "Rezerva", note: "", group: null },
];

/** Zalozi spravy, playlisty, skupiny a panely v tomto poradi (cisla od 1 ako v odkazoch vyssie). */
export async function seedSignage(api: APIRequestContext) {
  for (const [kind, items] of [
    ["messages", MESSAGES],
    ["playlists", PLAYLISTS],
    ["groups", GROUPS],
    ["panels", PANELS],
  ] as const)
    for (const item of items) {
      const response = await api.post(`/api/signage/${kind}`, { data: item });
      if (!response.ok()) throw new Error(`Panely (${kind}): ${await response.text()}`);
    }
}
