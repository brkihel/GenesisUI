# GenesisUI — Roadmap

Each phase ends with a Preview package and its test scripts passing on Diego's
client. Phases do not overlap: a phase starts when the previous one's exit criteria
are met.

| Phase | Delivers | Exit criteria |
|---|---|---|
| **F0 — Study and docs** ✅ | Source study, these documents, AGENTS.md, repository initialized | Documents approved by Diego |
| **F1 — Foundation** ✅ | Solution and build (Debug/Preview/Release), `Foundation/`: guard and fault registry, guarded patcher, contracts + contract-test harness, input leases, log categories, rate limit, report writer; banned-API and Foundation-isolation tests; an empty plugin that loads, logs its session header and shows the watermark | L1–L3 green; R-000 (smoke) passes on Diego's client |
| **F2 — Skeleton** ✅ | Plugin, ModuleHost, RegionRegistry, VanillaVeil (spike resolved), Scheduler, diagnostics overlay + inspector + fault injection, theme tokens, runtime fonts, first ornament atlas, **one module: vitals** (vertical health/stamina/eitr bars, concepts 4–6) | R-000, R-001, R-002 and the vitals script pass; enter/leave/re-enter, resolution and GUI scale changes clean |
| **F3 — HUD (read-only)** ✅ | Hotbar; status tiles with timers; circular minimap frame with biome, wind, day/time; sprint bar; boss/enemy plate with stars; interaction and hover cards; active foods; notifications; key hints (incl. Jötunn hints) | Every HUD module passes its script; perf budget met with the real modpack |
| **F4 — Inventory and crafting windows** | Window shell (top tabs, Q/E, footer hints) over vanilla's `InventoryGui`; inventory grid + equipment + item details + weight/armour; containers (chests, carts, ships, tombstones); crafting and every station (workbench, forge, cauldron, stonecutter, artisan table, black forge, galdr table…) with recipe list, requirements have/need and upgrade; skills; texts; trophies; build piece browser (hammer, hoe, cultivator) | No item lost or duplicated in the inventory/container script matrix; every station script passes |
| **F5 — Menus** | Main menu (title, character and world selection, join/host), pause (Esc) menu, vanilla settings window, GenesisUI settings | Menu scripts pass; nothing of the vanilla flow (saves, joining, settings) behaves differently |
| **F6 — Map** | Map frame, pin filters, marker palette, legend, zoom/center, visibility toggle | Map script passes |
| **F7 — Extension API v1 and adapters** | Public API, foreign-element dock (spike resolved), first adapters (Backpacks, Jewelcrafting, StarLevelSystem) | Adapter scripts pass with the modpack's versions; contract tests against their DLLs |
| **F8 — Gameplay package (optional)** | Separate package with server authority. **Inventory part moved into F4, same plugin (D-028)**: admin slot count, quick/utility slots, equipment slots, sort. Left for F8: crafting from chests… | Own design docs; server validates every action |
| **F9 — Switch-over** | GenesisUI replaces SeneaL UI on GenesisHeim | Release checklist + rollout in [RELEASE.md](RELEASE.md) |

F3 was approved by Diego on 2026-09-28 (R-040 on 0.5.0-preview.3; fixes in preview.4). F4.0 (textures from Diego's sheets on the whole HUD, D-027) is in 0.6.0-preview.1, script R-042.
The whole vanilla UI comes before any mod integration (D-024): F4 windows, F5 menus, F6 map,
then the Extension API and adapters in F7. The modpack performance check from the F3 exit
criteria moves to the first F4 test run. The F4 plan is in [F4-PLAN.md](F4-PLAN.md).

Diego advanced F4.2b before completing R-050. Preview.7 combines F4.2b's six vanilla
equipment cells with fixes for the two remaining preview.6 findings: inventory slot draw
order and the horizontal stamina burn. R-051 tests those fixes and item safety on the client;
Diego reports that the equipment flow works, with two window lifecycle defects: hints pile
up on the first opening and vanilla inventory flashes on close. Preview.8 targets those
defects. R-052 is the client gate before F4.2b is marked approved.

R-052 (2026-09-29): Diego found the windows had drifted from ConceptArt (9) — giant, empty
panels — and that he had chosen replacement, not skins. Preview.9 implements D-032: our own
inventory window on the concept's design board, vanilla as the hidden engine, plus quick/action
slot rules and Organizar. R-053 is its client gate. Crafting (F4.3) follows the same approach.

**1.0.0 (2026-09-30):** Diego approved the windows after R-061 (0.8.0-preview.5) and asked for the
1.0 release: F4 complete, F6 (map) delivered with the framed map and custom markers, and from F5 the
Esc menu. The main menu (FejdStartup) and the game's settings screen remain in F5.

## Research items (not scheduled)

- **Authorized stabilization:** `fix/stability-1.1.2` / `1.1.2-preview.3` implements the base
  corrections from all 25 findings; tracker [STABILITY-FIXES](STABILITY-FIXES.md), decision D-040,
  client regression [R-065](testing/scripts/R-065-stability-base.md). Partial vanilla feedback
  led to D-041 character/cape, map, Produce, Esc and lore fixes. R-066 rejected the baked
  character and found native equip-bar leakage; the visual rig and D-042 progress replacement
  await [R-067](testing/scripts/R-067-character-rig-and-equip-progress.md). Core/contracts/data/package
  gates precede delivery. Structured providers and Backpacks/Jewelcrafting/HipLantern adapters
  remain separate integration work; R-064 remains their future combined certification script.

- 3D character and item previews are implemented in the 1.1 development build (D-034/D-039).
  R-062 confirms models appear in 1.1.1-preview.1, with a pink outline. The
  1.1.1-preview.2 edge correction passed shader compilation and the Direct3D11 edge
  check; Diego approved the visual result in R-063, committed as `2a1854a`.
  The [stability and FullPlaythrough review](review/2026-10-02/README.md) is complete:
  25 findings, 71 package/74 DLL inventory and an [implementation sequence](review/2026-10-02/IMPLEMENTATION.md).
  Lifecycle and inventory prerequisites precede dependent compatibility modules; no
  modpack runtime certification or new phase approval follows from the review alone.
- Container fill level on hover before opening.
- Live HUD preview inside Settings.
- Painterly world-map texture (possible link with GenesisMapPrinter).
