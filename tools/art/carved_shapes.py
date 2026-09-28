"""The "carved" art style (D-022): every UI shape in dark carved wood with iron fittings,
generated from tools/art/carved_style.py into art/src/carved/.

Standardised from Diego's hp-bars-new reference: one cap (pointed, with the engraved V of the
Valheim logo), one shaft, one diamond pommel, the same plank recipe, rivets and engravings on
every other piece. Do not edit the generated SVGs by hand.
"""
import math

from carved_style import (EDGE, IRON_DARK, IRON_LIGHT, chamfer, diamond, engrave, inlay, opening, plank, rivet,
                          rune_v, sunken, svg)


def bar_frame():
    # One continuous piece, as slender as the reference (R-040 comparison): the rails run the
    # full width of the cap and the pommel, which end in points, not in wider blocks. Drawn at
    # the bar's width ratio; the plugin scales its 9-slice borders with the bar's width, so a
    # wider bar keeps these proportions instead of stretching the cap.
    body = plank("M3,12 L18,1 L33,12 L33,147 L18,159 L3,147 Z")
    channel = opening("M8.5,31 L27.5,31 L27.5,129 L8.5,129 Z")
    rune_panel = sunken("M18,6.2 L28.2,13.4 L28.2,26.5 L7.8,26.5 L7.8,13.4 Z", 0.62) + rune_v(18, 17.6, 9.5, inlay)
    foot_panel = sunken("M8,133.5 L28,133.5 L28,145 L18,153.5 L8,145 Z", 0.62) + diamond(18, 142, 4.2, inlay)
    return svg(36, 160, body + channel + rune_panel + foot_panel,
               "Vital-bar frame: pointed rune cap, rails, pointed pommel, one piece. 9-slice: left 9, right 9, top 32, bottom 32.")


def bar_value():
    return svg(40, 30, plank(chamfer(1.5, 1.5, 37, 27, 2.5)) + sunken(chamfer(5, 5, 30, 20, 1.5), 0.55),
               "Value plate carried by a vital bar, barely wider than the bar (the number sits in the panel).")


def bar_glint():
    body = ('  <defs><radialGradient id="glint"><stop offset="0" stop-color="#FFFFFF" stop-opacity="0.85"/>'
            '<stop offset="0.38" stop-color="#FFFFFF" stop-opacity="0.35"/>'
            '<stop offset="1" stop-color="#FFFFFF" stop-opacity="0"/></radialGradient></defs>\n'
            '  <ellipse cx="12" cy="4" rx="11" ry="3.5" fill="url(#glint)"/>\n')
    return svg(24, 8, body, "Soft glint travelling along the liquid's surface.")


def sprint_frame():
    beam = plank("M10,3 L86,3 L94,12 L86,21 L10,21 L2,12 Z") + opening("M13,7.5 L83,7.5 L83,16.5 L13,16.5 Z")
    return svg(96, 24, beam + rivet(8.2, 12, 1.5) + rivet(87.8, 12, 1.5),
               "Stamina readout beam. 9-slice: left 16, right 16, top 10, bottom 10.")


def plate():
    beam = plank(chamfer(9, 5, 78, 62, 7)) + opening(chamfer(13.5, 9.5, 69, 53, 4))
    ends = plank("M9,26 L2,36 L9,46 Z") + plank("M87,26 L94,36 L87,46 Z")
    rivets = rivet(15, 7.4) + rivet(81, 7.4) + rivet(15, 64.6) + rivet(81, 64.6)
    return svg(96, 72, beam + ends + rivets, "Hotbar beam. 9-slice: left 24, right 24, top 16, bottom 16.")


def slot():
    return svg(56, 56, sunken(chamfer(2, 2, 52, 52, 7)), "Item slot. 9-slice border 12.")


def slot_active():
    path = chamfer(2, 2, 52, 52, 7)
    glow = "".join(f'  <path d="{path}" fill="none" stroke="#F2B640" stroke-opacity="{op}" stroke-width="{w}" stroke-linejoin="round"/>\n'
                   for w, op in ((7, 0.07), (5, 0.12), (3.2, 0.22)))
    return svg(56, 56, glow + f'  <path d="{path}" fill="none" stroke="#F6C766" stroke-width="1.8" stroke-linejoin="round"/>\n',
               "Selected / equipped slot overlay: ember-gold edge. 9-slice border 12.")


def tile():
    cell = sunken(chamfer(2, 8, 56, 50, 7))
    cap = plank("M22,10 L22,5 L30,0.8 L38,5 L38,10 Z") + diamond(30, 5.6, 1.9)
    return svg(60, 60, cell + cap, "Status tile with a small rune cap. Fixed size.")


def card():
    return svg(64, 64, plank(chamfer(1.5, 1.5, 61, 61, 8)) + opening(chamfer(6, 6, 52, 52, 5))
               + rivet(6.6, 6.6, 1.5) + rivet(57.4, 6.6, 1.5) + rivet(6.6, 57.4, 1.5) + rivet(57.4, 57.4, 1.5),
               "Card panel (hover, notices). 9-slice 16; contents inside the opening.")


def boss_plate():
    beam = plank("M16,4 L80,4 L92,30 L80,56 L16,56 L4,30 Z") + opening("M19,9 L77,9 L85.5,30 L77,51 L19,51 L10.5,30 Z")
    cap = plank("M42,6 L42,1 L48,-2.5 L54,1 L54,6 Z") + rivet(9.5, 30, 1.8) + rivet(86.5, 30, 1.8)
    return svg(96, 60, beam + cap, "Boss plate. 9-slice: left 28, right 28, top 18, bottom 18.")


def star():
    pts = []
    for i in range(10):
        r = 7.4 if i % 2 == 0 else 3.1
        a = math.radians(-90 + i * 36)
        pts.append(f"{8 + r * math.cos(a):.2f},{8 + r * math.sin(a):.2f}")
    return svg(16, 16, f'  <path d="M{" L".join(pts)} Z" fill="#E7C064" stroke="{EDGE}" stroke-width="0.9" stroke-linejoin="round"/>\n',
               "Creature level star.")


def map_ring():
    c = 125
    ring = (f'  <path d="M{c},{c - 123} A123,123 0 1 1 {c - 0.01},{c - 123} Z M{c},{c - 111} A111,111 0 1 0 {c + 0.01},{c - 111} Z" '
            f'fill="url(#wood)" fill-rule="evenodd" stroke="{EDGE}" stroke-width="2.2"/>\n'
            f'  <circle cx="{c}" cy="{c}" r="122" fill="none" stroke="url(#lit)" stroke-width="1.1"/>\n'
            f'  <circle cx="{c}" cy="{c}" r="111.6" fill="none" stroke="url(#cut)" stroke-width="1.3"/>\n')
    for a in (0, 90, 180, 270):
        ring += rivet(round(c + math.cos(math.radians(a)) * 117, 1), round(c + math.sin(math.radians(a)) * 117, 1), 2.6)
    for a in (45, 135, 225, 315):
        ring += diamond(round(c + math.cos(math.radians(a)) * 117, 1), round(c + math.sin(math.radians(a)) * 117, 1), 2.6)
    return svg(250, 250, ring, "Minimap ring: carved wooden ring, iron rivets on the cardinal points. Window radius 111.")


def map_mask():
    return svg(64, 64, '  <circle cx="32" cy="32" r="31.5" fill="#FFFFFF"/>\n', "Circular window shape (not drawn).")


def map_crest():
    arc = plank("M12,48 C36,4 164,4 188,48 C176,52 162,52 150,47 C128,39 72,39 50,47 C38,52 24,52 12,48 Z")
    return svg(200, 52, arc + rivet(18.5, 44, 1.8) + rivet(181.5, 44, 1.8) + diamond(100, 12.5, 2.6),
               "Minimap crest: carved arc on the ring holding wind and day/time.")


def map_banner():
    return svg(180, 32, plank("M18,4 L162,4 L174,16 L162,28 L18,28 L6,16 Z") + rivet(13, 16, 1.6) + rivet(167, 16, 1.6),
               "Biome banner under the ring.")


def wind_disk():
    return svg(26, 26, sunken("M13,1.6 A11.4,11.4 0 1 1 12.99,1.6 Z"), "Wind disk: a carved round cell for the wind arrow.")


def wind_arrow():
    body = (f'  <path d="M12,1.8 L18.4,15.6 L12,12.4 L5.6,15.6 Z" fill="url(#iron)" stroke="{EDGE}" stroke-width="0.9" stroke-linejoin="round"/>\n'
            f'  <path d="M12,12.4 L12,22" stroke="{IRON_LIGHT}" stroke-width="2" stroke-linecap="round"/>\n')
    return svg(24, 24, body, "Wind arrow in iron.")


def badge_cooldown():
    body = ('  <circle cx="10" cy="10" r="8.6" fill="#0B0D0C" fill-opacity="0.95" stroke="#E0643C" stroke-width="1.4"/>\n'
            '  <path d="M6.6,6.6 L13.4,13.4 M13.4,6.6 L6.6,13.4" stroke="#E23B2E" stroke-width="2" stroke-linecap="round"/>\n')
    return svg(20, 20, body, "Cooldown badge: dark disc, ember ring, red cross.")


SHAPES = {
    "bar_frame": bar_frame, "bar_value": bar_value, "bar_glint": bar_glint, "sprint_frame": sprint_frame,
    "plate": plate, "slot": slot, "slot_active": slot_active, "tile": tile, "card": card,
    "boss_plate": boss_plate, "star": star, "map_ring": map_ring, "map_mask": map_mask,
    "map_crest": map_crest, "map_banner": map_banner, "wind_disk": wind_disk, "wind_arrow": wind_arrow,
    "badge_cooldown": badge_cooldown,
}

# name -> (design width, design height, 9-slice border (l, b, r, t), content insets (l, b, r, t) or None, grain)
# grain: "v" / "h" wood-grain direction, or None for pieces that are not wood.
SPRITES = {
    "bar_frame": (36, 160, (9, 32, 9, 32), (9, 31, 9, 31), "v"),
    "bar_value": (40, 30, (0, 0, 0, 0), (6, 6, 6, 6), "h"),
    "bar_glint": (24, 8, (0, 0, 0, 0), None, None),
    "sprint_frame": (96, 24, (16, 10, 16, 10), (14, 8, 14, 8), "h"),
    "plate": (96, 72, (24, 16, 24, 16), None, "h"),
    "slot": (56, 56, (12, 12, 12, 12), None, "v"),
    "slot_active": (56, 56, (12, 12, 12, 12), None, None),
    "tile": (60, 60, (0, 0, 0, 0), None, "v"),
    "card": (64, 64, (16, 16, 16, 16), (12, 12, 12, 12), "h"),
    "boss_plate": (96, 60, (28, 18, 28, 18), (22, 12, 22, 12), "h"),
    "star": (16, 16, (0, 0, 0, 0), None, None),
    "map_ring": (250, 250, (0, 0, 0, 0), None, "h"),
    "map_mask": (64, 64, (0, 0, 0, 0), None, None),
    "map_crest": (200, 52, (0, 0, 0, 0), None, "h"),
    "map_banner": (180, 32, (0, 0, 0, 0), None, "h"),
    "wind_disk": (26, 26, (0, 0, 0, 0), None, "v"),
    "wind_arrow": (24, 24, (0, 0, 0, 0), None, None),
    "badge_cooldown": (20, 20, (0, 0, 0, 0), None, None),
}
