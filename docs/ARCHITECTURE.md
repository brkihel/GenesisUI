# GenesisUI — Architecture

Status: F3 HUD previews. Sections marked **Spike** still need a prototype on a
real client before they are final.

## 1. Repository layout

```
GenesisUI/
  AGENTS.md  README.md  CHANGELOG.md  LICENSE
  GenesisUI.sln
  Directory.Build.props          shared: LangVersion, Deterministic, channels (§9)
  src/
    GenesisUI.Core/              netstandard2.0 — NO Unity, Valheim, Harmony, BepInEx
    GenesisUI/                   net48 BepInEx plugin
      Api/                       public extension API (namespace GenesisUI.Api)
      Host/                      plugin entry, ModuleHost, RegionRegistry, VanillaVeil, Scheduler
      Game/                      readers (game → snapshots) and actions (UI → vanilla entry points)
      Modules/<ModuleName>/      one folder per module
      Widgets/                   reusable UI components (Panel, Slot, Bar, KeyCap, ...)
      Theme/                     token loading, fonts, ornament atlas
      Adapters/<ModName>/        one folder per third-party adapter
      Diagnostics/               overlay, inspector, report, fault injection (non-Release)
      Patches/                   Harmony patch classes, one class per target area
      Foundation/                guard, guarded patcher, contracts, input leases, log/report plumbing
                                 (extraction-ready: see §14)
  art/
    src/                         SVG sources we author (ornaments, frames, logo, nav icons)
    fonts/                       OFL fonts + their license files
  tools/                         atlas builder, packaging helpers
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
- **Views** only render view models. A view never reads the game directly.
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
2. Every call is wrapped by the Foundation guard. An exception moves the module
   to `Faulted`: `Teardown` runs, the veil is lifted from its regions (vanilla is
   back), input leases are released, the fault is logged once with full context.
   No automatic retry in the same session (Debug builds offer a manual retry).
3. A module's refresh never receives more than 0.25 s of elapsed time (a fresh build or a
   long hitch would otherwise feed animation clocks nonsense: R-040 striped bars).
   The host's HUD root stays below the large map when both hang under the vanilla HUD root.
4. Scene changes, logout, resolution and GUI-scale changes are handled by the host
   (rebuild on `GUIManager.OnCustomGUIAvailable`), never by modules on their own.
5. Toggling a module in settings takes effect live.

States: `Disabled` → `Unsupported` | `Blocked` (region owned by someone else) →
`Active` ⇄ `Suspended` → `Faulted`.

### Region registry

A region is a named piece of vanilla UI (`hud.hotbar`, `hud.minimap`,
`hud.statusEffects`, `hud.hoverText`, `hud.enemy`, `hud.keyHints`, `hud.messages`,
`window.inventory`, `window.crafting`, `window.map`, ...). **One owner per region.**
If a known mod already replaces a region (SeneaL UI while it is still installed, or
any mod listed in the region's conflict table), the module that wants it becomes
`Blocked` and says why. This is how GenesisUI and SeneaL UI can coexist during the
migration: whoever does not own a region leaves it alone.

### VanillaVeil — hide, never destroy (**Spike**)

Vanilla objects are hidden, not destroyed or deactivated: other mods find vanilla
objects by path and attach children to them. The veil records the prior state and
restores it exactly on teardown.

Preferred mechanism: a `CanvasGroup` (alpha 0, no raycasts, not interactable) on
the region root. Unity allows one `CanvasGroup` per GameObject, and some vanilla
roots are faded by vanilla code, so the F2 spike must list, per region, which
object is veiled and how. The result goes into `docs/regions.md`.

### VanillaNudge — move, never re-parent

When a vanilla element is not replaced but collides with our layout (the key hints under
the hotbar plate), `Foundation/VanillaNudge` offsets its `anchoredPosition`, records the
original, re-applies the offset if vanilla resets it, and restores it exactly when the
owner is torn down or faults — the same contract as the veil.

### Dynamic regions

Some regions are created by vanilla on demand (`hud.boss`: one clone per boss). The
registry resolves them anew and the host veils new objects as they appear; an empty
dynamic region is normal and not reported as a problem.

### Foreign-element dock

Children that other mods add under veiled vanilla roots would disappear with the
veil. The host watches veiled roots for GameObjects whose components come from a
non-vanilla assembly, and **re-hosts a visible proxy of their position** in a dock
the player can place. This is the generic compatibility path for mods that draw
into the vanilla HUD without knowing GenesisUI. (**Spike** in F7: proxy by
reparenting vs. by leaving it in place and punching a hole in the veil.)

## 4. Scheduling and data flow

- No `Update()` per view. A central **Scheduler** ticks modules at the rate each
  one declares (vitals 20 Hz, compass 30 Hz, status effects 4 Hz, ...), and runs
  event-driven refreshes when vanilla offers a signal (e.g. `Inventory.m_onChanged`).
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
