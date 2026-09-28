# GenesisUI — Roadmap

Each phase ends with a Preview package and its test scripts passing on Diego's
client. Phases do not overlap: a phase starts when the previous one's exit criteria
are met.

| Phase | Delivers | Exit criteria |
|---|---|---|
| **F0 — Study and docs** ✅ in progress | Source study, these documents, AGENTS.md, repository initialized | Documents approved by Diego |
| **F1 — Foundation** | Solution and build (Debug/Preview/Release), `Foundation/`: guard and fault registry, guarded patcher, contracts + contract-test harness, input leases, log categories, rate limit, report writer; banned-API and Foundation-isolation tests; an empty plugin that loads, logs its session header and shows the watermark | L1–L3 green; R-000 (smoke) passes on Diego's client |
| **F2 — Skeleton** | Plugin, ModuleHost, RegionRegistry, VanillaVeil (spike resolved), Scheduler, diagnostics overlay + inspector + fault injection, theme tokens, runtime fonts, first ornament atlas, **one module: vitals** (vertical health/stamina/eitr bars, concepts 4–6) | R-000, R-001, R-002 and the vitals script pass; enter/leave/re-enter, resolution and GUI scale changes clean |
| **F3 — HUD (read-only)** | Hotbar; status tiles with timers; circular minimap frame with biome, wind, day/time; sprint bar; boss/enemy plate with stars; interaction and hover cards; active foods; notifications; key hints (incl. Jötunn hints) | Every HUD module passes its script; perf budget met with the real modpack |
| **F4 — Extension API v1** | Public API, foreign-element dock (spike resolved), first adapters (Backpacks, Jewelcrafting, StarLevelSystem) | Adapter scripts pass with the modpack's versions; contract tests against their DLLs |
| **F5 — Windows** | Main window shell (top nav, Q/E, footer hints); inventory + equipment + item details over the vanilla grid; texts; skills (vanilla); settings; crafting and build browser | No item lost or duplicated in the inventory/container/backpack script matrix |
| **F6 — Map** | Map frame, pin filters, marker palette, legend, zoom/center, visibility toggle | Map script passes |
| **F7 — Gameplay package (optional)** | Separate package with server authority: extra slots, quick/action slots, crafting from chests… | Own design docs; server validates every action |
| **F8 — Switch-over** | GenesisUI replaces SeneaL UI on GenesisHeim | Release checklist + rollout in [RELEASE.md](RELEASE.md) |

F3 is feature-complete in 0.5.0-preview.1: vitals, food, hotbar, status, minimap,
stamina bar, boss plate, interaction card, notifications and key hints (lifted above
the hotbar), in two selectable art styles (`carved`, `gold`; D-022). It closes with
Diego's final review of R-040 and the modpack performance check; F4 starts after that.
Enemy (non-boss) plates stay vanilla until Diego asks for them.

## Research items (not scheduled)

- 3D character figure in the inventory/character panels (render-texture camera;
  see the ValheimSagas portrait pipeline already studied for the Armaria).
- Container fill level on hover before opening.
- Live HUD preview inside Settings.
- Painterly world-map texture (possible link with GenesisMapPrinter).
