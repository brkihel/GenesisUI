# GenesisUI — Art Direction

Source: the 12 concept images in Diego's local folder `~/GenesisUI-Concept/`
(1672×941 each) plus his font note. The full concepts stay outside the repository.
Diego owns them and approved cleaned UI cutouts for F4 (D-026). Generated shapes remain
available where they give a cleaner result. Everything below was studied from the concepts
on 2026-09-28; colors were sampled from pixels.

## 1. Mood

Dark, warm, crafted. Near-black panels framed in thin aged gold, Celtic/Norse
knotwork at corners and caps, parchment-white titles in wide-tracked capitals,
italic gold flavour text. The world stays the hero: the HUD hugs the edges and
leaves the center clear.

## 2. Color tokens

| Token | Value | Seen in |
|---|---|---|
| `bg.panel` | `#151816` at 92% (player-adjustable transparency) | all windows |
| `bg.header` | `#151718` | top navigation bar |
| `bg.slot` | `#141614` | inventory and hotbar cells |
| `bg.footer` | `#030302` at 85% | key-hint footer |
| `line.frame` | `#705B3F` | panel borders (1px) |
| `line.slot` | `#474538` | cell borders |
| `accent.gold` | `#FACF72` | selected slot border, active highlights |
| `accent.goldBright` | `#F7E283` | active tab label, section labels |
| `accent.goldFill` | `#D09D4B` → `#E6BE57` | weight/XP/slider fills |
| `text.title` | `#FBF3DA` | panel titles |
| `text.subtitle` | `#E6D8B2` | counters ("32/48 SLOTS EM USO") |
| `text.body` | `#D5D2CA` | descriptions, list labels |
| `text.muted` | `#EDE8DD` at 70% | inactive tabs |
| `text.flavor` | `#BA995C` | italic lore lines |
| `text.itemName` | `#FCCA55` | item title |
| `rarity.exceptional` | `#EF8820` | "Qualidade Excepcional" |
| `state.positive` | `#A2DC88` | effect bonuses, XP arc |
| `state.toggleOn` | `#6DAF50` | switches |
| `bar.health` | `#97181A` (fill), brighter top highlight | vitals, boss bar |
| `bar.stamina` | `#AA710F` → `#C6A576` | vitals, sprint bar |
| `bar.eitr` | `#2281AD` → `#1897CC` | vitals |
| `bar.durability` | `#6DAF50` | 2px bar under slots |

These are the theme tokens (`ThemeTokens.cs`). Contrast of body text on `bg.panel`
against WCAG AA has not been measured for the shipped window text panels.

## 3. Typography

From Diego's note, confirmed against the concepts:

| Role | Font | Use |
|---|---|---|
| `font.display` | **Cinzel SemiBold**, all caps, tracking ≈ +12% | panel titles (INVENTÁRIO, EQUIPAMENTO), VIDA, VIGOR, creature names |
| `font.label` | **Cinzel Medium**, all caps, tracking ≈ +8% | tabs, buttons, categories, section labels |
| `font.body` | **Cormorant Garamond Medium / SemiBold** | descriptions, tooltips, lore, secondary text; italic for flavour |

Both families are SIL OFL 1.1 (Google Fonts); the license files ship with the fonts.
Cinzel is also the GenesisMods site display face, which keeps the line consistent.
Numbers on the HUD (bar values, timers, slot counts) use Cinzel (the display face), which
stayed legible at HUD sizes in every client test so far; Cormorant carries labels and body.

## 4b. Vital-bar effects

Tinted per bar. The liquid is calm and rich: large soft dark
clouds (`mottle`) and **a few small crack fragments** (`veins`) in a darker shade of the liquid at
30 %, so they sit inside it, never lit on top; everything drifts slowly (R-040: long light cracks
read as one continuous line). A short **burn** edge (at most 9 units, in the bar's hot colour)
sits on the surface while the value drains, with **embers** rising; it never grows into a long
trail. At low health the **frame pulses red** and embers leave the bar's sides (R-040: a halo
around the bar was too weak and drew a red box). A small
value plate, barely wider than the bar, carries the number at 26 % of the bar height;
`[Vitals] ShowValues` hides it.
Hot colours: health orange `(1, .62, .26)`, stamina pale gold `(1, .93, .62)`, eitr ice
`(.72, .95, 1)`.

## 4a. One ornament language (the style system)

The only art direction (D-023). R-030: every piece looked good alone but they did not fit
together. Generated shapes use `tools/art/shapes.py` and `tools/art/style.py`; F4 concept
cutouts and their review process follow D-026. Never hand-edit a generated SVG; change the
style or the shape function.

| Rule | Value |
|---|---|
| Containers (vital bars, hotbar plate, minimap ring, crest, banner, medallion) | gold gradient outer line 1.6, bronze `#705B3F` inner line 0.9 at 3.2 inside |
| Cells (item slots, status tiles, wind disk) | bronze outer line 1.4, gold hairline 0.8 at 20 % inside |
| Fill | `#0B0D0C` at 90 % everywhere |
| Motifs | only three: diamond (finials, cardinal points), volute (terminal ends), bead (tips, joints) |
| Motion inside elements | larger bubbles at 14–15 % opacity rising slowly with slight sway; small travelling glint at the liquid surface |

Delicate, subtle, refined and memorable; never as busy as the concept art (Diego).

## 4. Ornament and component catalogue

Each item becomes a generated SVG in `art/src/` and a sprite (often 9-sliced) listed in
`art/out/sprites.json`. Numbers refer to the concept images.

| # | Element | Description | Concepts |
|---|---|---|---|
| O1 | Corner filigree | knotwork scroll in the four corners of every panel; one source, mirrored | 1, 2, 7, 9, 11, 12 |
| O2 | Frame line | 1px `line.frame` border with 1px dark inner line | all windows |
| O3 | Title flourish | small scroll before a panel title | 2, 7, 9 |
| O4 | Diamond divider | `◆` between thin lines; used under titles and around section names | 2, 5 (VIGOR), 11 |
| O5 | Knot medallion | triquetra-in-triangle mark; product logo, level badge, bar end caps | 1, 3, 4, 5, 6 |
| O6 | Bar frame | ornate vertical frame with medallion cap for health/stamina/eitr | 4, 5, 6, 8, 10 |
| O7 | Plate caps | knot ends on horizontal plates (boss bar, sprint bar, hotbar) | 5, 6 |
| O7a | Sprint frame | a thin horizontal container above the hotbar, visible while running and briefly after | 5 |
| O8 | Panel watermark | large knot at low alpha in panel corners | 2, 7, 9 |
| C1 | Slot | dark square, `line.slot` border; index top-left, quantity bottom-right, 2px durability bar; selected = `accent.gold` border + soft glow | 3–10 |
| C2 | Key cap | rounded rectangle outline with the key; mouse glyphs for buttons | footers, 2, 8, 10 |
| C3 | Footer hint bar | full-width bar of `C2` + action label, separated by small ornaments | 1, 2, 7, 9, 11, 12 |
| C4 | Top navigation | logo + product name, Q/E cycle keys, icon-over-label tabs, active tab gold with underline glow | 1, 2, 7, 9, 11, 12 |
| C5 | Filter pill | rounded chip, gold outline when active | 11, 12 |
| C6 | Toggle / slider / dropdown | green toggle, gold slider fill with light knob, outlined dropdown | 2 |
| C7 | Buttons | primary: gold outline + inner glow ("APLICAR", "CRIAR"); secondary: dark outline | 2, 12 |
| C8 | Status tile | dark rounded square with icon, label and timer beneath | 4, 5, 6, 8, 10 |
| C9 | Hover card | framed card with icon, name, primary action key and a pointer diamond to the target | 8, 10 |
| C10 | Stat list | label left, value right, thin separators; durability as inline bar | 7, 9, 12 |
| C11 | Circular minimap | round map with gold ring, biome name plate, N marker, wind and day/time crest | 3–6, 8, 10 |

Nav and UI icons are line icons in `accent.gold`: we draw them or take them from an
MIT/ISC set (Lucide or Tabler), recorded in `art/LICENSES.md`. Item icons are always
the game's own.

## 5. Motion

Short and quiet: 120 ms fades, 80 ms highlight transitions, a soft glow pulse
(≤ 1.5 s) only for "attention" states such as low health. No bounces and no
movement that fights the camera.

## 6. Screen-by-screen feasibility

What each concept shows, and what we can build in the visual, client-only scope.

| Concept | Screen | Buildable in visual scope | Needs a data provider (API/adapter) | Out of scope / later |
|---|---|---|---|---|
| 3, 4, 5, 6, 8, 10 | HUD | vertical health/stamina/eitr bars, hotbar, status tiles with timers, circular minimap frame, biome name, wind, day/time, sprint bar, boss plate with stars, interaction card | player level badge ("NÍVEL 12"): vanilla has no player level | chest card "12/24 espaços usados" before opening: container contents are not reliably on the client until opened; research item |
| 3 | HUD (horizontal variant) | long health bar with segmented food pips | — | — |
| 7, 9 | Inventory | grid of any size, equipment panel for vanilla slots (helmet, chest, legs, cape, utility, trinket, weapons, ammo), item details, weight bar, total armor | amulet/ring/gloves/boots resource bindings (Jewelcrafting and similar mods) | Slot layout/quick-use/action moved to F4 (D-028); character/item 3D previews and native equip border shipped in 1.1.2 |
| 1 | Skills ("Habilidades") | vanilla skills (including skills from mods) with level and XP bar, grouped | skill trees, points, attributes (Força, Destreza, Intelecto): no vanilla system | — |
| 1 | Texts ("Textos") | vanilla texts/lore list with categories | — | — |
| 2 | Settings | all GenesisUI options; live HUD preview is a later refinement | — | — |
| 11 | World map | frame, filter pills by pin type, marker palette, legend, zoom and center buttons, "visible to others" toggle (vanilla) | custom pin categories from mods | painterly map texture (see GenesisMapPrinter later), fast travel (gameplay) |
| 12 | Crafting / Build | recipe list, search, category pills, details, requirements with have/need, craft through vanilla, build piece browser | — | crafting from chests (gameplay, F8) |
| 1, 2 | "Conquistas" tab | native achievement lists, unlocked progress, earned trophies and details | additional foreign achievement data beyond native lists may need a future provider | — |

When a provider is missing, the element is **hidden**, not shown empty (GenesisMods
rule: never an empty panel).
