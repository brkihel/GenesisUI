"""Writes every UI shape SVG in art/src from the shared style (tools/art/style.py).

    tools/.venv/bin/python tools/art/shapes.py     (render.py runs it first)

Do not edit the generated art/src/*.svg by hand; change the style or the shape here.
"""
import math
import os

from style import (BRONZE, CONTAINER_OUTER, FILL, FILL_OPACITY, GOLD_TOP, HAIRLINE, HAIRLINE_OPACITY, INNER,
                   bead, cell_rect, container, diamond, svg, volute)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "art", "src")


def bar_frame():
    outer = "M7,32 Q7,17 24,12.5 Q41,17 41,32 L41,142 L24,152 L7,142 Z"
    inner = "M10.2,32.5 Q10.2,20 24,16.2 Q37.8,20 37.8,32.5 L37.8,140.3 L24,148.4 L10.2,140.3 Z"
    body = (container(outer, inner)
            + volute(7.2, 27, -1, 0.95) + volute(40.8, 27, +1, 0.95)
            + bead(7, 32, 1.3) + bead(41, 32, 1.3)
            + diamond(24, 5.8, 4.6, hollow=True)
            + bead(24, 155.2, 2.4))
    return svg(48, 160, body, "Vital-bar frame (O6). 9-slice: left 10, right 10, top 32, bottom 22.")


def plate():
    outer = "M16,6 L80,6 Q86,6 86,12 L86,60 Q86,66 80,66 L16,66 Q10,66 10,60 L10,12 Q10,6 16,6 Z"
    inner = "M17,9.2 L79,9.2 Q82.8,9.2 82.8,13 L82.8,59 Q82.8,62.8 79,62.8 L17,62.8 Q13.2,62.8 13.2,59 L13.2,13 Q13.2,9.2 17,9.2 Z"
    # Terminal ends: a volute pair and a bead on each side, the same motif as bars and crest.
    ends = (volute(10, 32, -1) + volute(10, 40, -1, downward=True) + bead(4.2, 36, 1.8)
            + volute(86, 32, +1) + volute(86, 40, +1, downward=True) + bead(91.8, 36, 1.8))
    return svg(96, 72, container(outer, inner) + ends,
               "Hotbar plate (O7). 9-slice: left 24, right 24, top 16, bottom 16; ornaments sit in the fixed side borders.")


def slot():
    return svg(56, 56, cell_rect(1.5, 1.5, 53, 53, 4), "Item slot (C1). 9-slice border 12.")


def slot_active():
    glow = "".join(f'  <rect x="1.5" y="1.5" width="53" height="53" rx="4" fill="none" stroke="{GOLD_TOP}" '
                   f'stroke-opacity="{op}" stroke-width="{w}"/>\n' for w, op in ((7, 0.06), (5, 0.1), (3.4, 0.18)))
    return svg(56, 56, glow + '  <rect x="1.5" y="1.5" width="53" height="53" rx="4" fill="none" stroke="url(#g)" stroke-width="2"/>\n',
               "Selected / equipped slot overlay (C1). 9-slice border 12, transparent centre.")


def tile():
    return svg(60, 60, cell_rect(2, 5, 56, 53, 7) + diamond(30, 4.6, 4, hollow=True), "Status tile (C8). Fixed size.")


def medallion():
    c, r = 32, 27
    body = (f'  <circle cx="{c}" cy="{c}" r="{r}" fill="{FILL}" fill-opacity="{FILL_OPACITY}" stroke="url(#g)" stroke-width="{CONTAINER_OUTER}"/>\n'
            f'  <circle cx="{c}" cy="{c}" r="{r - 3.4}" fill="none" stroke="{BRONZE}" stroke-width="{INNER}"/>\n')
    for x, y in ((c, 4.2), (c, 59.8), (4.2, c), (59.8, c)):
        body += diamond(x, y, 3)
    cx, cy, R = 32.0, 33.5, 14.0
    tips = [(cx + R * math.cos(math.radians(a)), cy + R * math.sin(math.radians(a))) for a in (-90, 30, 150)]
    lens = "".join(f'<path d="M{cx:.2f},{cy:.2f} A{R},{R} 0 0 1 {x:.2f},{y:.2f} A{R},{R} 0 0 1 {cx:.2f},{cy:.2f}Z"/>' for x, y in tips)
    body += f'  <g fill="none" stroke="url(#g)" stroke-width="1.8" stroke-linejoin="round">{lens}<circle cx="{cx}" cy="{cy}" r="6.8"/></g>\n'
    return svg(64, 64, body, "Knot medallion (O5) under the vital bars.")


def map_ring():
    c = 125
    body = (f'  <circle cx="{c}" cy="{c}" r="118" fill="none" stroke="{FILL}" stroke-opacity="{FILL_OPACITY}" stroke-width="9"/>\n'
            f'  <circle cx="{c}" cy="{c}" r="122" fill="none" stroke="url(#g)" stroke-width="{CONTAINER_OUTER + 0.2}"/>\n'
            f'  <circle cx="{c}" cy="{c}" r="113.4" fill="none" stroke="{BRONZE}" stroke-width="{INNER + 0.3}"/>\n'
            f'  <circle cx="{c}" cy="{c}" r="111.2" fill="none" stroke="{GOLD_TOP}" stroke-opacity="{HAIRLINE_OPACITY}" stroke-width="{HAIRLINE}"/>\n')
    for a in (0, 90, 180, 270):
        body += diamond(round(c + math.cos(math.radians(a)) * 118, 1), round(c + math.sin(math.radians(a)) * 118, 1), 4.2, hollow=True)
    for a in (45, 135, 225, 315):
        body += bead(round(c + math.cos(math.radians(a)) * 118, 1), round(c + math.sin(math.radians(a)) * 118, 1), 1.5)
    return svg(250, 250, body, "Minimap ring (C11). Map window radius 111 of 125.")


def map_crest():
    outer = "M14,46 C38,4 162,4 186,46 C172,50 160,50 148,46 C126,40 74,40 52,46 C40,50 28,50 14,46 Z"
    inner = "M22.5,42.6 C46,10.5 154,10.5 177.5,42.6"
    body = (container(outer, inner) + volute(14, 46, -1, 1.0) + volute(186, 46, +1, 1.0)
            + diamond(100, 5, 3.8, hollow=True))
    return svg(200, 52, body, "Minimap crest: arc plate on the ring for wind and day/time. Fixed size.")


def map_banner():
    outer = "M18,4 L162,4 L172,16 L162,28 L18,28 L8,16 Z"
    inner = "M19.6,7.2 L160.4,7.2 L167.8,16 L160.4,24.8 L19.6,24.8 L12.2,16 Z"
    return svg(180, 32, container(outer, inner) + bead(3.6, 16, 1.8) + bead(176.4, 16, 1.8), "Biome banner under the ring. Fixed size.")


def wind_disk():
    body = (f'  <circle cx="13" cy="13" r="11.6" fill="{FILL}" fill-opacity="{FILL_OPACITY}" stroke="{BRONZE}" stroke-width="1.4"/>\n'
            f'  <circle cx="13" cy="13" r="9.2" fill="none" stroke="{GOLD_TOP}" stroke-opacity="{HAIRLINE_OPACITY}" stroke-width="{HAIRLINE}"/>\n')
    return svg(26, 26, body, "Wind disk: the cell that holds the wind arrow on the crest.")


def wind_arrow():
    body = ('  <path d="M12,1.8 L18.4,15.6 L12,12.4 L5.6,15.6 Z" fill="url(#g)" stroke="#0B0D0C" stroke-width="0.8" stroke-linejoin="round"/>\n'
            '  <path d="M12,12.4 L12,22" stroke="url(#g)" stroke-width="2" stroke-linecap="round"/>\n')
    return svg(24, 24, body, "Wind arrow; rotated like vanilla's wind marker.")


def badge_cooldown():
    body = ('  <circle cx="10" cy="10" r="8.6" fill="#0B0D0C" fill-opacity="0.95" stroke="#E0643C" stroke-width="1.4"/>\n'
            '  <path d="M6.6,6.6 L13.4,13.4 M13.4,6.6 L6.6,13.4" stroke="#E23B2E" stroke-width="2" stroke-linecap="round"/>\n')
    return svg(20, 20, body, "Cooldown badge: dark disc, ember ring, red cross.")


SHAPES = {
    "bar_frame": bar_frame, "plate": plate, "slot": slot, "slot_active": slot_active, "tile": tile,
    "medallion": medallion, "map_ring": map_ring, "map_crest": map_crest, "map_banner": map_banner,
    "wind_disk": wind_disk, "wind_arrow": wind_arrow, "badge_cooldown": badge_cooldown,
}


def main():
    for name, make in SHAPES.items():
        with open(os.path.join(SRC, name + ".svg"), "w") as f:
            f.write(make())
    print(f"{len(SHAPES)} shapes written to art/src from tools/art/style.py")


if __name__ == "__main__":
    main()
