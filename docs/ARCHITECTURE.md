# GenesisUI — Architecture

Status: HUD and in-game windows shipped; the main menu and game settings remain planned.
`1.1.2-preview.2` follows the partial vanilla R-065 feedback with visual/lore fixes (D-041),
tracked in [STABILITY-FIXES](STABILITY-FIXES.md); client R-066 is pending.
[CAPABILITIES](CAPABILITIES.md) separates implemented and planned integration surfaces.

Visual timing: Plugin reads source state after native LateUpdates (execution order 30000),
preview stages render after those module changes (31000), shared CanvasGroup pins enforce last
(32000). Pause-menu drawing follows its active root rather than input visibility hysteresis.
TextViewer roots are permanently active: requested animator flags select one reader. Its native
rune/raven graphics are veiled through closing animations and restored on module teardown.
Lore is unframed, with Core's bounded reusable rune-reveal buffer and native dismissal.
Character previews bake the current visible pose into owned static meshes, with no copied bones,
cloth or gameplay components; framing establishes texture aspect first and refits on resize.

## 1. Repository layout

```
GenesisUI/
  AGENTS.md  README.md  CHANGELOG.md  LICENSE
  GenesisUI.sln
  Directory.Build.props          shared: LangVersion, Deterministic, channels (§9)
  src/
    GenesisUI.Core/              netstandard2.0 — NO Unity, Valheim, Harmony, BepInEx
    GenesisUI/                   net48 BepInEx plugin
      Api/                       planned public extension API (F7; not shipped)
      Host/                      plugin entry, ModuleHost, RegionRegistry, VanillaVeil, Scheduler
      Game/                      planned reader/action facade; current modules read live objects directly
      Modules/<ModuleName>/      one folder per module
      Widgets/                   reusable UI components (Panel, Slot, Bar, KeyCap, ...)
      Theme/                     tokens, runtime fonts, sprites from art/sprites.json
      Adapters/<ModName>/        planned exact-version third-party adapters (not shipped)
      Diagnostics/               overlay, inspector, report, fault injection (non-Release)
      Patches/                   Harmony patch classes, one class per target area
      Foundation/                guard, guarded patcher, contracts, input leases, log/report plumbing
                                 (extraction-ready: see §14)
  art/
    src/                         generated SVG shapes (tools/art/shapes.py), bar_fill.svg, logo.svg
    out/                         rendered PNGs + sprites.json (shipped as plugins/art/)
    fonts/                       OFL fonts + their license files
  tools/
    art/                         style.py, shapes.py, patterns.py, render.py, mock.py, fonts.py
    inspect/                     lists members of ref/*.dll (before declaring a contract)
    fill-ref.sh, package.sh      references from the server; Preview/Release packages
  tests/
    GenesisUI.Core.Tests/        L1 — pure logic
    GenesisUI.Contract.Tests/    L2 — game and adapter contracts, banned-API scan
  docs/                          this folder
```

`GenesisUI.Core` uses a build guard (the pattern from GenesisPlayerBots D-001): the build fails if
it references Unity, Valheim, Harmony or BepInEx, or the plugin project.

## 2. Layers and the direction of arrows

```
   vanilla game ──read──▶ Game/Readers ──snapshots──▶ Core view models ──▶ Widgets/Views
        ▲                                                                     │
        └──────────── Game/Actions (vanilla entry points only) ◀── user input ┘
```

- **Readers** sample vanilla objects and produce immutable snapshots (Core types).
  They never write game state.
- **Core** turns snapshots into view models: formatting, diffing, layout solving,
  settings validation. It is where almost all testable logic lives.
- **Views:** the target separation is pure view models. Current modules/views also read live game
  objects directly, under declared contracts and owner guards; a complete reader facade is deferred.
- **Actions** are the only path from a click back to the game, and they call the
  same vanilla methods the vanilla UI calls (e.g. `InventoryGui`, `Humanoid.EquipItem`,
  `Player.ConsumeItem`). An action never claims ZDO ownership, never removes items
  directly, never registers or invokes an RPC. Enforced by the banned-API scan
  ([TESTING.md](TESTING.md#l2--contract-and-banned-api-tests)).

## 3. Modules

A module owns one or more **regions** of the vanilla UI and draws its replacement.

```csharp
internal interface IUiModule                 // src/GenesisUI/Host/IUiModule.cs
{
    string Id { get; }                       // "hud.vitals"
    string NameToken { get; }                // "$genesisui_module_vitals"
    IReadOnlyList<string> Regions { get; }   // ["hud.health", "hud.stamina", "hud.eitr"]
    float RefreshRate { get; }               // Hz; the scheduler never calls faster
    void Build(ModuleContext ctx);           // create own objects under ctx.Root
    void Refresh(float deltaSeconds);        // scheduled tick
    void Teardown();                         // destroy own objects
}
```

Game members a module touches are declared with `[GameContract]` on the class instead
of a `Requires` list. Suspend/Resume are not needed yet: the module root lives under the
vanilla HUD root, which vanilla already moves off-screen when the HUD hides (see
[regions.md](regions.md)).

An additive module such as `hud.sprint` has an empty region list: it does not veil
vanilla or claim an existing region. It still gets contract checks, guarding,
diagnostics and live enable/disable from the host.

**ModuleHost guarantees**, for every module:

1. The `[GameContract]`s are checked before `Build`. A missing member means
   state `Unsupported` with the exact member named; vanilla keeps the region.
   Base/nested/helper contracts are included with field/parameter/static/return shapes.
   Helper links exclude other module lifecycles; those use explicit host prerequisites so a
   broken optional module cannot disable unrelated regions through a broad dependency graph.
2. Every call is wrapped by the Foundation guard. An exception moves the module
   to `Faulted`: `Teardown` runs, the veil is lifted from its regions (vanilla is
   back), input leases are released, the fault is logged once with full context.
   Recovery is queued until all fault subscribers finish. D-038/D-040 allow bounded automatic
   rebuilding; repeated failures stay off. A partial Build receives the same cleanup as teardown.
3. A module's refresh never receives more than 0.25 s of elapsed time (a fresh build or a
   long hitch would otherwise feed animation clocks nonsense: R-040 striped bars).
   The host's HUD root stays below the large map when both hang under the vanilla HUD root.
4. Scene changes, logout, resolution and GUI-scale changes are handled by the host
   (rebuild on `GUIManager.OnCustomGUIAvailable`), never by modules on their own.
5. Toggling a module in settings takes effect live.

States (`Host/IUiModule.cs`): `Disabled` (off in config), `Waiting` (no HUD yet: main
menu, loading), `Unsupported` (a contract is missing), `Blocked` (region owned by someone
else), `Building`, `Recovering`, `Active`, `Faulted` (restart limit reached; diagnostics can retry).

### Region registry

A region is a named piece of vanilla UI (`hud.hotbar`, `hud.minimap`,
`hud.statusEffects`, `hud.hoverText`, `hud.enemy`, `hud.keyHints`, `hud.messages`,
`window.inventory`, `window.crafting`, `window.map`, ...). **One owner per region.**
If a known mod already replaces a region (SeneaL UI while it is still installed, or
any mod listed in the region's conflict table), the module that wants it becomes
`Blocked` and says why. This is how GenesisUI and SeneaL UI can coexist during the
migration: whoever does not own a region leaves it alone.

### VanillaVeil — hide, never destroy (resolved in F2–F3)

Vanilla objects are hidden, not destroyed or deactivated: other mods find vanilla
objects by path and attach children to them. The veil records the prior state and
restores it exactly on teardown.

Mechanism: a `CanvasGroup` (alpha 0, no raycasts, not interactable) on each vanilla object
of a region, our own when the object has none. `Enforce` re-applies alpha 0 every
LateUpdate and logs once if vanilla fought it; dead targets are pruned. Per region, which
objects are veiled and what vanilla does to them is in `docs/regions.md`. When our view
must live inside a veiled vanilla object (creature plates), it carries its own CanvasGroup
with `ignoreParentGroups`.

### VanillaNudge — move, never re-parent

When a vanilla element is not replaced but collides with our layout (the key hints under
the hotbar plate), `Foundation/VanillaNudge` offsets its `anchoredPosition`, records the
original, re-applies the offset if vanilla resets it, and restores it exactly when the
owner is torn down or faults — the same contract as the veil.

### Dynamic regions

Some regions are created by vanilla on demand (`hud.boss`, `hud.enemy`: one clone per boss
or creature). The owning module veils new clones as they appear (clones are matched by the
exact name vanilla gives them, template name + "(Clone)"); an empty dynamic region is normal
and not reported as a problem.

### Mirroring vanilla

Where vanilla already decides what to show, a module mirrors it instead of re-implementing
the logic: minimap pins and material (D-019, D-021), hover text, message queue timing,
creature plates (placement, visibility, marks). This keeps other mods' content visible
and never reveals more than vanilla would.

### Foreign-element dock

**Design only (F7), not implemented.** Foreign children under hidden vanilla roots may still
need explicit adapters. Generic detection/docking needs a capability and lifetime prototype;
the current host does not re-host foreign objects. Known replacement owners block the conflicting
GenesisUI regions instead. Item details retain the full vanilla tooltip text as the baseline fallback.

## 4. Scheduling and data flow

- Modules use `ModuleHost.Tick`; visual effects and preview stages have separately guarded Unity
  callbacks. `ModuleHost.Tick` (from the plugin's `LateUpdate`, so after vanilla
  opened or closed its windows in its own `Update`: nothing vanilla is drawn for a frame before
  GenesisUI hides it, R-059) refreshes each
  module at the rate it declares: every frame for what follows vanilla positions
  (minimap, creature plates, notice stack), 30 Hz for bars and plates, 10–20 Hz for food,
  status, hotbar and hover. The elapsed time handed to a module is capped at 0.25 s.
  Event-driven refreshes (e.g. `Inventory.m_onChanged`) come with the windows (F4).
- Views update only when their view model changed (Core diffing).
- Per-frame budget is enforced by measurement, not by trust (§8).

## 5. Input

- Keys are registered through Jötunn `InputManager` under `GenesisUI_*` names,
  keyboard and gamepad, backed by config.
- Windows that need the mouse take an **input lease** from Foundation. A lease
  is an `IDisposable` per owner; the host disposes all leases of a module on
  teardown or fault. Rationale: `GUIManager.BlockInput` counts requests but its
  reset zeroes the counter for everyone, so one unbalanced caller releases the
  others' block.
- `BlockOtherInputs` is used only while a GenesisUI window has focus.
- Text fields hold a text-focus lease so typing never triggers hotkeys.
- A key-conflict check lists collisions with vanilla bindings and other mods'
  `ButtonConfig`s in the diagnostics overlay.

## 6. Layout

- Anchor-based. Reference resolution 1920×1080. Final scale = GenesisUI scale ×
  vanilla GUI scale.
- A **layout profile** is a JSON data file in the config folder: per widget,
  anchor, offset, size, visibility. Parsed by Core's `StrictJson` into typed
  classes (no type names, no code; D-018), max 256 KB, schema version checked,
  every number clamped by Core. A broken file falls back to the default layout and
  is reported, never partially applied.
- A drag-to-place edit mode comes after F3.

## 7. Theme

- **Tokens** (colors, font roles, spacing, frame styles) from
  [ART-DIRECTION.md](ART-DIRECTION.md), shipped as the default theme data file.
- **Fonts**: OFL TTFs shipped as files next to the DLL, loaded at runtime with
  `TMP_FontAsset.CreateFontAsset(path, ...)`, with the vanilla TMP font as fallback
  for missing glyphs. If loading fails, the vanilla font is used and the failure is
  reported; the UI never renders without text.
- **Ornaments and frames**: our own SVG sources, rasterized by `tools/art/render.py`
  into PNGs plus `art/out/sprites.json` (size, 9-slice border, optional wrap and
  **content insets** — the rectangle inside a frame where content goes).
- One art direction, gold (D-023). Views ask the theme where a frame's content goes
  (`Content`) instead of hard-coding the frame's measurements.
- **Game sprites** (item icons, some UI sprites) are read at runtime from the game
  (`GUIManager.GetSprite`, `ItemData.GetIcon()`); never redistributed.

## 8. Performance budget

| Metric | Target | Where it is measured |
|---|---|---|
| GenesisUI total, HUD only, steady state | ≤ 0.5 ms/frame average | Diagnostics perf panel |
| Any single module | ≤ 0.15 ms/frame average | Diagnostics perf panel |
| Managed allocations per frame, steady state | 0 bytes goal, flagged above 1 KB | Diagnostics perf panel |
| Opening a window (inventory, crafting) | ≤ 1 frame hitch above 16 ms | Test script |

Numbers are measured on Diego's client with the real modpack; the test script
records them.

## 9. Build channels

| Channel | Configuration | Diagnostics | Who gets it |
|---|---|---|---|
| `Debug` | unoptimized, symbols | everything on | local development |
| `Preview` | optimized, symbols | compiled in (`GENESIS_DIAGNOSTICS`), overlay on hotkey, watermark on screen | Diego's client tests |
| `Release` | optimized | errors, warnings and on-demand report only | Hexium |

Details in [DIAGNOSTICS.md](DIAGNOSTICS.md) and [RELEASE.md](RELEASE.md).

## 10. Settings

- The Settings tab (see concept "Configurações") edits client-local BepInEx config
  entries through Core-validated models; "Apply" commits, "Reset" restores defaults.
- Everything also works through BepInEx ConfigurationManager.
- Nothing is admin-only or synced in the visual scope.

## 11. Localization

- pt-BR and English at launch, through Jötunn `LocalizationManager`
  (`Translations/<Language>/genesisui.json`).
- Player-facing text follows the GenesisMods Norse tone. Code, logs and comments
  are English.

## 12. Security model

A client UI mod can hurt players in four ways; each has a rule:

| Risk | Rule |
|---|---|
| Creating item loss or duplication | UI never mutates game state except through vanilla entry points; banned-API scan in tests |
| Being an attack surface | No RPC, no network input, no code or type names from data files, bounded and validated JSON, file I/O only under `BepInEx/config/GenesisUI/` and `BepInEx/GenesisUI/` with canonicalized paths |
| Leaking player data | Diagnostic reports redact player names, platform IDs and server addresses by default |
| Fighting other mods | No `Harmony.Unpatch` of foreign patches, postfix-first ([PATCH-POLICY.md](PATCH-POLICY.md)), hide-don't-destroy |

Anti-cheat: the server runs AzuAntiCheat with a DLL whitelist and instant ban;
production rollout requires whitelisting ([RELEASE.md](RELEASE.md)).

## 13. Dependencies

| Dependency | Why | Rule |
|---|---|---|
| BepInExPack_Valheim | loader | — |
| Jötunn (latest) | GUI root per scene, input, localization, key hints, ModQuery | compile and run against the same, latest version |

No other runtime dependency without a decision in [DECISIONS.md](DECISIONS.md).

## 14. Foundation — the future GenesisModLIB

Infrastructure that any GenesisMods mod could use lives in `Foundation/` (plugin) and
`GenesisUI.Core/Foundation/` (pure logic), under the `GenesisUI.Foundation` namespace:
the guard and fault registry, the guarded patcher, contract declaration and
resolution, input leases, log categories and rate limiting, the report writer with
redaction, invariant formatting.

Rule: **Foundation never references anything outside Foundation** (no modules, views,
theme or API types). A contract test enforces it by scanning type references. The day
a second GenesisMods mod needs it, `Foundation` moves as-is into GenesisModLIB, a
separate plugin (DECISIONS.md D-002, D-015).
