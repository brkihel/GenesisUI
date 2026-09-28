# Changelog

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
