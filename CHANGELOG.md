# Changelog

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
