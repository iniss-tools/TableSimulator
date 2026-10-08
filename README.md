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
`--data <priečinok>` (priečinok nastavení a databázy inde než v `%AppData%\TableSimulator`, napr. prenosná
inštalácia alebo oddelené prostredie), `--reset-admin [meno]` (obnova prístupu správcu, pozri
[nižšie](#obnova-prístupu-správcu)). Chýbajúci parameter nahradí premenná prostredia `TABLESIMULATOR_DATA` alebo
`TABLESIMULATOR_NO_BROWSER=1` (využívajú ju testy servera). Zrušený `--config` server len ohlási – linky a tabule
sú v databáze a prenášajú sa exportom a importom (pozri [Údaje servera](#údaje-servera)).

### Služba Windows

Aby simulátor bežal bez prihláseného používateľa a štartoval so systémom, nainštaluje sa ako služba (príkazový riadok
spustený ako správca):

```bash
TableSimulator.exe --install-service [--data <priečinok>] [--port <číslo>]
TableSimulator.exe --uninstall-service
```

Služba `TableSimulator` (účet LocalSystem) štartuje oneskorene so systémom, po páde ju Windows spustí znova a v
Nastaveniach (sekcia Server) sa dá reštartovať – server sa ukončí s kódom 3 a Windows ho spustí znova. Bez `--data`
použije inštalácia priečinok nastavení používateľa, ktorý ju spustil (tie isté linky, tabule a používatelia); služba
spustená bez `--data` má predvolený `%ProgramData%\TableSimulator`. Kľúče ochrany dát sa šifrujú DPAPI pre počítač
(prečíta ich server spustený používateľom aj služba); staršie kľúče šifrované pre používateľa inštalácia prevedie.
Opätovná inštalácia zmení nastavenie existujúcej služby, odinštalovanie ponechá nastavenia aj databázu. Služba nevidí
vetvu registra INISSu v HKCU prihláseného používateľa (načítanie stanice z INISSu číta HKLM a súbor .INI).

## Údaje servera

Všetko, čo server uchováva, je v databáze `tablesimulator.db` (SQLite) v priečinku nastavení: nastavenie servera,
linky a tabule (profil tabule ako JSON v stĺpci, ostatné údaje v stĺpcoch, enumy kľúčmi z JSON – `display`,
`noReply`…), používatelia, prístupové kľúče, audit, virtuálne tabule a ich rozloženia. Mimo nej ostávajú súbory,
ktoré sa upravujú alebo prenášajú ako súbory: záznamy komunikácie (`recordings\*.tsrec`), vlastné vzhľady
(`designs\<id>\design.json`, menia sa ručne a server ich načíta hneď), certifikáty HTTPS (`https\`), kľúče
ochrany dát (`keys\`) a logy.

Nastavenie servera sa číta pri štarte ešte pred zostavením servera (porty, prístup zo siete, prihlasovanie);
linky a tabule sa po každej zmene uložia celé s krátkym odkladom a pri zastavení servera. **Prvý štart**
s prázdnou databázou prevezme staré súbory `settings.json` a `config.json` z priečinka nastavení a premenuje ich na
`*.imported` (poškodený súbor sa odloží ako `*.bad` a použijú sa predvolené údaje); bez nich začne predvoleným
nastavením a jednou linkou ELEN so samoučením. Neskôr podložené staré súbory server ignoruje.

Linky a tabule sa prenášajú na stránke **Linky**: **Exportovať** stiahne súbor JSON (`GET /api/config`, rovnaký tvar
ako starý `config.json`), **Importovať…** ho nahrá (`POST /api/config/import` `{content, replace}`) – predvolene
pridá len chýbajúce linky a tabule (ako pri nahratí záznamu), s voľbou *nahradiť všetko* (s potvrdením) nahradí
všetky; neplatný súbor (`config.invalid`, aj tabuľa bez linky či nepodporovaná trieda) nič nezmení. Import je
v audite (`ImportConfig`, cieľ `add`/`replace`).

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

### Stav liniek

Nad tabuľami je prehľad liniek: stav spojenia s INISSom a pri pripojenom INISSe čas poslednej správy (klik na linku
ukáže len jej tabule). INISS tabuliam posiela len pri zmene (a čas raz za hodinu), takže ticho na linke nie je
porucha. **Výpadok** je odpojenie INISSu po TCP alebo rúre (stav *INISS sa odpojil* – linka čaká, kým sa pripojí
znova) alebo linka, ktorá nebeží (obsadený port, chýbajúci sériový port). Pri UDP a sériovom porte simulátor
odchod INISSu nepozná – ukazuje len čas poslednej správy. Výpadok ukáže oznámenie, ktoré trvá do návratu linky
(návrat krátko potvrdí), a počet liniek s výpadkom pri položke **Linky** v menu; odpojenie zapíše aj log servera
ako varovanie. *Systémové oznámenia* v ponuke používateľa (len cez HTTPS alebo na tomto počítači) ohlásia výpadok
aj vtedy, keď okno simulátora nie je vidieť.

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

## Snímka tabule

Fotoaparát na karte tabule uloží jej obsah v tej chvíli ako PNG (`tabula-<linka>-<adresa>-<dátum>_<čas>.png`) –
do dokumentácie alebo hlásenia chyby – alebo ho skopíruje do schránky (len cez HTTPS alebo na tomto počítači).
Snímka má pevnú mierku nezávislú od veľkosti karty: bod tabule = `pitch` px vzhľadu, tabuľa užšia ako 1000 px
dvojnásobne. Kreslí ju ten istý renderer ako kartu, s hlavičkou, hodinami a striedaním v danej chvíli.

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

## Archív tabúľ a hlásení

Záložky **Archív tabúľ** a **Archív hlásení** na stránke **Virtuálne** ukazujú denné archívy INISSu
`TAB<rrrr-mm-dd>.XML` (čo tabule ukazovali o vlakoch a informačné texty) a `REP<rrrr-mm-dd>.XML` (hlásenia, zmeny
stavu a úpravy vlakov, prihlásenia) zo sledovaných priečinkov logov – ako archív v INISSView. Formát opisuje stránka
*Archív tabúľ a hlásení* vo formátoch súborov v dokumentácii INISSTools; archív hlásení sa zapisuje len pri
`ReportLogMode` s príznakom `+2` a `LogReport` = `REP@.XML`.

- Deň sa vyberá zo zoznamu (rovnaký deň vo viacerých priečinkoch = novší súbor), vyhľadáva sa podľa čísla vlaku
  (časť čísla) a textu v ktoromkoľvek údaji záznamu bez ohľadu na veľkosť písmen a diakritiku.
- Záznamy idú po stránkach (predvolene od najnovších); **Obnoviť** načíta znova dnešný, ešte zapisovaný súbor.
- Kódy výluk a odklonov (`vyl`, `vylo`, `od`) dostanú text z legendy, ktorú INISS zapisuje do toho istého súboru.
- Neúplný posledný riadok, poškodené riadky a riadky v textovom tvare (archív hlásení bez príznaku `+2`) sa
  preskočia a ich počet sa ukáže pod filtrom.
- Prečítaný súbor si server drží (posledné štyri), kým sa nezmení – listovanie a filtrovanie ho nečíta znova.

API (oprávnenie na zobrazenie): `GET /api/archive` (dni oboch archívov), `GET /api/archive/tables/{rrrr-mm-dd}`
a `GET /api/archive/reports/{rrrr-mm-dd}` s parametrami `train`, `text`, `newest`, `offset`, `limit` (1–1000,
predvolene 200); odpoveď má vybrané záznamy, počet vyhovujúcich (`total`) a preskočených riadkov (`skipped`).

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

## Zobrazenie na inom zariadení

`#/view/<linka>-<adresa>` ukáže jednu tabuľu bez ovládania (dvojklik = celá obrazovka), `#/wall` všetky tabule
pod sebou. Aby boli dostupné z iných zariadení, zapnite v **Nastaveniach** prístup zo siete (po reštarte server
počúva na všetkých rozhraniach; Windows sa môže spýtať na bránu firewall). Prístup zo siete vždy vyžaduje
prihlásenie – zariadenie s tabuľou sa nemusí prihlasovať, stačí mu odkaz s kľúčom zariadenia (pozri
[Prístupové kľúče](#prístupové-kľúče)). Heslá a kľúče idú sieťou čitateľne, kým nie je zapnuté [HTTPS](#https).

### Kiosk na tablete a televízore

Okno nového kľúča zariadenia ukáže odkaz aj **QR kód** (na počítači so serverom ponúkne adresu počítača v sieti
namiesto `localhost`). Stránky kiosku (`#/view/…`, `#/wall`, `#/virtual/N`) majú vlastný manifest (`GET
/api/manifest?view=…&name=…&key=…`): „Nainštalovať“ / „Pridať na plochu“ vytvorí aplikáciu, ktorá sa spustí rovno
na kiosku na celú obrazovku a s kľúčom zariadenia (`/?view=…` web prevedie na adresu za `#`). Kiosk drží obrazovku
zapnutú (Wake Lock), kurzor a tlačidlá (Celá obrazovka, Nainštalovať) sa po 3 s nečinnosti skryjú. Keď server
nebeží (reštart, zariadenie zapnuté skôr), stránka ukáže „Čakám na server…“ a pripojí sa sama; service worker
(`sw.js`) drží rozhranie v pamäti prehliadača, takže sa kiosk načíta aj bez servera. Inštalácia, Wake Lock a service
worker fungujú len cez HTTPS (alebo na tomto počítači). Ikony aplikácie kreslí `web/tools/icons/make-icons.py`.

## Prihlasovanie a oprávnenia

Kým je server dostupný len z tohto počítača a v **Nastaveniach** nie je zapnuté *Vyžadovať prihlásenie aj na tomto
počítači*, prihlásenie sa nepýta a každý je správca (`local`). Prihlásenie platí, ak je zapnutá aspoň jedna z volieb
*prístup zo siete* a *vyžadovať prihlásenie*; obe sa prejavia po reštarte servera.

Kým neexistuje žiadny aktívny správca, prihlasovacia stránka ponúkne založenie **prvého správcu** – len na počítači
so simulátorom (požiadavka zo siete sa odmietne). Heslo má aspoň 8 znakov, po 5 neúspešných pokusoch sa účet na
5 minút zablokuje. Okrem toho server po 10 neúspešných pokusoch o heslo (prihlásenie aj zmena vlastného hesla)
z jednej adresy za 15 minút ďalšie pokusy z nej odmieta (`429 auth.tooManyAttempts`) bez overenia hesla, kým
najstarší neúspech z okna nevypadne – chráni to pred skúšaním mnohých účtov z jedného miesta. IPv6 sa ráta po
sieťach /64; úspešné prihlásenie počítadlo nenuluje. Počítadlo je v pamäti, reštart ho vynuluje.
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

Používatelia a audit sú v databáze servera `tablesimulator.db`, kľúče na podpis prihlásenia v `keys\` (vo Windows
šifrované DPAPI) – ich zmazanie odhlási všetkých.

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

### HTTPS

V **Nastaveniach** (skupina HTTPS) sa zadá **port HTTPS** – po reštarte server počúva aj na ňom (na rovnakých
adresách ako HTTP). HTTP ostáva: z tohto počítača ho používa GVDEditor aj prehliadač otvorený serverom. Prístup
**zo siete** cez HTTP sa predvolene presmeruje (`308`, aj zmeny – zachová metódu a telo) na rovnakú adresu cez
HTTPS; voľba *Prístup zo siete cez HTTP presmerovať na HTTPS* to vypne (napr. pre televízor, ktorý certifikát
simulátora neprijme). Prihlasovacia cookie získaná cez HTTPS má príznak `Secure`.

**Certifikát simulátora** (bez vlastného certifikátu): pri prvom štarte s HTTPS vznikne vlastná certifikačná
autorita simulátora (RSA 2048, 10 rokov) a ňou podpísaný certifikát servera (najviac 397 dní) pre `localhost`,
meno počítača (aj s doménou), adresu pre INISS a pri prístupe zo siete adresy IPv4 aktívnych sieťových rozhraní.
Mesiac pred koncom platnosti alebo keď pribudne adresa, server vydá nový certifikát servera sám – autorita ostáva,
takže zariadenia ju inštalujú len raz: nastavenia ju ponúknu na stiahnutie (`GET /api/https/certificate`, verejné –
zariadenie si ju stiahne pred prihlásením) aj s odtlačkom SHA-256 na porovnanie. Inštalácia: Windows – dvojklik
→ *Nainštalovať certifikát* → *Dôveryhodné koreňové certifikačné autority*; Android – *Nastavenia → Zabezpečenie →
Šifrovanie a poverenia → Inštalovať certifikát → Certifikát CA*; iOS – profil nainštalovať a v *Všeobecné → Info →
Nastavenia dôvery certifikátov* zapnúť. Súbory sú v priečinku nastavení v `https\` (`authority.crt` verejný,
súkromné kľúče `*.key` šifrované ochranou dát servera – na inom počítači alebo pod iným používateľom Windows sa
nedajú prečítať a vznikne nová autorita).

**Vlastný certifikát** (napr. od firemnej autority alebo win-acme): súbor `.pfx`/`.p12` (s heslom) alebo PEM
(certifikát a kľúč v jednom súbore, alebo kľúč v *Súbore kľúča PEM*; zašifrovaný kľúč s heslom). Nastavenie
certifikát pred uložením overí (`settings.httpsCertificateMissing`, `…Invalid`, `…Key`); heslo sa ukladá
šifrované (ochrana dát servera) a server ho nikdy nevracia – `httpsPasswordSet`, v požiadavke `httpsPassword`
`null` = ponechať, prázdne = zmazať. Zmenu súboru (obnova certifikátu) server načíta sám do 5 minút, zmenu
nastavenia hneď – bez reštartu. Ak sa vlastný certifikát načítať nedá, server použije certifikát simulátora
a chybu ukáže v nastaveniach (`GET /api/https` – stav, mená, platnosť, odtlačok).

GVDEditor na inom počítači zadá adresu `https://server:port`; ak jeho počítač certifikátu nedôveruje, **Zistiť
stav** to ohlási, a ak zadá `http://` a server presmeruje, ohlási adresu HTTPS.

### Obnova prístupu správcu

```bash
TableSimulator --reset-admin [meno] [--data <priečinok>]
```

Pre zabudnuté heslo, zablokovaný alebo vypnutý účet správcu: server sa nespustí, účet dostane nové náhodné heslo
(16 znakov, vypíše sa len do konzoly, do logu nie), odblokuje sa, zapne, dostane rolu správca a jeho prihlásenia
na zariadeniach do minúty skončia. Bez mena sa obnoví jediný správca (ak ich je viac, vypíše ich a skončí s kódom
2); meno, ktoré neexistuje, sa založí ako nový správca. V audite je akcia `ResetAdmin` používateľa `(console)`.
Môže bežať aj pri spustenom serveri (rovnaký `--data`).
