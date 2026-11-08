# TableSimulator – pokyny pre agentov

Simulátor tabúľ INISS (informačný systém železničných staníc) bez desktopovej aplikácie – ovládanie aj zobrazenie
na webe. Repozitár je súčasťou pracovného priečinka `INISSTools` (nie je git repozitár, každý podpriečinok má vlastné
repo); `ToolsCore` musí byť naklonovaný vedľa (`..\ToolsCore`, odkazujú sa `ToolsCore.Iniss` a `ToolsCore.Iniss2`).
GVDEditor odkazuje `TableSimulator.Contracts`. Komunikácia, texty UI aj komentáre v kóde (bez diakritiky) sú slovenské.

## Projekty

Menný priestor = cesta priečinka (`TableSimulator.Core/Boards/AkSignal` → `TableSimulator.Core.Boards.AkSignal`).

### TableSimulator.Core

`net10.0` bez UI a Windows. Všetko beží pod zámkom `Simulation/Simulator`, udalosti (`Simulation/Events`) idú
von mimo zámku.

- `Transport/` – linky: TCP/UDP server, klient rúry (rúru vytvára INISS), COM a zrkadlo (System.IO.Ports);
  `TransportManager.SyncAsync` spúšťa a zastavuje linky podľa nastavenia.
- `Boards/` – profil tabule `BoardProfile` a obsah podľa technológie: `Elen/` (riadky odspodu), `Chars/` (znaky zhora
  vo vrstvách, písma LCD1/ERP podľa čísla), `Flaps/` (kódy listov a otáčanie, text listu `FlapLabels`),
  `AmplifierDisplay` (zosilňovače ELSVO a DTR), predloha čela `Faces/`.
  - Zariadenia, ktoré nie sú tabule: ústredne `AkSignal/`, rozhlas vzdialeného INISSu `RemoteAudio/`, ich spoločné
    hlásenia `Announcements/` (`IAnnouncementDisplay`, fronta a okruh v `Announcement`) a história `History/`.
  - Obsah meniaci sa s časom = `ITimedDisplay`, posúva ho `Simulator.Tick` (server každú sekundu).
  - Prekrytie zo scenára (`AkSignalOverride` v `SimBoard.Override` cez `Simulator.SetOverride`) sa neukladá. Protokoly,
    tik, história aj `DisplayDto` idú cez `SimBoard.EffectiveProfile`, `Profile` ostáva nastavením.
- `Protocols/` – skladanie správ a odpovede podľa `docs/iniss/protokoly`. Nová rodina = záznam
  v `Simulator.Protocols` s výrobcami triedy, poruchy cez `BoardFaults.Apply` a `ILineProtocol.FaultReply`.
  - `Elen/`, `Elektrocas/` (LCD, LCD1, ERP, listové FERS, obal ITP), `Ers/` (listové ERS, vlastný protokol),
    `Apel/` (APEL a APEL_AN: rámec `DLE STX…DLE ETX`, viac záznamov v správe, XOR bez adresy).
  - `Elsvo/` – spínacie jednotky zosilňovačov (trieda 129, rodina `amplifier`).
  - `Dtr/` – zosilňovač spínaný signálom DTR INISSu 3.39: trieda 900 len simulátora (`Simulator.ModemSignalClass`),
    len COM – `LineHost` sleduje DSR/DCD a volá `Simulator.SetModemSignal`; zariadenie `DTR` na adrese 0.
  - `Remote/` – vzdialený INISS (trieda 128, simulátor hrá INISS s `/Remote`): obal `S`/`0xAC` s `RemoteCrc`.
    `TCMD` bez súčtu doplní a pošle vnútornému protokolu rodiny podľa `TCLS` (kód výrobcu), `TSTA` = stav tabule.
    Na jednej linke sú tabule rôznych rodín – obsah podľa výrobcu `ILineProtocol.CreateDisplay(manufacturer)`,
    do zrkadla idú vybalené správy (`MirrorsInnerFrames` + `ILineContext.Forward`).
  - Fronty hlásení `RSTA` drží zariadenie `REMOTE_AUDIO` na adrese 0 (`ILineContext.Device` ho založí aj bez
    samoučenia). `RWAV` = trojice `jazyk/skupina/zvuk,pauza pred,pauza po`; hlásenie hrá podľa dĺžky zvuku
    (`SimEnvironment.SoundLength`, inak 1,5 s) a kým hrá, `RSTA` má `0x100`. Prerušenie
    `Simulator.BreakAnnouncement` pošle raz `RSTA` 0 (riadiaci zapíše `remote break`). Porucha `noReply` = vzdialený
    INISS mlčí (`remote timeout`), `delay` oneskorí odpoveď.
  - `AkSignal/` – ústredne AK Signal servera INISS2 (svetlo a rozhlas): trieda 131 binárny `AKSIGNAL`, 132 textový
    `AKSIGNALUH`, jedna ústredňa na linke (v simulátore adresa 0). Automatiku a ručné svietenie zapína server, tmu
    hlási snímač (`SunTimes` podľa `SimEnvironment.Location`), rozhlas UH má kanály 1 a 2. Testy cez náhradu
    servera `TableSimulator.Tests/Protocols/AkSignal/IniSrvAkSignal`.
- `Recording/` – záznam `.tsrec` a `ReplayPlayer` (pauza, rýchlosť a posun cez príkazy pod zámkom a prebudenie
  slučky). Posun = `Simulator.FastForward`: zmaže tabule liniek záznamu a prehrá správy pred miestom naprázdno bez
  logu, štatistiky a počítadiel. Po dohraní prehrávač čaká, končí len zastavením. Porovnanie záznamov
  `RecordingTimeline` (prehratie naprázdno do vlastného simulátora, zmeny do 1 s = jeden stav) a `TimelineDiff`.
- `Archive/` – denné archívy TAB/REP (formát v docs `formaty-suborov/archiv.mdx`); `ReportTail` číta len pribudnuté
  celé riadky, iný súbor alebo prepísaný začiatok = od začiatku.
- `Virtual/` – XML tabúľ INISSu pre virtuálne tabule: `XmlTableFolders` (novší z rovnomenných), `XmlTableFilter`
  (riadky a oddeľovač ako INISSView), `InissViewConfig` (`Web.config` INISSView), `XslTablePages` (ako `Tabule.aspx`).
- `Statistics/` – `TrafficCollector` pod zámkom simulátora, `Simulator.TakeTraffic(minute)`; minúta =
  `TrafficMinutes.Of` od 1970 UTC.
- `Scenarios/` – scenáre porúch, čistá `ScenarioTimeline` (kroky v cykle a ich účinky). Výpadok napájania
  `Simulator.SetPower` (`FindBoard` vráti null), výpadok linky `SetLineOutage` (stav `outage`, linka sa nespustí).
- `Monitor/` – súbory ELEN Monitora.

### TableSimulator.Contracts

DTO API a WebSocketu bez logiky a závislostí, jedno v súbore, po oblastiach. Enumy s `JsonStringEnumMemberName`
a vlastným `JsonConverter`, správy `/ws` polymorfné cez `type`. Používa ich aj GVDEditor.

### TableSimulator.Application

- CQRS bez mediátora podľa vzoru Milana Jovanovića: `ICommand`/`IQuery<T>` + handlery po oblastiach, `Result`/`Error`
  s kódom chyby, registrácia skenovaním (Scrutor). Príkaz alebo dotaz môže byť v jednom súbore so svojím validátorom
  a handlerom, iné typy (chyby, pravidlá, porty) sú v samostatných súboroch.
- Dekorátory: overenie (FluentValidation, kódy cez `WithErrorCode`, popisy anglicky), audit (každý príkaz do DB – cieľ
  cez `IAuditable`, heslá `***`, veľké obsahy `[property: JsonIgnore]`) a záznam.
- `Mapping/` – `DtoMapper`/`RequestMapper` jadro ↔ kontrakty. Porty pre server: `ISettingsStore`, `ILiveNotifier`,
  `IAuthService`, `IServerBackup`, `IDatabaseSettings`…
- `Auth/` – oprávnenia podľa činností `Permissions` (kľúč = politika, nemeniť; `Policies.AnyOf` = aspoň jedno z)
  a roly v DB (`AppRole.Permissions`; vstavané `Roles` – správca vždy všetko, operátor a pozorovateľ upraviteľné).
  `RoleCatalog` v pamäti (načíta `DatabaseInitializer`, po zmene `RoleService`), autorizácia `PermissionHandler`
  podľa roly v prihlásení. Nikto nepridelí ani nespravuje viac, než sám má (`RoleCatalog.Covers`).
  - Nový endpoint = vhodné oprávnenie. Nové oprávnenie = `Permissions.All`, predvolené v `Roles.DefaultsOf`, na webe
    `PERMISSIONS` + skupina v `features/roles/roles.ts` + texty `permission.*`.
- `Alerts/` – oznámenia o výpadkoch liniek: kedy (`LineAlertTracker`, čisté), texty SK/CZ (`AlertMessage`), kanál
  `IAlertChannel`. Ticho servera INISS2 (`ServerSilent`) ide ako stav linky `silent` (`AlertLines`).
- `Devices/` – `DeviceClock` (dĺžky zvukov z banky programu v nastavení) a história ústrední
  `GetDeviceHistoryQuery` (stav na začiatku obdobia; log svetiel servera INISS2 priradený ústredni podľa názvu
  svetla). Snímač a stavy ústredne mení `Boards/SetAkSignalCommand` cez `Simulator.UpdateProfile` (svetlá a audio
  linky ostanú).
  - `Sounds/` – banka zvukov `ISoundBank`: server INISS2 `SoundLengths` (súbor podľa mena, dĺžka z hlavičky EWA),
    INISS 3.39 `Iniss339SoundLengths` (kľúče z `FYZBANK.DAT`/`FYZZVUK.DAT` vedľa programu). `SoundBanks` drží banku
    programu v nastavení pre `DeviceClock` aj `GET /api/sounds` (nahrávka ako WAV na prehratie v prehliadači, len
    súbory v banke, skrátený blok dát opraví `WavHeader`).
- `Scenarios/ScenarioRunner` – zlúči účinky bežiacich scenárov a mení len rozdiely, pôvodné poruchy vráti.
- `Signage/` – informačné panely ako INISSView 2 (len Application, Persistence a web): čistý rozvrh
  `SignageSchedule`, správy, playlisty a skupiny v DB ako JSON požiadavky, `SignagePanelTracker` v pamäti, súbory
  `SignageMediaStore` v `media\`. Záloha `SignageBackup` (ZIP `signage.json` + `media/`), obnova nahradí s pôvodnými
  číslami alebo pridá s prečíslovaním (`ISignageRepository.ImportAsync` v transakcii).
- `Station/` – stanica z programu INISSu v nastavení (`InissStations`, import cez `StationImporter`; novým tabuliam
  čelo z údajov stanice `StationFaces`, rovnaké čelá zdieľajú predlohu `Faces/KnownFaces`). Do .INI INISSu nezapisuje.
  - Server INISS2 spozná `Iniss2Installation.Find`. `Iniss2Stations`: linky z `CommLines.xml` (číslo podľa poradia),
    tabule z `Tables.xml`/`TCatalog.xml` aj s čelom (stĺpce Elektročasu a FERS v znakoch, ELEN v bodoch, obsah podľa
    `CatalogColumn.Section`), ústredne AK Signal zo `Lights.xml` a `Audio.xml` (`Iniss2Devices` – linka a adresa
    podľa pravidiel servera, chyby v `InissStationDto.DeviceProblems`).
  - Nové načítanie prepíše svetlá a audio linky ústrední, snímač a stavy ostanú.
  - Presmerovanie liniek INISS2 prepíše `line` v `CommLines.xml` (`CommLinesXml.Write`, sériovej linke aj `typ`
    na `TCP_CLIENT`, pôvodné hodnoty v komentári); platí po reštarte servera. Tlačidlo na stránke Linky sa riadi
    `GetInissRedirectQuery` – vždy úspech, aby stránka bez INISSu nedostala chybu.
- `Virtual/` – virtuálne tabule, šablóny XSL INISSView (`InissViewSite`), vstavané rozloženia `Layouts/*.json`
  vložené do zostavy (vlastné v DB).
- `Designs/DesignDefaults` = dvojča `defaultDesignId` vo `web/src/render/design.ts` – meniť oboje.

### TableSimulator.Persistence

- EF Core + ASP.NET Core Identity: abstraktný `AppDbContext`, poskytovateľ = podtrieda s vlastnými migráciami –
  `SqliteAppDbContext` (predvolený, `Migrations/Sqlite`) a `PostgresAppDbContext` (časy len UTC,
  `Migrations/Postgres`). Nová migrácia vždy pre oba (príkazy v README).
- Entity sú aj typy z Application (záznamy signage, štatistiky, scenárov, histórie zariadení) – ich menný priestor
  je v snímke modelu, nepresúvať ich.
- `DatabaseConnection` (poskytovateľ + pripojenie, `Description` bez hesla), `DatabaseCopier` (prenos všetkých údajov
  medzi databázami; záloha je vždy súbor SQLite), `DatabaseInitializer` pri štarte migruje a zakladá roly,
  `ServerDatabase` otvorí DB aj mimo DI.
- Linky a tabule `SimulatorConfigRepository` (profil tabule ako JSON stĺpec, enumy kľúčmi zo `SimulatorConfigJson`).
- Testy PostgreSQL bežia len s premennou `TABLESIMULATOR_TEST_POSTGRES`.

### Server TableSimulator

ASP.NET Core cez `FrameworkReference`, nie Web SDK (`wwwroot` sa kopíruje do výstupu).

- `Api/<oblasť>/` – jeden `IEndpoint` na súbor: len príkaz/dotaz a `Match(…, CustomResults.Problem)` (ProblemDetails
  s `code` a `errors`). Typ odpovede `.Produces<T>()`/`.Produces(204)` (zhodu s handlerom stráži `OpenApiTests`)
  a povinne `.RequireAuthorization(Permissions.X)` alebo `.AllowAnonymous()` (stráži `AuthApiTests`). Názov
  operácie OpenAPI = názov triedy, oblasť = priečinok. Dokument `/api/openapi/v1.json`
  (`Infrastructure/OpenApi/ApiDocument` – všetky vlastnosti povinné, čísla bez textu, chyba `ApiProblem`).
- `Infrastructure/Auth/` – cookie Identity; bez prihlásenia (len localhost, `RequireLogin` vypnuté) je používateľ
  správca `local`. Prístupové kľúče (`Bearer` alebo `access_token` na `/ws`), schéma `Auto` vyberá kľúč, cookie
  alebo `Local`. Zmeny v `/api` s cookie len s hlavičkou `X-Requested-With`; `LoginThrottle`, `AdminReset`
  (`--reset-admin`), kľúče ochrany dát DPAPI pre počítač.
- `Infrastructure/Https/` – Kestrel podľa `ServerEndpoints` (HTTP vždy, HTTPS na vlastnom porte), vlastná
  certifikačná autorita `HttpsCertificates` v `https\` priečinka nastavení; tajné hodnoty cez `ISecretProtector`.
- `Infrastructure/Backup/ServerBackup` – záloha celého simulátora (ZIP: `backup.json`, online kópia SQLite, vlastné
  vzhľady, `media`, voliteľne záznamy). Obnova sa len pripraví do `restore.pending`, použije ju `ApplyPendingAsync`
  v `Program` pred `DatabaseBootstrap` (pôvodné do `before-restore\<čas>`).
- `Infrastructure/Hosting/` – služba Windows (`WindowsService`, `--install-service`, reštart z webu = koniec
  s kódom 3), jeden server nad priečinkom nastavení (`ServerLock`; ďalšie spustenie len otvorí bežiaci server),
  dôvod neúspešného štartu `StartupError`, odtlačok servera v hlavičke `X-TableSimulator-Server` (`ServerIdentity`),
  služby na pozadí:

  | Služba | Perióda | Čo robí |
  |---|---|---|
  | `DeviceClockService` | 1 s | `Simulator.Tick` – ústredne a rozhlas |
  | `DeviceHistoryService` | 5 s | história zariadení do DB (`Simulator.TakeDeviceHistory`) |
  | `StatisticsService` | 1 min | štatistika komunikácie do DB, mazanie po 30 dňoch |
  | `ScenarioHostedService` | 200 ms | tik scenárov, pri ukončení ich zastaví pred uložením tabúľ |
  | `ReportMonitorService` | 2 s | monitor hlásení (správa `reports`) |
  | `UpdateCheckService` | 12 h | kontrola aktualizácií (nastavenie `CheckUpdates`) |
  | `Alerts/LineAlertService` | 2 s | oznámenia o výpadkoch (webhook, e-mail) |

- `Infrastructure/Persistence/` – `DatabaseBootstrap` (pred zostavením servera: záloha SQLite do `before-update\`,
  migrácie, prevod starých `settings.json`/`config.json`), výber databázy `DatabaseConfig` (`database.json` mimo
  databázy, heslo DPAPI, zmena po reštarte), `SimulatorConfigStore` (linky a tabule do DB s odkladom).
- Ďalšie: Serilog `Infrastructure/Logging/SerilogSetup` (konzola + denné `logs`; sem idú aj záznamy `ToolsCore.Iniss`),
  WebSocket `/ws` `Realtime/LiveHub`, stránky INISSView bez prihlásenia `Hosting/InissViewEndpoints`.
- `POST /api/station` volá GVDEditor pri presmerovaní liniek INISSu (`SimulatorRedirect` v ToolsCore.Iniss).

### Server MCP (`/mcp`)

- Prístup: `Infrastructure/Mcp/McpSetup` – prístupový kľúč alebo token OAuth (schémy `AccessKey`, `OAuthToken`,
  výzva `McpAuth`), aj bez prihlasovania; kontrola `Origin`, bez relácie – GET/DELETE `/mcp` = 405 (inak by ich
  zachytil web a vrátil 401 bez `resource_metadata`).
- Server OAuth `Infrastructure/OAuth` – dynamická registrácia (návrat https, aplikačná schéma, http len loopback),
  súhlas na webe `#/authorize/{id}`, `/oauth/token` s PKCE S256 a `iss`. Žiadosti a kódy v pamäti
  (`Application/OAuth/OAuthPending`), klienti a povolenia v DB len s hashmi tokenov (prístupový 1 h, obnovovací
  30 dní s výmenou – použitie vymeneného = zrušenie). VS Code sa prihlási len cez HTTPS simulátora.
- Nástroje `Mcp/<Oblasť>Tools.cs` – statické metódy nad handlermi ako endpointy, výsledky `Mcp/Results/`
  (stručné, text tabule `BoardText`, nie kódy; skracovanie v `McpMapper`):
  - povinne `[Authorize(Policy = Permissions.X)]` (nedostupný nástroj klient nevidí), názov snake_case, popisy
    anglicky aj pri parametroch a vlastnostiach výsledku, explicitne `ReadOnly`/`OpenWorld` (pri zmene aj
    `Destructive`/`Idempotent`), `UseStructuredContent`;
  - chyba `ValueOrThrow` = `kód: popis`, limity `McpResults.CheckLimit`; schémy upravuje `McpSetup` (`TimeOnly`
    a `DateTime` bez `format`, null sa zapisuje);
  - zmeny cez existujúce príkazy (audit `key:<názov>`), `set_*` menia len zadané hodnoty (`McpRequests`),
    `McpRequests.Patch<T>` – null = predvolená hodnota, prázdne reťazce sú platné, polia sa nahrádzajú celé;
  - žiadny prázdny reťazec ako príznak v parametroch (model ho posiela ako neplatný JSON) – radšej `bool`;
  - `send_frame` len na linke bez pripojeného INISSu, napájanie a výpadok linky len scenárom;
  - `set_board_look` mení profil JSON merge patchom a vracia `designInUse` a `viewUrl`.
- MCP Apps (experimentálne MCPEXP003) – `show_board` a `show_virtual_board` s `[McpAppUi]`, jeden
  `wwwroot/mcp-app/app.html` z webu (`web/src/mcp-app`, `vite.mcp.config.ts` – všetko vložené). Rámec nesmie na
  sieť – údaje len cez nástroje `board_view` a `virtual_view` (Visibility App). VS Code posiela výsledok nástroja
  len ako text (aplikácia štartuje z `ui/notifications/tool-input`); rámec bez posuvníka a výška len pri zmene.
  Claude Code MCP Apps nezobrazí.
- Testy: `McpToolsTests` (pravidlá a snímka `TableSimulator.Tests/Mcp/mcp-tools.json`) a `McpToolsServerTests`
  (každý nástroj nad stanicou s údajmi, kontrola voči schéme) – nový nástroj pridať do oboch. Docs
  `referencia/mcp.mdx`.

## Web (`web/`)

React + TS 7, bun/Vite; texty SK/CZ v `src/i18n` – žiadny text UI natvrdo, chyby API podľa kódu `error.<kód>`.

- API: klient generuje @hey-api/openapi-ts zo snímky `web/openapi.json` do `src/api/generated` (ručne nemeniť;
  generátor má vlastný balík `web/tools/api` s TS 6). Dáta cez generované `…Options()`/`…Mutation()`, nie vlastný
  `fetch`; cache TanStack Query (`api/query.ts`), WebSocket mení cache (`api/live.ts`), `api/hooks.ts` (`useMe`,
  `useCan`, `useSimulatorState`).
- Renderer `src/render` (z API len typy a `api/auth`): `BoardView` vyberá kreslenie podľa rodiny, listové tabule
  na plátne s modulmi (`render/flaps.ts`). Bodové písma misc-fixed `pixel-fonts.data.ts` generuje
  `tools/pixel-fonts/bdf2ts.py` – ručne nemeniť. Snímka tabule = `BoardView` `ref` → `snapshot()`.
- Rám `src/app`: TanStack Router s adresami za `#` (`router.tsx` – adresy nemeniť, sú v odkazoch zariadení
  `?key=…#/virtual/N`; premenovaná stránka dostane presmerovanie zo starej adresy – úvodná stránka Zariadenia
  `#/devices`, `#/` a `#/boards` na ňu presmerujú), menu `navigation.ts` (stránku bez oprávnenia rám nezobrazí), kiosky `app/kiosk.ts`, service
  worker `public/sw.js` (len stránka a `/assets`), upozornenia na výpadky `app/line-alerts.ts` + `app/notifications.ts`.
  Zvuk hlásení `app/sound` (Web Audio, predvolene stlmený, hrá len hlásenia začaté po načítaní stránky a zapnutí
  zvuku, dohraté nezastavuje; čistá logika v `announcement-sound.ts`).
  Iný server na tej istej adrese = nové načítanie (`api/server-identity.ts`). Nové rozhranie na tom istom serveri
  (zostava, aktualizácia) zistí `app/use-web-version.ts` po každom pripojení podľa vstupného skriptu stránky – rám
  ukáže pruh, kiosk (`useKiosk`) sa načíta znova sám, raz pre každú verziu.
- Stránky po oblastiach `src/features/<oblasť>/` (stránka, dialógy, pomocné funkcie). Spoločné súčasti
  `src/components/common` (`FormDialog`, `OptionSelect`, `confirmAction` – nie `window.confirm`, `RowActions`…),
  tabuľky `src/components/data-table` (`columnsFor<T>()`; hodnotu z iného dotazu vložiť do dát riadkov – tabuľka si
  `accessor` pamätá, kým sa nezmenia dáta), dlhý zoznam virtualizovať (`@tanstack/react-virtual`).
- Rozhranie shadcn/ui (Base UI, preset nova, ikony lucide) v `src/components/ui` – kód je náš, texty cez `t()`;
  Tailwind v4, tokeny v `src/index.css`; CSS modul len pre vzhľad určený dátami.
- Editor čiel `features/faces` – čisté moduly `faces/*.ts` (úpravy, história, prichytávanie, zarovnanie, meranie),
  príkazy z klávesov aj ponuky cez `faces/actions.ts`. Klávesy stránky `DocumentKeys` (posluchač s aktuálnou
  obsluhou, nie efekt pred skorým návratom); ťahanie za popis poľa `NumberField` = jeden krok histórie.
- Kvalita: ESLint (`tools/lint` – vlastný balík s TS 6, typescript-eslint nevie TS 7) a Prettier (šírka 160,
  `endOfLine auto` kvôli CRLF) – po zmene `bun run lint` a `bun run format`, obe bez nálezu. Sľub bez čakania
  označiť `void`; modul s komponentom neexportuje nič iné (Fast Refresh – pomocníci do `.ts` vedľa); čas počas
  vykresľovania cez `hooks/use-now`, nie `Date.now()`.
- Koreňový `.gitignore` (šablóna VS) ignoruje priečinky `log`, `bin`, `obj`, `debug`, `release`… – tak nepomenúvať
  priečinky vo `web/src`. Web zostaví cieľ `BuildWeb` v `TableSimulator.csproj` (zdroje `WebSource` sa zbierajú až
  v cieli, inak by ich Rider ukazoval v koreni projektu).

### Snímky do dokumentácie a e2e

- `docshots/*.shots.ts` (`bun run docshots`): server `e2e/server.mjs docshots` na porte 5495 (HTTPS 5496)
  s ukážkovou stanicou `docshots/demo.ts` (linky 47000+N, ak sú voľné, inak 48000+N – snímka aj tak ukáže
  47000+N, `defaultPorts` v `shoot.ts`). Písma ELEN len z premennej `DOCSHOTS_FONTS`
  (žiadne absolútne cesty v kóde); údaje do SQLite priamo (`docshots/statistics.ts`, `docshots/device-history.ts`),
  logy INISSu `docshots/iniss-log.ts`, ukážkový server INISS2 `docshots/iniss2-install.ts`. Cesty, meno a adresy
  počítača nahradí `mask()` v `shoot.ts`. Výstup do
  `iniss-tools-docs/static/img/tablesimulator/<článok>/<snímka>-light|dark.png`.
- Testy prehliadača `e2e/*.e2e.ts` (nie `*.spec.ts` – tie spustí aj bun test): texty podľa SK rozhrania, výber cez
  role a popisy, spoločný server – testy po jednom, každý si zakladá vlastné linky. Správy ako INISS cez
  `e2e/helpers.ts` – trvalé spojenie `openLine` (odpojenie vyvolá upozornenie na výpadok, ktoré prekryje tlačidlá).
- Beh ~13 min a `playwright_chromiumdev_profile-*` v `%TEMP%` = ESET „Ochrana bankovníctva a platieb“ blokuje
  dočasný profil Edge – vypnúť ju.

## Zásady

- Jadro určuje, čo tabuľa ukazuje (vrátane striedania a pohyblivého textu), prehliadač, ako vyzerá. Profil tabule,
  vzhľad (`designs/<id>/design.json`, editor `features/designs`), predloha čela a rozloženie virtuálnej tabule sú
  dáta, nie kód.
- Údaje servera do databázy (Persistence, migrácia pre oboch poskytovateľov), nie do súborov JSON. Ako súbory
  ostávajú len záznamy `.tsrec`, vlastné vzhľady, certifikáty, kľúče ochrany dát a logy; linky a tabule sa prenášajú
  exportom/importom (`/api/config`, tvar `SimulatorConfigJson`).
- Kľúče v JSON explicitne (`JsonStringEnumMemberName`), nie z názvov C#; vlastnosti kontraktov camelCase
  (`JsonSerializerDefaults.Web`) – tvar JSON stráži `ContractsJsonTests`.
- Logovanie: server cez `ILogger<T>` (Microsoft.Extensions.Logging nad Serilogom), statický `Serilog.Log` len
  v `Program` pred zostavením servera; pravidlá šablón a úrovní v `ToolsCore/AGENTS.md` (časť Logovanie).

## Príkazy

```bash
# build zostavi aj web (bun); -p:SkipWebBuild=true ho vynecha
dotnet test TableSimulator.Tests/TableSimulator.Tests.csproj
# renderer; bun run build = typecheck + wwwroot
cd web && bun test
# ESLint (balik tools/lint s TS 6) a Prettier; bun run format prepise
cd web && bun run lint && bun run format:check
# Playwright v nainstalovanom Edge proti 2 serverom s cistymi datami (e2e/.run)
cd web && bun run e2e
# nova snimka web/openapi.json; potom vo web/ bun run api
TABLESIMULATOR_UPDATE_OPENAPI=1 dotnet test TableSimulator.Tests/TableSimulator.Tests.csproj --filter OpenApiTests
# nova snimka nastrojov MCP (Mcp/mcp-tools.json)
TABLESIMULATOR_UPDATE_MCP=1 dotnet test TableSimulator.Tests/TableSimulator.Tests.csproj --filter McpToolsTests
# instalator: publish len win-x64 do prazdneho priecinka (sluzba Windows, firewall, data pri odinstalovani ostavaju)
dotnet publish TableSimulator/TableSimulator.csproj -c Release
"/c/Program Files/Inno Setup 7/ISCC.exe" TableSimulatorInstaller.iss
```

Vydanie: značka `vX.Y` spustí `.github/workflows/release.yml` (spoločný postup v `ToolsCore/Installer/README.md`).

## Spoločné pravidlá

- `Directory.Build.props`: `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `AnalysisLevel latest-recommended` –
  build bez jediného varovania; zámerne vypnuté pravidlá len tam (`NoWarn` s dôvodom), varovanie opraviť v kóde,
  výnimočné potlačenie priamo na nájdenom člene (`[SuppressMessage(..., Justification = ...)]`). Kopírovací
  konštruktor recordu s primárnym konštruktorom nespúšťa inicializátory polí – priradiť všetky členy.
- Jeden typ najvyššej úrovne na súbor (vnorené typy môžu ostať), viac súvisiacich súborov do vlastného priečinka.
  Výnimka: príkaz alebo dotaz CQRS môže byť v jednom súbore so svojím validátorom a handlerom.
- Kultúra: UI `CultureInfo.CurrentCulture`, formáty a logy `InvariantCulture`.
- Testy MSTest, metódy `Oblast_CoOveruje`; do repa len testy s vlastnými dátami (korpusové overenie nad
  `D:\INISSroot` len na stroji používateľa).
- Skripty: generátory ukladať ako súbor (bash heredoc mení `\\` a `\n`); v `.ps1` žiadna diakritika.

## Commity (Conventional Commits)

Commitovať len na požiadanie. Správy podľa [Conventional Commits](https://www.conventionalcommits.org/):
`<typ>(<oblasť>): <popis>`.

- Typ: `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `build` (projekty, balíky, inštalátor), `ci` (workflow),
  `style`, `chore`, `revert`.
- Oblasť nepovinná, malými písmenami (napr. `web`, `mcp`, `oauth`, `protocols`, `signage`, `persistence`, `e2e`).
- Popis anglicky v rozkazovacom spôsobe, malým písmenom, bez bodky, najviac 72 znakov; názvy tried v backtickoch.
  Telo po prázdnom riadku vysvetľuje prečo; nekompatibilná zmena (API, MCP nástroje, formát exportu, databáza bez
  migrácie) má `!` a pätičku `BREAKING CHANGE: …`.
- Príklad: ``fix(protocols): answer `TSTA` for powered-off boards``.
