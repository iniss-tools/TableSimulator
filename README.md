# TableSimulator

Simulátor informačných tabúľ pre INISS 3.39. INISS posiela obsah tabúľ po svojich linkách (sekcie `Driver*`)
a simulátor sa tvári ako tabule na druhom konci linky – odpovedá bajtovým protokolom výrobcu a obsah tabúľ
zobrazuje vo webovom prehliadači. Navyše ukazuje XML tabúľ a archívy INISSu ako INISSView, hrá informačné panely
ako INISSView 2, nahráva a porovnáva komunikáciu a predstiera poruchy.

> Nezávislý projekt, nie je spojený so spoločnosťou CHAPS ani s výrobcami tabúľ a nie je nimi podporovaný. INISS je
> produkt spoločnosti CHAPS. Projekt neobsahuje žiadnu časť programu INISS ani jeho dáta; protokoly tabúľ a formáty
> súborov INISSu boli zistené kvôli interoperabilite.

**Používateľská dokumentácia** je v `iniss-tools-docs/docs/tablesimulator/` (sekcia *Simulátor tabúľ*), protokoly
tabúľ v `iniss-tools-docs/docs/iniss/protokoly/`. Tento súbor je pre vývoj.

| Téma                                         | Dokumentácia (`docs/tablesimulator/…`)                                     |
|----------------------------------------------|----------------------------------------------------------------------------|
| inštalácia, parametre, služba Windows        | `instalacia.mdx`                                                           |
| pripojenie INISSu, načítanie stanice         | `prve-kroky/`                                                              |
| linky, tabule, podporované triedy, vzhľady, poruchy a scenáre, ELEN Monitor | `linky-a-tabule/`                           |
| log, štatistiky, záznamy, porovnanie         | `komunikacia/`                                                             |
| virtuálne tabule, šablóny XSL, archív        | `inissview/`                                                               |
| informačné panely                            | `informacne-panely/`                                                       |
| kiosk, prístupové kľúče                      | `zariadenia/`                                                              |
| nastavenia, používatelia a roly, HTTPS, upozornenia, záloha, databáza | `sprava/`                                         |
| API pre programy, pojmy, riešenie problémov  | `referencia/`                                                              |

## Spustenie

```bash
dotnet run --project TableSimulator/TableSimulator.csproj
```

Server otvorí prehliadač na `http://localhost:5470/`. Parametre `--port`, `--no-browser`, `--data`, `--reset-admin`,
`--install-service` a `--uninstall-service` popisuje `instalacia.mdx`. Chýbajúci parameter nahradí premenná
prostredia `TABLESIMULATOR_DATA` alebo `TABLESIMULATOR_NO_BROWSER=1` (využívajú ju testy servera). Zrušený `--config`
server len ohlási – linky a tabule sú v databáze a prenášajú sa exportom a importom.

Nad jedným priečinkom nastavení beží jeden server (zámok `server.lock`, `Infrastructure/Hosting/ServerLock`) – ďalšie
spustenie server nespustí, len otvorí bežiaci (`RunningServer`). Pred migráciou existujúcej databázy SQLite si server
odloží jej kópiu do `before-update\<čas>` (`DatabaseBootstrap.BackupBeforeUpdateAsync`).

## Inštalátor

```bash
dotnet publish TableSimulator/TableSimulator.csproj -c Release        # len win-x64, do prázdneho bin\Release\net10.0\win-x64\publish
"/c/Program Files/Inno Setup 7/ISCC.exe" TableSimulatorInstaller.iss  # Output\tablesimulator-setup.exe
```

`TableSimulatorInstaller.iss` stavia na spoločnom základe inštalátorov `ToolsCore/Installer` (`Common.iss`, `DotNet.iss`):
služba Windows (`--install-service --data %ProgramData%\TableSimulator`), pravidlo Windows Firewall pre program,
chýbajúci .NET 10 Runtime a ASP.NET Core Runtime. Používateľský opis je v `instalacia.mdx`.

Server zapisuje záznam (Serilog) do konzoly a do denných súborov `logs\tablesimulator-RRRRMMDD.log` v priečinku
nastavení (14 dní): spojenia liniek s INISSom, vybavené príkazy, prihlásenia, odmietnuté požiadavky a chyby.
Riadok súboru nesie meno prihláseného používateľa (`-` bez prihlásenia a mimo požiadaviek).

Build zostaví aj webové rozhranie (`bun install` a `bun run build` v `web/`, výstup do `TableSimulator/wwwroot`);
`-p:SkipWebBuild=true` ho vynechá. Pri vývoji webu: spustený server a v `web/` `bun run dev` (Vite posiela `/api`
a `/ws` na server).

## Testy

```bash
dotnet test TableSimulator.Tests/TableSimulator.Tests.csproj   # -p:SkipWebBuild=true bez buildu webu
cd web && bun test                                            # renderer a pomocné funkcie webu
cd web && bun run lint && bun run format:check                # ESLint a Prettier
cd web && bun run e2e                                         # Playwright v Edge proti dvom serverom s čistými dátami
```

Testy nad PostgreSQL (`PostgresTests`, časť `DatabaseApiTests`) bežia len s pripojením v premennej
`TABLESIMULATOR_TEST_POSTGRES` (používateľ smie zakladať databázy – každý test si založí a zmaže vlastnú
`ts_test_…`), inak sa preskočia. Stačí kontajner:

```bash
docker run -d --name ts-pg -e POSTGRES_PASSWORD=<heslo> -p 55432:5432 postgres:17
```

a `TABLESIMULATOR_TEST_POSTGRES=Host=localhost;Port=55432;Database=postgres;Username=postgres;Password=<heslo>`.

## Architektúra

| Projekt                       | Obsah                                                                                    |
|-------------------------------|------------------------------------------------------------------------------------------|
| `TableSimulator.Core`         | `net10.0` bez UI a bez Windows: linky (`Transport/`), protokoly (`Protocols/`), stav tabúľ (`Boards/`), záznam a prehratie (`Recording/`), súbory ELEN Monitora (`Monitor/`), XML tabúľ INISSu a definície INISSView (`Virtual/`), denné archívy TAB/REP (`Archive/`), `Simulation/Simulator` |
| `TableSimulator.Contracts`    | kontrakty API a WebSocketu – DTO bez logiky a bez závislostí, každé v samostatnom súbore po oblastiach (`Lines/`, `Boards/`, `Display/`, `Frames/`, `Recordings/`, `Live/`…); používa ich aj GVDEditor |
| `TableSimulator.Application`  | príkazy a dotazy (CQRS bez mediátora) s handlermi a validátormi FluentValidation po oblastiach, dekorátory (overenie, záznam), `Result`/`Error`, mapovanie jadro ↔ kontrakty, služby (záznamy, písma, vzhľady), roly a oprávnenia (`Auth/`) |
| `TableSimulator.Persistence`  | databáza servera (EF Core + ASP.NET Core Identity): nastavenie servera, linky a tabule, používatelia a roly, kľúče, audit, virtuálne tabule; `AppDbContext` je spoločný, poskytovateľ má vlastnú podtriedu a migrácie (`Sqlite/`, `Migrations/Sqlite/`); `ServerDatabase` otvorí databázu aj mimo DI (štart servera) |
| `TableSimulator`              | server ASP.NET Core: endpointy v `Api/` (každý v samostatnom súbore), WebSocket `/ws`, Serilog, prihlasovanie a obnova správcu (`Infrastructure/Auth/`), HTTPS a certifikáty (`Infrastructure/Https/`), súbory nastavení a konfigurácie v `Infrastructure/` |
| `TableSimulator.Tests`        | MSTest – rámce a súčty podľa príkladov z docs (ELEN, LCD1, ITP), rozklad riadku, simulátor, linka TCP aj rúra, záznam a prehratie, aplikačná vrstva, tvar JSON kontraktov, XML tabúľ a `Web.config` INISSView, archívy TAB/REP, server cez `WebApplicationFactory` (prihlasovanie, oprávnenia, virtuálne tabule, archív) |
| `web/`                        | React + TypeScript (bun, Vite): ovládanie a renderer tabúľ; testy `bun test`              |

### Snímky do dokumentácie

`bun run docshots` vo `web/` zostaví web a server, spustí ukážkovú stanicu (`docshots/demo.ts`) a uloží snímky
oboch tém do `iniss-tools-docs/static/img/tablesimulator`. Písma ELEN berie z priečinka v premennej `DOCSHOTS_FONTS`
(napr. `Sim\FONT` programu ELEN Monitor); bez nej tabule ELEN ukážu náhradné bodové písmo. Cesty, meno a adresy
počítača nahradí na snímkach ukážkovými. Simulátor spustený na tom istom počítači treba zastaviť – inak sú porty
liniek 4700x obsadené a snímky ukážu iné porty.

```powershell
$env:DOCSHOTS_FONTS = "C:\ELEN\Monitor\Sim\FONT"; bun run docshots
```

### API

Endpoint je tenký: z požiadavky zostaví príkaz (`ICommand`) alebo dotaz (`IQuery<T>`), zavolá jeho handler
(`ICommandHandler<>`, `IQueryHandler<,>` – vstrekovaný priamo, bez mediátora) a výsledok `Result` prevedie na
odpoveď. Handlery a validátory sa registrujú skenovaním zostavenia (Scrutor) a každý handler obalia dekorátory:
**overenie** (validátory FluentValidation – pri chybe sa handler nespustí) a navonok **záznam** (čas, chyba).

Chyba je odpoveď ProblemDetails so stavom podľa druhu (`400` neplatné vstupy a nevykonateľná požiadavka, `401`
neprihlásený, `403` bez oprávnenia, `404`, `409`, `500`), kódom chyby vo vlastnosti `code` a pri neplatných vstupoch zoznamom `errors: [{code, description}]`.
Kód je stabilný kľúč (`line.endpoint`, `board.notFound`…) – webové rozhranie podľa neho zobrazí text v jazyku obsluhy
(`error.<kód>` v `web/src/i18n`), `description` je anglický popis pre záznam a klientov bez prekladu.

Novú požiadavku pridáte takto: DTO do `TableSimulator.Contracts`, príkaz alebo dotaz s handlerom (a validátorom) do
oblasti v `TableSimulator.Application`, trieda `IEndpoint` do `TableSimulator/Api/<oblasť>/` s politikou
`.RequireAuthorization(Permissions.X)` (verejný endpoint výslovne `.AllowAnonymous()` – test
`KazdyEndpoint_MaPolitikuAleboJeVerejny` endpoint bez jedného z nich odmietne), novú snímku OpenAPI
(`TABLESIMULATOR_UPDATE_OPENAPI=1 dotnet test … --filter OpenApiTests`, potom vo `web/` `bun run api` vygeneruje klienta)
a texty nových kódov chýb do `web/src/i18n`.

Zmena databázy = nová migrácia pre každého poskytovateľa:

```bash
dotnet ef migrations add <Nazov> --project TableSimulator.Persistence --startup-project TableSimulator.Persistence --context SqliteAppDbContext --output-dir Migrations/Sqlite
dotnet ef migrations add <Nazov> --project TableSimulator.Persistence --startup-project TableSimulator.Persistence --context PostgresAppDbContext --output-dir Migrations/Postgres
```

PostgreSQL ukladá časy (`timestamptz`) len v UTC – `PostgresAppDbContext` ich pri zápise prevedie, pri čítaní
označí ako UTC.

Server databázu pri štarte vytvorí alebo zmigruje (ešte pred zostavením servera – `DatabaseBootstrap` – kvôli
nastaveniu) a doplní roly.

### Jadro, profil a vzhľad

Jadro rozhoduje, **čo** tabuľa ukazuje (rozklad správ, riadky ELEN odspodu a Elektročas zhora, striedanie,
pohyblivý text, jas, hodiny). Prehliadač rozhoduje, **ako** to vyzerá. Medzi nimi sú tri vrstvy dát mimo kódu:

1. **Profil tabule** – hardvér: šírka v bodoch, počet a výška riadkov, súbor písiem ELEN (`.bin`), farebnosť,
   hlavička, časovanie striedania (predvolene 2 s), blikania (1,0 s svieti / 0,5 s nesvieti) a pohyblivého textu
   (60 bodov/s) – predvolené hodnoty podľa ELEN Monitora a konfigurácie panelov. Upravuje sa v dialógu tabule.
2. **Vzhľad** – priečinok s `design.json` (vstavané vo `web/public/designs/`, vlastné v
   `%AppData%\TableSimulator\designs`; rovnaký názov vstavaný prekryje). Zmena súboru sa na tabuliach prejaví hneď.
   Predvolený je `elen-led` pre tabule ELEN a `lcd-dark` pre znakové tabule Elektročas (ďalej `lcd-green`,
   `lcd-blue`); oba druhy tabúľ kreslí ten istý renderer bodov, takže sa dá použiť ktorýkoľvek vzhľad.
3. **Písma ELEN** – súbory `.bin` z priečinka písiem (napr. `Sim\FONT` programu ELEN Monitor). Bez písma sa text
   kreslí vstavaným bodovým písmom – bitmapové písma misc-fixed (X.org, voľné dielo) od 5x7 po 10x20 podľa výšky
   riadku (6x10 pre 10 riadkov, 9x15 pre 16), so všetkými znakmi CP1250; polohy textu sedia, tvary znakov sa od
   tabule môžu líšiť. Rez písma ELEN: pevná šírka (6 bodov, od 16 riadkov 8), proporcionálne alebo tučné. Znakové
   tabule Elektročas kreslia písmo 7x14 do bunky 8×16. Dáta písiem generuje `web/tools/pixel-fonts/bdf2ts.py`
   (bodky ako štvorčeky 2×2, ď ť ľ Ľ s apostrofom); pod 7 riadkov ostáva písmo prehliadača.

### `design.json`

```json
{
  "name": "LED – okrúhle body",
  "renderer": "led-matrix",
  "pitch": 5,
  "dot": 0.78,
  "shape": "circle",
  "off": "#2b1a14",
  "colors": { "0": "#ffb000", "1": "#ff3b22", "2": "#3dff4a", "3": "#ffc21a" },
  "glow": 0.6,
  "background": "#0c0c0c",
  "rowGap": 1,
  "padding": 3,
  "frame": { "width": 10, "color": "#24272b", "radius": 8 }
}
```

| Kľúč         | Význam                                                                           |
|--------------|----------------------------------------------------------------------------------|
| `pitch`      | vzdialenosť stredov bodov v CSS px pri mierke 1 (tabuľa sa aj tak prispôsobí miestu) |
| `dot`        | priemer bodu ako podiel `pitch`                                                  |
| `shape`      | `circle`, `square` alebo `rounded`                                               |
| `off`        | farba zhasnutej LED; `null` = nekresliť                                          |
| `colors`     | farba podľa farby písma ELEN: `0` bez farby (aj všetky znaky tabúľ Elektročas), `1` červená, `2` zelená, `3` žltá, `mono` jednofarebný panel |
| `glow`       | žiara okolo rozsvietenej LED, `0`–`1`                                            |
| `rowGap`     | medzera medzi riadkami tabule v bodoch                                           |
| `padding`    | okraj medzi rámom a bodmi v bodoch                                               |
| `frame`      | rám tabule: šírka a polomer v CSS px, farba                                      |
| `gapColor`   | výplň medzi riadkami; `null` = pozadie                                          |
| `header`     | hlavička: `background`, `color`, `font`, `bold`, `clockColor`, `clockBackground`, `clockFont` |

Chýbajúce kľúče doplní predvolený vzhľad.

Vzhľady sa dajú upravovať aj na stránke **Vzhľady** (oprávnenie upravovať simulátor): náhľad na ukážkovej alebo
skutočnej tabuli sa mení pri každej úprave, uloženie zapíše `design.json` do priečinka vlastných vzhľadov (vstavaný vzhľad
sa uloží ako vlastný s rovnakým id a prekryje ho; „Vrátiť vstavaný“ ho zmaže). API: `PUT /api/designs/{id}`
(`SetDesignRequest`), `DELETE /api/designs/{id}`. Predvolený priečinok vlastných vzhľadov server založí pri štarte.

### Písma ELEN

Číslo písma vyberie z `.bin` písmo takto (overené v ELEN Monitore): rozšírené písmo `n` (bit `0x8000`, bity 8–11)
→ písmo `n`, inak rez (bity 4–5) → písmo s číslom rezu. Inak sa dá zvoliť v profile (`fontMap`: `face0`…`face3`, `ext1`…). Pri písme
s vysokými číslicami (bit `0x08`) sa medzera, číslice a dvojbodka berú zo znakov `0x14`–`0x1F`.

## Poznámky k implementácii

Podrobnosti, ktoré používateľská dokumentácia neuvádza:

- **Údaje servera** – všetko je v databáze (SQLite predvolene, voliteľne PostgreSQL – výber v `database.json`
  mimo databázy, heslo DPAPI pre počítač). Mimo nej ostávajú `recordings\*.tsrec`, `designs\`, `media\`, `https\`,
  `keys\` a logy. Prvý štart s prázdnou databázou prevezme staré `settings.json` a `config.json` a premenuje ich na
  `*.imported` (poškodený súbor na `*.bad`). Nová databáza PostgreSQL prevezme zo SQLite len nastavenie servera.
  Súbor SQLite pri prechode ostáva bez zmeny – po návrate na SQLite simulátor pokračuje s údajmi, ktoré v ňom boli. Údaje medzi
  databázami prenáša `DatabaseCopier` (Persistence): rovnaký model, tabuľky v poradí cudzích kľúčov s pôvodnými
  kľúčmi, v PostgreSQL potom posunie postupnosti identity; všetko v jednej transakcii.
- **Záloha** (`Infrastructure/Backup/ServerBackup`) – ZIP s `backup.json`, online kópiou SQLite (pri PostgreSQL
  prenesenou do súboru SQLite), `designs/`, `media/` a voliteľne `recordings/`. Obnova sa rozbalí do
  `restore.pending` a použije ju `ApplyPendingAsync` v `Program` pred `DatabaseBootstrap`; pôvodné údaje idú do
  `before-restore\<čas>`, výsledok do `restore-result.json`, zlyhaná záloha do `restore.failed-<čas>`.
- **Záloha informačných panelov** – ZIP so `signage.json` (formát `TableSimulator.Signage` verzia 1) a
  `media/<32 hex>.<prípona>`; pred zápisom sa overí každá definícia, odkazy aj súbory (`signage.importFormat`,
  `signage.importInvalid`), databáza sa mení v jednej transakcii.
- **Záznam `.tsrec`** – JSON Lines v UTF-8: hlavička s linkami a tabuľami, potom `{"t":…,"line":…,"rx":"hex"}` od
  INISSu a `"tx"` odpovede; ukladajú sa bajty, ako prišli. Porovnanie (`RecordingTimeline`, `TimelineDiff` – Myers)
  prehrá oba záznamy naprázdno do vlastného simulátora, zmeny do 1 s sú jeden stav.
- **Štatistika** – `TrafficCollector` počíta po minútach pod zámkom simulátora, `StatisticsService` zapisuje skončenú
  minútu (pri zastavení aj rozpracovanú) a maže po 30 dňoch; dotaz zoskupuje do 3 h po 1 min, do 2 dní po 10 min,
  do 8 dní po hodine, inak po 6 h (`TrafficRetention.BucketFor`).
- **Scenáre** – `ScenarioRunner` zlúči účinky bežiacich scenárov, mení len rozdiely a pôvodné poruchy vráti;
  `ScenarioHostedService` tiká 200 ms a pri ukončení scenáre zastaví pred uložením tabúľ. Beh sa neukladá.
- **Vzdialený INISS** – porucha tabule sa prejaví v stave `TSTA`: *neodpovedá* `0C`, NAK `10`, nezmysel `11`, stav
  Elektročasu podľa bitov (`10`, `14`–`17`); Adon/BUSE dostane kód `19`. Pravidelné dotazy na stav front (`RSTA`) do
  logu nejdú, do zrkadla idú vybalené správy tabúľ.
- **Prihlasovanie** – cookie ASP.NET Core Identity; bez prihlásenia (len localhost a vypnuté `RequireLogin`) je
  používateľ správca `local`. Zmeny v `/api` s cookie len s hlavičkou `X-Requested-With`, WebSocket odmietne cudzí
  `Origin`. `LoginThrottle`: po 10 neúspešných pokusoch z adresy (IPv6 po /64) za 15 minút `429 auth.tooManyAttempts`.
  Prístupové kľúče `ts_` + 43 znakov, server drží len SHA-256. Kľúče ochrany dát v `keys\` šifruje DPAPI pre počítač.
- **HTTPS** – vlastná certifikačná autorita (RSA 2048, 10 rokov) v `https\`, certifikát servera najviac 397 dní,
  obnova mesiac pred koncom alebo pri novej adrese; vlastný certifikát server načíta znova do 5 minút po zmene súboru.

## Licencia

GNU General Public License v3.0, pozri [LICENSE](LICENSE).
