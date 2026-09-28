# GenesisUI — Diagnostics (debug layer and logs)

**Every build that is not `Release` ships the full diagnostics layer.** It is not
optional: tests happen on Diego's client, far from the developer, so the build
itself must explain everything that goes wrong. A preview without diagnostics is not
delivered.

## 1. Channels

| | Debug | Preview | Release |
|---|---|---|---|
| Compile symbol `GENESIS_DIAGNOSTICS` | yes | yes | no |
| Version string | `x.y.z-dev+<sha>` | `x.y.z-preview.N+<sha>` | `x.y.z` |
| On-screen watermark | yes | yes ("GenesisUI PREVIEW x.y.z-preview.N") | no |
| Overlay and inspector | yes | yes (hotkey) | no |
| Fault injection | yes | yes | no |
| Own log file | yes | yes | no |
| BepInEx log level | Debug | Info | Warning |
| Diagnostic report | yes | yes | yes (on request) |

The watermark means every screenshot tells us the exact build.

## 2. Logging

- Prefix and category: `[GenesisUI:<Category>]`. Categories: `Host`,
  `Module:<id>`, `Patch`, `Adapter:<id>`, `Api:<mod>`, `Input`, `Layout`, `Theme`,
  `Perf`, `Report`.
- **Session header** at startup: game version and build, Unity version, BepInEx,
  Jötunn and GenesisUI versions with git sha, channel, OS,
  resolution, GUI scale, language, and the full plugin list with versions.
- **State transitions** of every module and adapter are logged
  (`hud.vitals: Active -> Faulted`).
- **Faults** are logged once, with full stack trace and context: module, region,
  scene, the last 10 events from the event trace. Repeats are counted, not
  re-logged.
- **Rate limit**: the same message at most once per 10 s; the counter appears in the
  overlay and in the next log line.
- Own file (non-Release): `BepInEx/GenesisUI/logs/genesisui-<yyyyMMdd-HHmmss>.log`,
  rotating, 5 files × 2 MB. BepInEx `LogOutput.log` still receives Info and above.

## 3. The overlay (the "debug bed")

Opened with a configurable hotkey (default `F8`). Panels:

| Panel | Shows |
|---|---|
| Modules | id, state, owned regions, reason for `Blocked`/`Unsupported`/`Faulted`, fault count |
| Regions | every vanilla region, its owner (GenesisUI module, another mod, or vanilla), veil state |
| Patches | each patch class: applied / skipped (missing target) / failed |
| Adapters & API | detected mods, version, adapter state, extensions registered and their fault counts |
| Perf | ms/frame per module (avg, p95, max), allocations per frame, Scheduler budget use |
| Input | open input leases and owners, key conflicts |
| Events | last 50 events of the event trace |

Tools:

- **Inspector**: hold `Ctrl+Alt` and hover any GenesisUI element to see its path,
  its module and its view model.
- **Veil toggle**: show vanilla under GenesisUI to compare.
- **Fault injection**: throw inside a chosen module or adapter, or pretend a
  contract is missing. This is how the test scripts prove that a failure returns
  the region to vanilla.
- **Reload** theme and layout files from disk.
- **Copy report** (below).

## 4. Diagnostic report

A plain-text file written to `BepInEx/GenesisUI/reports/report-<timestamp>.txt`
and its path copied to the clipboard. It contains the session header, all overlay
panels as text, the last 200 log lines of GenesisUI, and the
config values of GenesisUI.

Redacted by default: player and character names, platform IDs, server address and
world name. A config switch disables redaction for private debugging.

## 5. Rules for developers

- New module → its states, faults and perf must appear in the overlay without extra
  work (the host does it). Adding a module-specific overlay line is welcome.
- New adapter → its contract resolution result must be visible in "Adapters & API".
- Anything that can fail silently must log at least once.
- Diagnostics code is compiled out of Release with `#if GENESIS_DIAGNOSTICS`,
  except the report and error logging.
