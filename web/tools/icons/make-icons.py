# Ikony aplikacie (PWA, plocha Androidu a iOS) podla public/favicon.svg: tabula s bodmi.
# Spustenie z web/: python tools/icons/make-icons.py (potrebuje Pillow).
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PUBLIC = os.path.join(HERE, "..", "..", "public")
SS = 8  # kreslenie vo vacsom a zmensenie = hladke okraje

BOARD = (0x15, 0x18, 0x1D, 255)
AMBER = (0xFF, 0xB0, 0x00, 255)
GREEN = (0x46, 0xE0, 0x3A, 255)


def logo(draw, x0, y0, unit, board=True):
    """Kresba favicon.svg (mriezka 32 x 32) s lavym hornym rohom x0, y0 a mierkou unit."""
    u = lambda v: v * unit
    if board:
        draw.rounded_rectangle([x0 + u(1), y0 + u(5), x0 + u(31), y0 + u(27)], radius=u(4), fill=BOARD)
    for cx in (8, 13, 18):
        draw.ellipse([x0 + u(cx - 2), y0 + u(10), x0 + u(cx + 2), y0 + u(14)], fill=AMBER)
    for cx in (8, 13, 18, 23):
        draw.ellipse([x0 + u(cx - 2), y0 + u(18), x0 + u(cx + 2), y0 + u(22)], fill=GREEN)


def icon(size, maskable):
    big = size * SS
    image = Image.new("RGBA", (big, big), BOARD if maskable else (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if maskable:
        # cela plocha je pozadie, kresba v bezpecnej zone (stred 70 %)
        scale = big * 0.7 / 32
        logo(draw, (big - 32 * scale) / 2, (big - 32 * scale) / 2, scale, board=False)
    else:
        logo(draw, 0, 0, big / 32)
    return image.resize((size, size), Image.LANCZOS)


for name, size, maskable in (("icon-192.png", 192, False), ("icon-512.png", 512, False), ("icon-maskable-512.png", 512, True),
                             ("apple-touch-icon.png", 180, True)):
    icon(size, maskable).save(os.path.join(PUBLIC, name), optimize=True)
    print(name)
