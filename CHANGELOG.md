# Changelog

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
