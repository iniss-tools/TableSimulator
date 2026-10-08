# Prevod pisiem misc-fixed (X.org, Markus Kuhn - BDF, public domain) na src/render/pixel-fonts.data.ts: len znaky
# ASCII a CP1250, kazdy znak ako riadky bitov v hex (bit 0x80 prveho bajtu = lavy stlpec bunky). Upravy pre LED
# tabule: bodky v tvare plus -> stvorcek 2x2, ď ť ľ Ľ s apostrofom vpravo namiesto mäkčeňa nad pismenom.
# Spustenie z web/: python tools/pixel-fonts/bdf2ts.py (BDF stiahne do tools/pixel-fonts/.bdf, ak tam nie su).
import io
import os
import sys
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
BDF = os.path.join(HERE, ".bdf")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "..", "..", "src", "render", "pixel-fonts.data.ts")
SOURCE = "https://gitlab.freedesktop.org/xorg/font/misc-misc/-/raw/master/%s.bdf"
FONTS = ["5x7", "5x8", "6x10", "6x12", "7x13", "7x14", "9x15", "10x20"]

os.makedirs(BDF, exist_ok=True)
for font in FONTS:
    path = os.path.join(BDF, font + ".bdf")
    if not os.path.exists(path):
        urllib.request.urlretrieve(SOURCE % font, path)

# znaky: ASCII + vsetko z CP1250 (stredoeuropske pismena, uvodzovky, pomlcky, °...)
CHARS = set(range(0x20, 0x7F))
for b in range(0x80, 0x100):
    try:
        CHARS.add(ord(bytes([b]).decode("cp1250")))
    except UnicodeDecodeError:
        pass
CHARS.discard(0xA0)  # nedelitelna medzera = medzera


def parse(path):
    lines = io.open(path, encoding="latin-1").read().splitlines()
    props, glyphs = {}, {}
    i = 0
    while i < len(lines):
        parts = lines[i].split()
        if not parts:
            i += 1
            continue
        key = parts[0]
        if key in ("FONTBOUNDINGBOX",):
            props["bbx"] = list(map(int, parts[1:]))
        elif key in ("FONT_ASCENT", "FONT_DESCENT"):
            props[key] = int(parts[1])
        elif key == "STARTCHAR":
            enc, bbx, rows, dwidth = None, None, [], None
            i += 1
            while not lines[i].startswith("ENDCHAR"):
                p = lines[i].split()
                if p[0] == "ENCODING":
                    enc = int(p[1])
                elif p[0] == "BBX":
                    bbx = list(map(int, p[1:]))
                elif p[0] == "DWIDTH":
                    dwidth = int(p[1])
                elif p[0] == "BITMAP":
                    i += 1
                    while not lines[i].startswith("ENDCHAR"):
                        rows.append(lines[i].strip())
                        i += 1
                    break
                i += 1
            if enc is not None:
                glyphs[enc] = (bbx, rows, dwidth)
        i += 1
    return props, glyphs


def cell(props, glyph):
    """Bunka sirka x vyska (ascent + descent) s bitmi znaku na spravnom mieste."""
    width, _, _, _ = props["bbx"]
    ascent, descent = props["FONT_ASCENT"], props["FONT_DESCENT"]
    height = ascent + descent
    (gw, gh, gx, gy), rows, _ = glyph
    grid = [[0] * width for _ in range(height)]
    top = ascent - (gy + gh)
    for r, hexrow in enumerate(rows):
        bits = int(hexrow, 16)
        nbits = len(hexrow) * 4
        for c in range(gw):
            if bits >> (nbits - 1 - c) & 1:
                y, x = top + r, gx + c
                if 0 <= y < height and 0 <= x < width:
                    grid[y][x] = 1
    return grid


def square_dots(grid):
    """Bodky v tvare plus (pismo pre obrazovku) na stvorcek 2x2 ako na LED tabuli - inak dvojbodka vyzera ako ‡."""
    h, w = len(grid), len(grid[0])
    on = lambda y, x: 0 <= y < h and 0 <= x < w and grid[y][x] == 1
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            plus = on(y, x) and on(y - 1, x) and on(y + 1, x) and on(y, x - 1) and on(y, x + 1)
            corners = on(y - 1, x - 1) or on(y - 1, x + 1) or on(y + 1, x - 1) or on(y + 1, x + 1)
            # samostatny plus - okolie bez dalsich bodov
            ring = [(y + dy, x + dx) for dy in (-2, -1, 0, 1, 2) for dx in (-2, -1, 0, 1, 2) if max(abs(dy), abs(dx)) == 2]
            if plus and not corners and not any(on(a, b) for a, b in ring):
                for dy in (-1, 0, 1):
                    grid[y + dy][x] = 0
                grid[y][x - 1] = grid[y][x + 1] = 0
                for dy in (-1, 0):
                    for dx in (-1, 0):
                        grid[y + dy][x + dx] = 1
    return grid


def ink(grid):
    cols = [x for row in grid for x, bit in enumerate(row) if bit]
    rows = [y for y, row in enumerate(grid) if any(row)]
    return min(cols), max(cols), min(rows), max(rows)


def apostrophe(base):
    """ď ť ľ Ľ: pismeno s apostrofom vpravo pri hornom konci tahu (nie mäkčeň nad pismenom)."""
    grid = [row[:] for row in base]
    h, w = len(grid), len(grid[0])
    _, right, top, _ = ink(grid)
    size = max(2, h // 5)
    if right + 2 < w:
        # s medzerou vedla tahu
        x, rows = right + 2, range(top, top + size)
    else:
        # bunka je uzka (ď v pisme 6x10) - sikmo nad koniec tahu, aby nesplyval s tahom
        x, rows = w - 1, range(max(0, top - size), top)
    for y in rows:
        grid[y][x] = 1
    return grid


def encode(grid, width):
    nbytes = (width + 7) // 8
    out = []
    for row in grid:
        v = 0
        for c, bit in enumerate(row):
            if bit:
                v |= 1 << (nbytes * 8 - 1 - c)
        out.append(format(v, "0%dx" % (nbytes * 2)))
    return "".join(out)


parts = []
for name in FONTS:
    props, glyphs = parse(os.path.join(BDF, name + ".bdf"))
    width = props["bbx"][0]
    ascent, descent = props["FONT_ASCENT"], props["FONT_DESCENT"]
    missing = sorted(c for c in CHARS if c not in glyphs)
    if missing:
        print(name, "chyba", [chr(c) for c in missing], file=sys.stderr)
    cells = {c: square_dots(cell(props, glyphs[c])) for c in CHARS if c in glyphs}
    for accented, base in ((0x10F, "d"), (0x165, "t"), (0x13E, "l"), (0x13D, "L")):
        cells[accented] = apostrophe(cells[ord(base)])
    entries = ["%d:\"%s\"" % (c, encode(cells[c], width)) for c in sorted(cells)]
    parts.append('  {\n    name: "%s",\n    width: %d,\n    height: %d,\n    ascent: %d,\n    glyphs: { %s },\n  }' % (
        name, width, ascent + descent, ascent, ", ".join(entries)))
    print(name, "znakov", len(entries), "bunka", width, "x", ascent + descent, file=sys.stderr)

header = """// VYGENEROVANE z pisiem misc-fixed (X.org, Markus Kuhn; "Public domain font. Share and enjoy.") skriptom tools/pixel-fonts/bdf2ts.py -
// nemenit rucne. Znaky ASCII a CP1250; kazdy znak = riadky bunky v hex, prvy bit = lavy stlpec.

/** Bitmapove pismo s bunkou width x height bodov; ascent = riadky nad zakladnou ciarou. */
export interface PixelFontData {
  name: string;
  width: number;
  height: number;
  ascent: number;
  glyphs: Record<number, string>;
}

export const PIXEL_FONTS: PixelFontData[] = [
"""
io.open(OUT, "w", encoding="utf-8", newline="\n").write(header + ",\n".join(parts) + ",\n];\n")
