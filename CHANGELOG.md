# Changelog

## 0.6.0-preview.1 — F4.0: Diego's textures on the whole HUD

### Changed
- Every HUD frame now comes from Diego's isolated texture sheets (D-027): the three vital
  bars (three sizes, as drawn), the stamina readout, the eight-cell hotbar, food slots,
  status tiles, the interaction card, notices, boss and creature plates, the minimap ring,
  its day/time and biome plates, and the vitals medallion. Nothing is stretched except plain
  straight rails; the card's side diamonds and the crest's top diamond are separate pieces.
- Health, stamina and eitr are filled with Diego's liquids, two layers drifting at different
  paces inside each frame's opening; the burn, embers, surface glint and low-health pulse stay.
  The stamina readout and the boss bar use the liquids too, and the readout shows the number.
- The selected hotbar cell glows from inside instead of drawing a second frame.

### Added
- The dark stone panel material behind every frame, clipped to the frame's silhouette, with
  its own opacity: `[Backgrounds] Default` and one override per panel (-1 = default). Only
  the material fades; frames, texts and icons stay.
- F8 panel: sprite and live background counts.

## 0.5.0-preview.4 — R-040 fixes; F3 approved

### Added
- Top-left notices stack up to three, newest on top; each fades while dropping after 4 s,
  and a repeat of the top one updates it (as vanilla merges pickups).

### Fixed
- Vital bars striped after a fault injection and retry: the host now caps the elapsed time a
  module receives at 0.25 s (a fresh build passed `float.MaxValue`, which turned animation
  clocks into NaN).
- The interaction card stayed over the open large map (Vegvísir): it hides while the map is
  open, and the HUD root is kept below the large map.
- Low health drew a red box around the health bar: the halo is gone; the frame pulses red.

### Changed
- The burn on a draining bar is a short bright edge instead of a trail that kept growing.
- Cracks in the liquid are a few small fragments in a darker shade of the liquid.

## 0.5.0-preview.3 — gold only, creature plates, F3 complete

### Removed
- The `carved` art style, the `[Theme] Style` option and the style selector (D-023). Gold is
  the only art direction; art ships flat in `plugins/art/`. An old `[Theme] Style` line in
  the config file is ignored.

### Added
- **Creature plates** (`hud.enemy`): over regular creatures, a small plate in the boss
  plate's language with name, 0–2 stars, health with a hot trail (green for tamed/friendly),
  and vanilla's aware "?" / alerted "!" marks. Each lives inside vanilla's own plate, so it
  follows vanilla's position and visibility exactly and disappears with it. `[Modules] Enemy`,
  `[Enemy] OffsetY`.

### Changed
- Gold vital bars keep the calmer liquid (soft clouds, a few long thin cracks) and the compact
  value plate from preview.2; bar sizes and the food position are back to the gold layout.
- The stamina readout scales its whole frame to its small height instead of squashing it.
- The F8 panel is taller and sums creature-plate veils in one line; restoring a module's
  veils logs one summary line.

## 0.5.0-preview.2 — carved bars reworked from the reference comparison

### Changed
- Carved vital bars are one slender piece: pointed rune cap and pommel as wide as the rails,
  a light V and diamond in recessed panels, a lighter weathered wood with worn edges, chips
  and a bevelled channel; the empty part is dark brown, not black. The frame now scales its
  borders with the bar's width (both styles), so the cap never stretches. Bars are slimmer
  (health 40, stamina and eitr 34 wide); food moves to `OffsetX` 176 by default.
- Liquid in every bar is calmer: large soft clouds and a few long, thin cracks instead of
  dense veins; bubbles fainter.
- The value plate is barely wider than the bar.

### Added
- `[Vitals] ShowValues`: hide the numbers for clean bars.

## 0.5.0-preview.1 — F3 complete, carved-wood style

### Added
- **Selectable art styles** (D-022): `[Theme] Style` = `carved` (carved wood with iron
  rivets and a V rune, the new default) or `gold` (the filigree style), switched live.
  Both ship; sprite sizes, 9-slice borders and content insets come from each style's
  `sprites.json`.
- **Boss plate** top centre: name, stars, health with a hot trail; replaces vanilla's boss
  bar (`hud.boss`, a dynamic region veiled as vanilla creates it).
- **Interaction card** beside the crosshair, mirroring vanilla's hover text and fade.
- **Notifications**: top-left card with icon and the large centre message, mirroring
  vanilla's queue, timing and fade.
- **Key hints** lifted above the hotbar (`[Hotbar] KeyHintsLift`) through `VanillaNudge`,
  restored exactly when the hotbar module stops or faults.
- Vital-bar effects in every bar's own colour: drifting veins and clots inside the liquid,
  a burn band with rising embers over the part just lost, a soft danger glow with embers
  at low health, and a value plate for the number.

### Changed
- The stamina bar above the hotbar is much smaller (220 x 22), appears whenever stamina is
  spent (not only running) and fades only after stamina is full.
- Package layout: `plugins/art/<style>/` per style.

## 0.4.1-preview.2 — R-031 follow-up, F3 sprint

### Changed
- The minimap terrain now uses a circular UI mesh with the live vanilla map material;
  only the pin images use a stencil mask. The old stencil material copy could leave
  the terrain grey even after following `minimap(Clone)`. The client test still needs
  to confirm this candidate fix. The minimap log now names its map textures.
- Vital-bar bubbles are larger and more translucent (14–15 % tint alpha). A small
  reflection moves along the liquid surface and brightens briefly when the value
  changes.

### Added
- A temporary horizontal sprint bar above the hotbar, using vanilla running and
  stamina values. It fades after running stops and has its own module toggle, offset
  and scale. It has no gameplay action or vanilla region to veil.

## 0.4.1-preview.1 — R-030 feedback

### Fixed
- **Minimap turned grey after a while**: vanilla's `Minimap.Start` replaces the small-map
  material and only then sets its textures; the module could be built before that and keep
  the old material. It now follows vanilla's live material every frame. Also stops copying
  `_zoom`, `_pixelSize`, `_mapCenter`, which the map shader does not declare (Unity errors).
- **Guardian-power cooldown stayed on a ready tile** after the cooldown was reset: the tile
  now really clears the clock text.
- Numbers inside the bars, slot corners and tile names shrink to fit instead of touching the
  frames.

### Changed
- **One ornament language** (D-020): every shape is generated from `tools/art/style.py`.
  Containers share a gold outer and bronze inner line; slots, status tiles and the wind disk
  share a bronze line with a gold hairline; the hotbar plate ends now use the same volutes
  and beads as the bars and crest.
- Bars: the glass is softer (no hard white stripe), and the living detail is now **subtle
  rising bubbles** with a slight sway (about 20 % opacity).
- Wind: the arrow sits in its own small disk on the crest, larger and always clearly visible;
  stronger wind makes it fuller.

## 0.4.0-preview.1 — Round minimap (includes 0.3.1)

### Added
- **Minimap module**: round map at the top-right in a gold ring; a crest on top of the ring
  holds the wind arrow (vanilla direction, opacity by strength) and "Dia N · HH:MM"; the
  biome name sits on a banner below. The map mirrors vanilla's small map (same fog of war,
  zoom, pins — including pins from other mods — player and ship markers; D-019). Hidden
  with the large map and in no-map worlds, like vanilla.
- Status effects now start below the minimap (`[Status] OffsetY` 340).

## 0.3.1-preview.1 — R-020 feedback (not delivered separately)

### Changed
- **Vital bars v2**: slimmer frame with small volutes on the arch shoulders, gold dots and a
  banner point with a bead at the bottom; the value is now a living liquid: glass shading,
  a bright surface line, and a seamless flow pattern rising slowly inside (calm for
  health, livelier for stamina, a counter-drifting shimmer for eitr). Health stays the
  widest and tallest bar.
- **Guardian power is one tile**: steady gold when ready; pulsing gold with the effect's
  time while active (no second tile); dimmed with a small red cross and the cooldown after.

### Fixed
- Vanilla leftovers under our bars: the three empty food slot frames, and the remaining
  decoration of the vanilla health panel (new region `hud.healthDecor`).

### Added
- Diagnostic report: tree of the vanilla health panel, marking what GenesisUI veils.
- Sprite manifest `wrap` (`clamp`/`repeat`) for scrolling textures; `tools/art/patterns.py`.


## 0.3.0-preview.1 — F3 part 1: food, hotbar, status effects; art fix

### Fixed
- **Sprites did not load in 0.2.0-preview.1** (`art/sprites.json rejected: no sprites`):
  Unity's JsonUtility left the sprite list empty without an error. Data files are now read
  by our own strict JSON reader in Core (D-018), and a build-time test runs the shipped
  manifest and PNGs through the same code the game uses.

### Added
- **Food module**: three framed slots next to the vital bars; food icon, time left written
  exactly like vanilla ("12m", blinking "45s"), a bar of what is left, the vanilla pulse
  when the food can be eaten again.
- **Hotbar module**: eight slots on a framed plate with knot caps at the bottom centre;
  key index, stack amount, durability bar (blinks red when broken), gold highlight for
  equipped items and for the gamepad selection. Keys and gamepad keep working through
  vanilla, which is veiled, not disabled.
- **Status module**: effect tiles with name and time, top-right below the vanilla
  minimap; the guardian power first, with its cooldown as m:ss and a gold frame when
  ready. Effects added by other mods appear too.
- Regions can hold several vanilla objects (the food strip is many loose pieces).
- New art: slot, active slot, status tile, hotbar plate (SVG sources).
- Config: `[Modules] Food/Hotbar/Status`, `[Food]`, `[Hotbar]`, `[Status]` offsets.


## 0.2.0-preview.1 — F2: first visual module

### Added
- **Vitals module**: health, stamina and eitr as framed vertical bars at the bottom-left
  (concepts 4–6). Numbers inside the bars, a trail showing the damage just taken, an
  ember pulse on the health frame below 25 %, the eitr bar only when the character has
  eitr. Vanilla bars are veiled, not destroyed. Position and size in `[Vitals]` config.
- **Module host**: regions with a single owner (blocked while SeneaL UI is installed),
  guarded build/refresh/teardown, live toggles (`[General] Enabled`, `[Modules] Vitals`),
  scene changes handled centrally, allocation-free refresh path.
- **Vanilla veil**: CanvasGroup veil restored exactly; re-applied every LateUpdate when a
  vanilla animator fights it, logged once.
- **Theme**: Cinzel and Cormorant Garamond (OFL) loaded at runtime with the game font as
  fallback; SVG-sourced sprites (bar frame, bar fill, medallion) from a validated manifest.
- **Diagnostics panel (F8)** in Preview/Debug: modules with state and cost, regions, veils,
  faults, input leases; buttons to inject a fault, retry a module, show vanilla underneath,
  write the report.
- `tools/inspect` (member lister over `ref/`), `tools/art/fonts.py`, `docs/regions.md`.
- 33 more Core tests (bar animation, colours, pulse, sprite manifest).


## 0.1.0-preview.2 — F1 fixes from R-000

### Fixed
- The session header logged the window size before Valheim applied the player's
  resolution (302x193 on a 1920x1080 screen). Screen and GUI scale are now logged as a
  `Display:` line once the game GUI exists, and again whenever they change.
- The watermark no longer triggers TextMeshPro's "LiberationSans SDF Font Asset was not
  found" warning: it is built inactive and enabled after the game font is assigned.
- Reference and contract labels said Valheim l-1.0.15; the copied assemblies were
  already l-1.0.16 (build 25527701). The label came from a stale server log.

## 0.1.0-preview.1 — F1 Foundation

### Added
- Plugin that loads on clients only (stops on dedicated servers) and declares
  `NetworkCompatibility(NotEnforced)`.
- `Foundation`: guard and fault registry, guarded per-class patcher with rollback,
  game-contract resolver, input leases over Jötunn, category logging with rate limit,
  own log file (non-Release), session header, redacted diagnostic report.
- Diagnostics key (F8): writes a report to `BepInEx/GenesisUI/reports/` and copies its path.
- Watermark with the exact build in Debug and Preview.
- Tests: 42 Core unit tests; contract, banned-API, Foundation-isolation and merge checks.
- `tools/`: `fill-ref.sh`, `package.sh`, art rendering; package icon.

## Unreleased

### Docs
- F0: vision, architecture, art direction, patch policy, extension API draft,
  adapters, diagnostics, testing, release, roadmap, decisions D-001–D-017,
  AGENTS.md.
