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
`--config <súbor>` (linky a tabule).

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

Linka môže ísť aj cez **sériový port** (`COM13:9600,n,8,1`) – napríklad druhý koniec virtuálneho páru COM
portov, ktorého prvý koniec má INISS v `TablePort=COM3`.

Linka môže ísť aj cez **pomenovanú rúru**. Tam sú roly opačné: rúru vytvára INISS (`TablePort=3=\\.\PIPE\Tabule`)
a simulátor sa k nej pripája – v nastavení linky `\\.\PIPE\Tabule`, prípadne `\\počítač\PIPE\Tabule`, ak INISS
beží na inom počítači. Kým INISS nebeží, simulátor to skúša každú sekundu znova.

Tabule netreba zakladať ručne: linka so **samoučením** vytvorí tabuľu pri prvej správe na novú adresu a jej
rozmer zväčšuje podľa toho, čo INISS posiela. Režim linky **len počúvať** neodpovedá – hodí sa na sledovanie
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

## Architektúra

| Projekt                  | Obsah                                                                                    |
|--------------------------|------------------------------------------------------------------------------------------|
| `TableSimulator.Core`    | `net10.0` bez UI a bez Windows: linky (`Transport/`), protokoly (`Protocols/`), stav tabúľ (`Boards/`), záznam a prehratie (`Recording/`), súbory ELEN Monitora (`Monitor/`), `Simulation/Simulator` |
| `TableSimulator`         | server ASP.NET Core: API, WebSocket `/ws`, statické súbory webu, nastavenia               |
| `TableSimulator.Tests`   | MSTest – rámce a súčty podľa príkladov z docs (ELEN, LCD1, ITP), rozklad riadku, simulátor, linka TCP aj rúra, záznam a prehratie |
| `web/`                   | React + TypeScript (bun, Vite): ovládanie a renderer tabúľ; testy `bun test`              |

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
počúva na všetkých rozhraniach; Windows sa môže spýtať na bránu firewall).
