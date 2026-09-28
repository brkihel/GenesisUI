"""The "gold" art style (D-020): every UI shape of the original GenesisUI look, generated
from tools/art/gold_style.py into art/src/gold/. Kept as a selectable style (D-022); new work
goes to the style Diego chooses after evaluating both.

Do not edit the generated SVGs by hand; change the style or the shape here.
"""
import math
import os

from gold_style import (BRONZE, CONTAINER_OUTER, FILL, FILL_OPACITY, GOLD_TOP, HAIRLINE, HAIRLINE_OPACITY, INNER,
                   bead, cell_rect, container, diamond, svg, volute)



def bar_frame():
    outer = "M7,32 Q7,17 24,12.5 Q41,17 41,32 L41,142 L24,152 L7,142 Z"
    inner = "M10.2,32.5 Q10.2,20 24,16.2 Q37.8,20 37.8,32.5 L37.8,140.3 L24,148.4 L10.2,140.3 Z"
    body = (container(outer, inner)
            + volute(7.2, 27, -1, 0.95) + volute(40.8, 27, +1, 0.95)
            + bead(7, 32, 1.3) + bead(41, 32, 1.3)
            + diamond(24, 5.8, 4.6, hollow=True)
            + bead(24, 155.2, 2.4))
    return svg(48, 160, body, "Vital-bar frame (O6). 9-slice: left 10, right 10, top 32, bottom 22.")


def bar_glint():
    body = ('  <defs><radialGradient id="glint"><stop offset="0" stop-color="#FFFFFF" stop-opacity="0.85"/>'
            '<stop offset="0.38" stop-color="#FFFFFF" stop-opacity="0.35"/>'
            '<stop offset="1" stop-color="#FFFFFF" stop-opacity="0"/></radialGradient></defs>\n'
            '  <ellipse cx="12" cy="4" rx="11" ry="3.5" fill="url(#glint)"/>\n')
    return svg(24, 8, body, "Soft glint travelling along the vital-bar meniscus.")


def sprint_frame():
    outer = "M13,5 L83,5 Q89,5 89,11 L89,25 Q89,31 83,31 L13,31 Q7,31 7,25 L7,11 Q7,5 13,5 Z"
    inner = "M15,8.2 L81,8.2 Q85.8,8.2 85.8,12 L85.8,24 Q85.8,27.8 81,27.8 L15,27.8 Q10.2,27.8 10.2,24 L10.2,12 Q10.2,8.2 15,8.2 Z"
    ends = (volute(7, 18, -1, 0.8) + volute(89, 18, +1, 0.8)
            + bead(2.8, 18, 1.5) + bead(93.2, 18, 1.5))
    return svg(96, 36, container(outer, inner) + ends,
               "Sprint frame (O7). 9-slice keeps the terminal volutes fixed while the track grows.")


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


def map_mask():
    return svg(64, 64, '  <circle cx="32" cy="32" r="31.5" fill="#FFFFFF"/>\n',
               "Circular mask for the minimap pins; the terrain itself uses a circular mesh.")


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


def bar_value():
    return svg(44, 26, container("M6,2.5 L38,2.5 L42,13 L38,23.5 L6,23.5 L2,13 Z", "M7.6,5.6 L36.4,5.6 L39,13 L36.4,20.4 L7.6,20.4 L5,13 Z"),
               "Value plate carried by a vital bar.")


def card():
    return svg(64, 64, container("M8,2 L56,2 Q62,2 62,8 L62,56 Q62,62 56,62 L8,62 Q2,62 2,56 L2,8 Q2,2 8,2 Z",
                                 "M9,5.2 L55,5.2 Q58.8,5.2 58.8,9 L58.8,55 Q58.8,58.8 55,58.8 L9,58.8 Q5.2,58.8 5.2,55 L5.2,9 Q5.2,5.2 9,5.2 Z")
               + diamond(32, 2.2, 2.6), "Card panel (hover, notices). 9-slice 16.")


def boss_plate():
    return svg(96, 60, container("M16,6 L80,6 L90,30 L80,54 L16,54 L6,30 Z", "M17.8,9.2 L78.2,9.2 L86.6,30 L78.2,50.8 L17.8,50.8 L9.4,30 Z")
               + volute(6, 30, -1) + volute(90, 30, +1) + diamond(48, 5, 3.4, hollow=True), "Boss plate. 9-slice: left 28, right 28, top 18, bottom 18.")


def star():
    import math as _m
    pts = []
    for i in range(10):
        r = 7.4 if i % 2 == 0 else 3.1
        a = _m.radians(-90 + i * 36)
        pts.append(f"{8 + r * _m.cos(a):.2f},{8 + r * _m.sin(a):.2f}")
    return svg(16, 16, f'  <path d="M{" L".join(pts)} Z" fill="url(#g)" stroke="#0B0D0C" stroke-width="0.8" stroke-linejoin="round"/>\n',
               "Creature level star.")


SHAPES = {
    "bar_frame": bar_frame, "bar_glint": bar_glint, "sprint_frame": sprint_frame,
    "plate": plate, "slot": slot, "slot_active": slot_active, "tile": tile,
    "medallion": medallion, "map_ring": map_ring, "map_mask": map_mask,
    "map_crest": map_crest, "map_banner": map_banner,
    "wind_disk": wind_disk, "wind_arrow": wind_arrow, "badge_cooldown": badge_cooldown,
    "bar_value": bar_value, "card": card, "boss_plate": boss_plate, "star": star,
}


# name -> (design width, design height, 9-slice border (l, b, r, t), content insets (l, b, r, t) or None, grain)
SPRITES = {
    "bar_frame": (48, 160, (10, 22, 10, 32), (10, 24, 10, 32), None),
    "bar_glint": (24, 8, (0, 0, 0, 0), None, None),
    "sprint_frame": (96, 36, (24, 10, 24, 10), (24, 12, 24, 12), None),
    "medallion": (64, 64, (0, 0, 0, 0), None, None),
    "slot": (56, 56, (12, 12, 12, 12), None, None),
    "slot_active": (56, 56, (12, 12, 12, 12), None, None),
    "tile": (60, 60, (0, 0, 0, 0), None, None),
    "plate": (96, 72, (24, 16, 24, 16), None, None),
    "badge_cooldown": (20, 20, (0, 0, 0, 0), None, None),
    "map_ring": (250, 250, (0, 0, 0, 0), None, None),
    "map_crest": (200, 52, (0, 0, 0, 0), None, None),
    "map_banner": (180, 32, (0, 0, 0, 0), None, None),
    "map_mask": (64, 64, (0, 0, 0, 0), None, None),
    "wind_arrow": (24, 24, (0, 0, 0, 0), None, None),
    "wind_disk": (26, 26, (0, 0, 0, 0), None, None),
    "bar_value": (44, 26, (0, 0, 0, 0), (6, 6, 6, 6), None),
    "card": (64, 64, (16, 16, 16, 16), (12, 12, 12, 12), None),
    "boss_plate": (96, 60, (28, 18, 28, 18), (22, 12, 22, 12), None),
    "star": (16, 16, (0, 0, 0, 0), None, None),
}
