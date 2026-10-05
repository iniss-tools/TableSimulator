# TableSimulator

Simulátor informačných tabúľ pre INISS 3.39. INISS posiela obsah tabúľ po svojich linkách (sekcie `Driver*`)
a simulátor sa tvári ako tabule na druhom konci linky – odpovedá bajtovým protokolom výrobcu a obsah tabúľ
zobrazuje vo webovom prehliadači: na tomto počítači, na inom počítači v sieti, na tablete alebo televízore.

Protokoly sú popísané v `iniss-tools-docs/docs/iniss/protokoly/`. Hotové sú:

| `TableClass` | Tabule                                                        |
|--------------|---------------------------------------------------------------|
| `4`          | ELEN, ELENOLD, ELEN10, ELEN16, ELEN16Kam, ELEKON              |
| `5`          | Elektročas LCD1 a slepecké hlásiče ERP                        |
| `1`          | Elektročas LCD (staršie)                                      |
| `130`        | LCD1, ERP a LCD za sieťovým prevodníkom KTP (pakety ITP)      |

Adon/BUSE sa simulovať nedá – INISS ich ovláda cez knižnicu `infotabd.dll`, ktorej bajty na linke nepoznáme.

## Spustenie

```bash
dotnet run --project TableSimulator/TableSimulator.csproj
```

Server otvorí prehliadač na `http://localhost:5470/`. Parametre: `--port <číslo>`, `--no-browser`,
`--data <priečinok>` (nastavenia inde než v `%AppData%\TableSimulator`, napr. prenosná inštalácia),
`--config <súbor>` (linky a tabule). Chýbajúci parameter nahradí premenná prostredia `TABLESIMULATOR_DATA`,
`TABLESIMULATOR_CONFIG` alebo `TABLESIMULATOR_NO_BROWSER=1` (využívajú ju testy servera).

Server zapisuje záznam (Serilog) do konzoly a do denných súborov `logs\tablesimulator-RRRRMMDD.log` v priečinku
nastavení (14 dní): spojenia liniek s INISSom, vybavené príkazy, prihlásenia, odmietnuté požiadavky a chyby.
Riadok súboru nesie meno prihláseného používateľa (`-` bez prihlásenia a mimo požiadaviek).

Build zostaví aj webové rozhranie (`bun install` a `bun run build` v `web/`, výstup do `TableSimulator/wwwroot`);
`-p:SkipWebBuild=true` ho vynechá. Pri vývoji webu: spustený server a v `web/` `bun run dev` (Vite posiela `/api`
a `/ws` na server).

## Pripojenie INISSu

Každá linka simulátora zodpovedá jednej sekcii `Driver*` v INISSe a počúva na TCP porte (predvolene `47000 + číslo
linky`). INISS sa na ňu pripája ako klient:

```ini
[Driver]
TableClass=4
TablePort=1=TCP://127.0.0.1:47001
```

Hodnotu `TablePort` ukazuje stránka **Linky**. INISS musí mať `Environment\OutToTableDriver=1`. Číslo pred `=` je
číslo linky, podľa ktorého INISS priraďuje tabule z `TPhysic.txt` (`COMUNICATION_PORT`).

Najjednoduchšie je presmerovať INISS z **GVDEditora**: okno Nastavenia INISSu → Nástroje → *Presmerovať tabule na
simulátor*. Zapíše vybrané linky do `.INI` vedľa programu (celé sekcie `Driver*` s `TablePort=N=TCP://adresa:47000+N`
a `OutToTableDriver=1`, pôvodný stav v komentároch, zrušenie ho vráti) a cez `POST /api/station` založí v simulátore
linky s rovnakým číslom a triedou a tabule z `TPhysic`/`TKatalog` (adresa, výrobca, názov; profil podľa výrobcu so
samoučením rozmeru). Tabule, ktoré v simulátore už sú, ostanú nezmenené; linke, ktorá už existuje, sa zmení len trieda
a port. Zmena triedy linky prevedie obsah jej tabúľ na novú rodinu (ELEN ↔ znakové). Ak simulátor vyžaduje
prihlásenie, GVDEditor sa ohlási API kľúčom (pole *API kľúč simulátora*, kľúč druhu program s rolou operátor –
pozri [Prístupové kľúče](#prístupové-kľúče)).

Linka môže ísť aj cez **sériový port** (`COM13:9600,n,8,1`) – napríklad druhý koniec virtuálneho páru COM
portov, ktorého prvý koniec má INISS v `TablePort=COM3`.

Linka môže ísť aj cez **pomenovanú rúru**. Tam sú roly opačné: rúru vytvára INISS (`TablePort=3=\\.\PIPE\Tabule`)
a simulátor sa k nej pripája – v nastavení linky `\\.\PIPE\Tabule`, prípadne `\\počítač\PIPE\Tabule`, ak INISS
beží na inom počítači. Kým INISS nebeží, simulátor to skúša každú sekundu znova.

Tabule netreba zakladať ručne: linka so **samoučením** vytvorí tabuľu pri prvej správe na novú adresu a jej
rozmer zväčšuje podľa toho, čo INISS posiela. Výrobcu (`MANUFACTURER_KEY`) jej dá podľa nastavenia linky – ak INISS
posiela správy, ktoré patria inému výrobcovi (rozšírené písmo `19 h` tabuli `ELEN`, zápis `15`/`16` tabuli `LCD`,
zápis `10` tabuli `LCD1`/`ERP`), karta tabule na to upozorní a ponúkne prepnutie na správneho výrobcu. Režim linky **len počúvať** neodpovedá – hodí sa na sledovanie
linky, na ktorej odpovedajú skutočné tabule.

## ELEN Monitor

V **Nastaveniach** sa dá zadať priečinok programu ELEN Monitor (napr. `D:\INISSroot\Tools\Monitor`). Simulátor
z neho potom berie:

- **predlohy tabúľ** – každý typ panelu (`Sim\*.cfg` a jeho konfigurácia `Sim\CONFIG\rwp_*.bin`) dá rozmer, písmo,
  farebnosť, časy blikania, hlavičku a hodiny; v dialógu tabule „Predloha z ELEN Monitora“,
- **písma** zo `Sim\FONT` (popri vlastnom priečinku písiem),
- **projekty** (`Projects\*.prj`) – tlačidlo „Import z ELEN Monitora“ na stránke Tabule vytvorí z každého simulátora
  projektu tabuľu na zvolenej linke, vrátane dvojitých adries.

Vstavaný vzhľad **ELEN Monitor** kreslí tabule ako Monitor (hranaté body, tmavomodrá hlavička, sivé medzery medzi
riadkami).

Na porovnanie s Monitorom vedľa seba má linka voľbu **zrkadliť na sériový port**: všetko, čo pošle INISS (aj
prehrávaný záznam), sa skopíruje na zvolený COM port, kde cez virtuálny pár portov (napr. com0com) počúva Monitor
v režime „Monitor“. Simulátor odpovedá INISSu sám, Monitor len zobrazuje.

## Tabule Elektročas

Tabule `LCD1`, `ERP` a `LCD` sú znakové: INISS posiela riadky zhora od 0 a stĺpce v znakoch, každý znak s vlastným
číslom písma. Simulátor kreslí znak do bunky 8 bodov × výška riadku (predvolene 40 znakov × 4 riadky × 16 bodov),
písmo `0x40` inverzne a `0x80` blikajúce. Striedanie `#` prichádza vo vrstvách – v časti `k` tabuľa ukazuje
vrstvu `k` tam, kde má znaky, inde vrstvu 0. Každú správu tabuľa potvrdí stavom `70`, na dotaz `08` odpovie typom
`9` (smie zápis `15`), na dotaz `A1` časom a hromadný čas `A8` nastaví hodiny všetkých tabúľ na linke.

Porucha **hlási poruchu v stave** pošle v odpovedi zvolené stavové bity (`01` chybná správa, `08` izolácia 110 V –
INISS ju len zafarbí, `10` súradnice, `40` napájanie, `80` hardvér).

Na linke `130` simulátor hrá aj prevodník KTP: paket ITP rozbalí, odpoveď tabule zabalí s tým istým číslom a
hlavičkou s kódom chyby `0`. INISS tu čaká na odpoveď pri každom pakete, aj pri hromadnom – keď tabuľa neodpovie
(hromadná správa, porucha *neodpovedá*, adresa bez tabule), prevodník hlási chybu `1` *zariadenie neodpovedalo*.

## Hlavičky tabúľ

Hlavička je štítok nad maticou – texty (napr. „ODCHODY / DEPARTURES“ a popisy stĺpcov) a hodiny panelu. INISS ju
neposiela, patrí k profilu tabule; polohy a výšky sú v bodoch matice. Hodiny ukazujú čas, ktorý panelu nastavil
INISS hromadnou správou; kým ho nenastaví, čas počítača. Farby a písmo hlavičky určuje vzhľad (`header` v
`design.json`).

## Záznamy komunikácie

Stránka **Záznamy** nahrá všetko, čo INISS pošle na linky (aj odpovede tabúľ), do súboru `.tsrec` v
`%AppData%\TableSimulator\recordings`. Záznam sa dá prehrať v pôvodnom čase, zrýchlene alebo bez čakania, aj
dokola – správy idú do liniek simulátora, akoby ich posielal INISS, a odpovede tabúľ sa zahodia. Hodí sa na skúšky
bez INISSu, na ukážky a na prenos situácie z inej stanice (súbor sa dá stiahnuť a na inom počítači nahrať); linky
a tabule zo záznamu, ktoré v simulátore chýbajú, sa pri prehratí doplnia.

Formát je JSON Lines v UTF-8: prvý riadok je hlavička s linkami a tabuľami v čase začiatku, potom jeden riadok na
kus prevádzky – `{"t":1.234,"line":1,"rx":"0185…"}` od INISSu alebo `{"t":…,"line":1,"tx":"06"}` odpoveď tabule
(`t` sú sekundy od začiatku, bajty v hex). Ukladajú sa bajty tak, ako prišli, nie celé správy, aby prehratie prešlo
rovnakým skladaním správ.

## Virtuálne tabule

Tabuľa, ktorá má v INISSe vyplnený export do XML (`SAVE_XML` v `TPhysic.TXT`, spravidla s adresou `-1`), zapisuje
svoj obsah do súboru `<SAVE_XML>.xml` v priečinku logov INISSu (`PathNames\LogPath`). Simulátor tieto súbory číta
a ukazuje na celej obrazovke ako INISSView – bez IIS a šablón XSL. Nastavenie INISSu popisuje návod *Virtuálne
tabule cez INISSView* v dokumentácii INISSTools.

1. V **Nastaveniach** zadajte priečinok logov INISSu (najviac 4, jeden na riadok; aj sieťový). Simulátor ho
   prechádza každé 2 s. Rovnomenný súbor vo viacerých priečinkoch sa berie novší, pri rovnakom čase z prvého
   priečinka – ako INISSView pri dvoch priečinkoch. Denné archívy `TAB*.XML`/`REP*.XML` a iné XML vynechá.
2. Na stránke **Virtuálne** pridajte tabuľu: číslo (adresa zobrazenia `#/virtual/<číslo>`, ako `t=` v INISSView),
   súbor XML (`SAVE_XML` bez prípony), počet záznamov a poradie prvého (jedna tabuľa INISSu na dvoch obrazovkách:
   6 od 1. a 6 od 7.), oddeľovač a rozloženie. Tabuľa sa dá založiť aj priamo zo zoznamu nájdených súborov.
3. Odkaz **Zobraziť** dajte prehliadaču na monitore (dvojklik = celá obrazovka).

GVDEditor pri presmerovaní liniek INISSu na simulátor (so založením liniek a tabúľ) pošle aj priečinok logov
INISSu a tabule s vyplneným `SAVE_XML` zo všetkých grafikonov: simulátor priečinok pridá k sledovaným, ak ho vidí
(inak to GVDEditor ohlási), a pre XML, ktoré ešte žiadna virtuálna tabuľa neukazuje, založí tabuľu s rozložením
podľa názvu (odchody, príchody).

**Načítať z INISSView** prevezme definície tabúľ zo súboru `Web.config` (čísla, názvy, popisy, súbory, riadky,
oddeľovač; parametre oddeľuje `TabInfoSeparator`, apostrofy sa vynechajú). Tabuľu s číslom, ktoré už je, prepíše
len so zaškrtnutým *prepísať*. Priečinky z `TabXmlDir` ponúkne na sledovanie. Rozloženie odhadne podľa mena
šablóny XSL (`Pokladny_*` → pokladne, `*cerna*` → čierna, `Odj*` → odchody, `Prij*`/`Prj*` → príchody). Šablóna XSL
a parameter `x` sa uložia pre budúcu podporu šablón, zatiaľ sa nepoužívajú.

### Rozloženia

Vzhľad virtuálnej tabule určuje **rozloženie** – dáta, nie kód: nadpis (`{name}` = názov tabule, `{station}` =
stanica), stanica, dátum a hodiny v hlavičke, popisy stĺpcov, počet riadkov pre veľkosť písma (ak tabuľa nemá
pevný počet záznamov), písmo, farby, intervaly striedania a blikania a stĺpce. Stĺpec ukazuje jeden alebo viac
prvkov XML (`type number` = vlak), má popis (prázdny = predvolený v jazyku obsluhy), šírku (prázdna = podľa
obsahu, `fill` = zvyšok riadku, `12%`, `6em`), zarovnanie, farbu (`accent`, `muted`, `delay` z farieb rozloženia
alebo `#rrggbb`), tučné písmo, veľkosť v % a oddeľovač prvkov. Bez stĺpcov idú stĺpce podľa XML.

Vstavané rozloženia (`TableSimulator.Application/Virtual/Layouts/*.json`, vložené do zostavy): modrá a čierna so
stĺpcami podľa XML (modrá je predvolená), odchody, príchody a pokladne (2 riadky). Nedajú sa meniť; **Kopírovať**
otvorí kópiu v editore s náhľadom na údajoch vybranej tabule. Vlastné rozloženia sú v databáze; rozloženie, ktoré
tabule používajú, sa nedá odstrániť. Zmena rozloženia sa na otvorených obrazovkách prejaví hneď.

Zobrazenie:
- Stĺpce podľa XML idú v poradí stĺpcov katalógovej tabule (prvky `row`). Druh a číslo vlaku sú jeden stĺpec,
  `typehex` a stĺpce prázdne vo všetkých záznamoch sa vynechajú.
- Časti textu oddelené `#` sa striedajú (predvolene po 1,5 s). Oddeľovač ich ukáže naraz, ale len v cieľovej stanici a smere,
  ako v INISSView.
- Text s jedinou časťou a znakom `#` (napr. `3#`) bliká.
- Keď uplynie platnosť obsahu (`validto`), tabuľa ukáže, že údaje nie sú aktuálne (INISS asi nebeží). Neúplný
  súbor, ktorý INISS práve zapisuje, sa skúsi znova; chyba sa ohlási až po troch neúspešných pokusoch.

Definície a vlastné rozloženia sú v databáze servera (`tablesimulator.db`). API: `GET /api/virtual` (definície,
súbory, priečinky, rozloženia), `GET /api/virtual/{číslo}` (vybrané záznamy a rozloženie), `PUT`/`DELETE
/api/virtual/{číslo}`, `PUT`/`DELETE /api/virtual/layouts/{kľúč}`, `POST /api/virtual/import`; zmenu súboru,
definícií alebo rozložení ohlási WebSocket správou `virtual`.

## Architektúra

| Projekt                       | Obsah                                                                                    |
|-------------------------------|------------------------------------------------------------------------------------------|
| `TableSimulator.Core`         | `net10.0` bez UI a bez Windows: linky (`Transport/`), protokoly (`Protocols/`), stav tabúľ (`Boards/`), záznam a prehratie (`Recording/`), súbory ELEN Monitora (`Monitor/`), XML tabúľ INISSu a definície INISSView (`Virtual/`), `Simulation/Simulator` |
| `TableSimulator.Contracts`    | kontrakty API a WebSocketu – DTO bez logiky a bez závislostí, každé v samostatnom súbore po oblastiach (`Lines/`, `Boards/`, `Display/`, `Frames/`, `Recordings/`, `Live/`…); používa ich aj GVDEditor |
| `TableSimulator.Application`  | príkazy a dotazy (CQRS bez mediátora) s handlermi a validátormi FluentValidation po oblastiach, dekorátory (overenie, záznam), `Result`/`Error`, mapovanie jadro ↔ kontrakty, služby (záznamy, písma, vzhľady), roly a oprávnenia (`Auth/`) |
| `TableSimulator.Persistence`  | databáza servera (EF Core + ASP.NET Core Identity): používatelia a roly, definície virtuálnych tabúľ; `AppDbContext` je spoločný, poskytovateľ má vlastnú podtriedu a migrácie (`Sqlite/`, `Migrations/Sqlite/`) |
| `TableSimulator`              | server ASP.NET Core: endpointy v `Api/` (každý v samostatnom súbore), WebSocket `/ws`, Serilog, prihlasovanie (`Infrastructure/Auth/`), súbory nastavení a konfigurácie v `Infrastructure/` |
| `TableSimulator.Tests`        | MSTest – rámce a súčty podľa príkladov z docs (ELEN, LCD1, ITP), rozklad riadku, simulátor, linka TCP aj rúra, záznam a prehratie, aplikačná vrstva, tvar JSON kontraktov, XML tabúľ a `Web.config` INISSView, server cez `WebApplicationFactory` (prihlasovanie, oprávnenia, virtuálne tabule) |
| `web/`                        | React + TypeScript (bun, Vite): ovládanie a renderer tabúľ; testy `bun test`              |

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
`KazdyEndpoint_MaPolitikuAleboJeVerejny` endpoint bez jedného z nich odmietne), typy do `web/src/api.ts` a texty
nových kódov chýb do `web/src/i18n`.

Zmena databázy = nová migrácia pre každého poskytovateľa:

```bash
dotnet ef migrations add <Nazov> --project TableSimulator.Persistence --startup-project TableSimulator.Persistence --context SqliteAppDbContext --output-dir Migrations/Sqlite
```

Server databázu pri štarte vytvorí alebo zmigruje a doplní roly.

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
   kreslí približným písmom prehliadača; polohy textu sedia, tvary znakov nie. Znakové tabule Elektročas kreslia
   písmo prehliadača s pevnou šírkou do bunky znaku.

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

### Písma ELEN

Číslo písma vyberie z `.bin` písmo takto (overené v ELEN Monitore): rozšírené písmo `n` (bit `0x8000`, bity 8–11)
→ písmo `n`, inak rez (bity 4–5) → písmo s číslom rezu. Inak sa dá zvoliť v profile (`fontMap`: `face0`…`face3`, `ext1`…). Pri písme
s vysokými číslicami (bit `0x08`) sa medzera, číslice a dvojbodka berú zo znakov `0x14`–`0x1F`.

## Zobrazenie na inom zariadení

`#/view/<linka>-<adresa>` ukáže jednu tabuľu bez ovládania (dvojklik = celá obrazovka), `#/wall` všetky tabule
pod sebou. Aby boli dostupné z iných zariadení, zapnite v **Nastaveniach** prístup zo siete (po reštarte server
počúva na všetkých rozhraniach; Windows sa môže spýtať na bránu firewall). Prístup zo siete vždy vyžaduje
prihlásenie – zariadenie s tabuľou sa nemusí prihlasovať, stačí mu odkaz s kľúčom zariadenia (pozri
[Prístupové kľúče](#prístupové-kľúče)).

## Prihlasovanie a oprávnenia

Kým je server dostupný len z tohto počítača a v **Nastaveniach** nie je zapnuté *Vyžadovať prihlásenie aj na tomto
počítači*, prihlásenie sa nepýta a každý je správca (`local`). Prihlásenie platí, ak je zapnutá aspoň jedna z volieb
*prístup zo siete* a *vyžadovať prihlásenie*; obe sa prejavia po reštarte servera.

Kým neexistuje žiadny aktívny správca, prihlasovacia stránka ponúkne založenie **prvého správcu** – len na počítači
so simulátorom (požiadavka zo siete sa odmietne). Heslo má aspoň 8 znakov, po 5 neúspešných pokusoch sa účet na
5 minút zablokuje.
*Zostať prihlásený* drží prihlásenie 14 dní od posledného použitia (aj po reštarte servera), inak do zatvorenia
prehliadača.

| Rola          | Môže                                                                         |
|---------------|------------------------------------------------------------------------------|
| `admin`       | všetko vrátane nastavení servera, používateľov a auditu                       |
| `operator`    | tabule a linky meniť, záznamy nahrávať, prehrávať a mazať, komunikácia        |
| `viewer`      | tabule, linky, záznamy a komunikáciu len pozerať                              |

Oprávnenia (`simulator.view`, `simulator.edit`, `recordings.manage`, `log.view`, `settings.manage`, `users.manage`,
`audit.view`) kontroluje server na každom endpointe aj na WebSockete; webové rozhranie podľa `/api/auth/me` len skryje, čo
používateľ nesmie.

Stránka **Používatelia** (správca) zakladá používateľov, mení im zobrazované meno, rolu a vypnutie, nastavuje nové
heslo (zruší aj zablokovanie), odomkne zablokovaný účet a odstráni ho. Svoju rolu nikto nezmení a seba nevypne ani
neodstráni; aktívny správca musí ostať aspoň jeden. Zmena roly sa prihlásenému prejaví do minúty bez odhlásenia,
vypnutie, nové heslo a odstránenie ho do minúty odhlásia. Vlastné heslo si každý zmení v ponuke pod svojím menom.
Používateľov sa dá pripraviť aj bez zapnutého prihlasovania.

**Audit** (záložka stránky Používatelia, oprávnenie `audit.view`) zaznamená každý príkaz – kto (`-` = neprihlásený),
kedy, z akej adresy, akcia (meno príkazu: `SetLine`, `Login`…), čoho sa týka (linka, tabuľa, používateľ…), či sa
podaril (inak kód chyby) a podrobnosti ako JSON; heslá sú nahradené `***`, obsahy súborov vynechané. Zapisuje ho
dekorátor príkazov, takže nový príkaz sa zaznamená sám – cieľ doplní explicitná implementácia `IAuditable`.
Zaznamenané sú aj neúspešné prihlásenia; požiadavky odmietnuté pre chýbajúce oprávnenie zachytí záznam servera.
Záznamy sa držia 365 dní. API: `GET /api/audit?before=&user=&action=&limit=` (od najnovších).

Databáza používateľov a auditu je `tablesimulator.db` (SQLite) v priečinku nastavení, kľúče na podpis prihlásenia
v `keys\` (vo Windows šifrované DPAPI) – ich zmazanie odhlási všetkých.

### Prístupové kľúče

Kľúč nahrádza prihlásenie zariadeniam a programom; vytvára ho správca na stránke Používatelia → **Kľúče**
(`POST /api/keys`). Kľúč (`ts_` + 43 znakov) sa ukáže len raz – server si pamätá iba jeho hash SHA-256, začiatok na
rozlíšenie a čas posledného použitia. Odstránený kľúč prestane platiť hneď (overuje sa pri každej požiadavke).

- **Zariadenie** (monitor, kiosk) má vždy rolu pozorovateľ. Dialóg po vytvorení zloží odkaz na zvolenú stránku
  (virtuálna tabuľa, tabuľa, všetky tabule): `http://server:5470/?key=…#/virtual/3`. Webové rozhranie si kľúč z adresy
  uloží do relácie karty, z adresy ho odstráni a posiela ho v hlavičke; WebSocket ho dostane v parametri
  `access_token`. Karta s kľúčom pristupuje ako zariadenie aj vtedy, keď je v prehliadači prihlásený používateľ.
- **Program** (GVDEditor, skript) posiela `Authorization: Bearer <kľúč>` s rolou podľa výberu (GVDEditor potrebuje
  operátora). Takú požiadavku nemôže podvrhnúť cudzia stránka, preto nepotrebuje hlavičku `X-Requested-With`.

V audite je požiadavka s kľúčom pod menom `key:názov`.

Klient API s prihlásením cez cookie: zmena (`POST`, `PUT`, `DELETE`) musí niesť hlavičku `X-Requested-With`
s ľubovoľnou hodnotou (ochrana proti CSRF, inak `403 auth.csrf`). WebSocket `/ws` odmietne spojenie
zo stránky iného servera (hlavička `Origin`).
