# Stability and FullPlaythrough readiness review

Date: 2026-10-02. Reviewed runtime baseline: **`2a1854a`**, GenesisUI **1.1.1-preview.2**.
This is an engineering review and implementation plan. It does not certify the modpack.

## Conclusion

Keep the approved preview as the visual baseline. Before implementing foreign equipment,
containers and resource modules, repair the lifecycle and inventory safety boundaries.
The current automated suite passes, but it does not exercise the failure paths that matter
most when another mod changes UI objects, equipment or inventory behavior.

The first implementation batch should address **S-01 through S-06** below. In particular:

- Recovery can rebuild a module before later fault subscribers release that owner's new resources.
- A partially failed Build does not receive the same cleanup as a successfully built module.
- Special-slot relocation can run without verifying that the inventory-height patch succeeded.
- Dedicated-server startup exits before the advertised synchronized inventory settings initialize.
- Windows lack the foreign-region arbitration that protects the HUD.
- Character equipment created after initial preview sanitization is not sanitized again.

No item loss, network mutation by a preview, or FullPlaythrough conflict was reproduced in a
running game during this review. These are code-proven mechanisms or conditional risks,
identified explicitly in each finding. No runtime corrections are included in this review commit.

## Documents and evidence

- [Findings, severity and acceptance criteria](#findings)
- [Coverage by subsystem](#coverage-by-subsystem)
- [FullPlaythrough inventory and integration study](MODPACK.md)
- [Implementation sequence and release gates](IMPLEMENTATION.md)
- [Future client verification script, in pt-BR](../../testing/scripts/R-064-stability-modpack.md)
- [Source inventory](source-inventory.csv), [package manifests](modpack-packages.csv),
  [assembly fingerprints](modpack-assemblies.csv), [plugin identities](modpack-plugin-identities.csv),
  [last observed loaded plugins](modpack-last-loaded.csv)
- [Pure reproductions](pure-reproductions.json), [installed-client contract check](installed-client-contracts.json)
- [Reproduction tool and commands](../../../tools/review/README.md)

The CSVs record metadata facts, not foreign implementation, assets or DLLs. Raw game
decompilation, raw logs, third-party API signature dumps and binaries stay in ignored scratch
storage. They must not be committed or shipped.

## Scope and method

The source inventory covers **171 tracked code/build/tool/test/shader files, 25,911 lines** at
the baseline. It includes Core, Foundation, host, every module, widgets, gameplay, patches,
tests, art tools, packaging and shaders. All were inventoried and structurally screened;
critical lifecycle, input, persistence, rendering and interop paths received direct source
inspection. Pattern searches and compiled IL inspection supplemented that reading.
This is not a claim that every line was formally verified or manually read in sequence.

Required project documents were consulted: vision, decisions, architecture, regions, patch
policy, changelog, roadmap, latest results, diagnostics, testing, release, adapters and extension
API. The referenced GenesisMods house-standard file was not found under the available `C:/dev`
workspace; compliance with its additional rules was not independently checked.

Foreign assemblies were examined with `MetadataLoadContext`: metadata only, no plugin code
execution. No closed-mod source was decompiled or reused. Five Valheim types were decompiled
serially, one type at a time with a heap cap: `Inventory`, `Player`, `Humanoid`, `VisEquipment`
and `InventoryGrid`. They supplied behavioral evidence, never implementation for copying.

The supplied profile is a **client snapshot**, not an authoritative inventory of server-side
plugins or their configuration. Its last available `LogOutput.log` was written at
2026-10-02 11:20:07 local time. It contains no GenesisUI load entry. Package presence, plugin
metadata and an old load entry each answer a different question; none proves current coexistence.

## Verified baseline

| Check | Evidence / result | Limit |
|---|---|---|
| Debug build and tests | 145 Core + 8 contract tests pass; 0 skips | Through `dotnet test GenesisUI.sln -c Debug` |
| Preview build and tests | 145 Core + 8 contract tests pass; 0 skips | Through `dotnet test GenesisUI.sln -c Preview` |
| Release build and tests | 145 Core + 8 contract tests pass; 0 skips | Through `dotnet test GenesisUI.sln -c Release` |
| Forbidden API scan | Pass in all three channels | Only the patterns currently listed in `BannedApiTests` |
| Core merge / Foundation isolation | Existing contract tests pass | Does not cover all lifecycle effects |
| Declared game contracts | 562 declarations resolve against `ref/` | Existence / declared overload checks, not behavioral compatibility |
| Installed Steam client contracts | Independent metadata check: 562 checked, 0 missing | No execution; different binary from `ref/` |
| Recovery subscription order | Pure reproduction: new veil and lease both removed | Actual Core event dispatch; modeled host/Unity effects |
| Duplicate layout IDs | Pure reproduction: accepted despite duplicate ID | Production caller currently supplies unique list indices |
| JSON number grammar | `01`, `1.`, `-.1` accepted | These are invalid JSON numbers |
| Foreign metadata | 74 DLLs inspected, no metadata-error records | Not adapter contract tests or runtime verification |
| Preview shader | Earlier GPU check: 256 solid, 512 edge, 256 clear pixels; D3D11 | Synthetic keyed-composition check, not arbitrary foreign materials |
| Approved preview | Diego explicitly approved 1.1.1-preview.2 visually | R-063 records no new F8 report or complete script results |

Test output and TRX files are in ignored `dist/review-20261002/`. The shader/GPU result and
preview approval are recorded in [R-063](../../testing/results/R-063-1.1.1-preview.2.md).
No Valheim session, second client, dedicated server or production service was started.

### Reference provenance

`ref/SOURCE.txt`: Valheim **l-1.0.16**, build **25527701**, Jötunn **2.30.2**, copied
2026-09-28T17:03:46Z. Hashes checked during this review:

| Artifact | SHA-256 |
|---|---|
| `ref/assembly_valheim.dll` | `50035055F9B158A025CACD25E038B603943F7C2A465DA3021707B5F1E44E39FD` |
| Installed Steam `assembly_valheim.dll` | `96CFC004F7F4A6F30D070BEF39EAFD79C466A137121C4665A2F19FB9C15C6127` |
| `ref/Jotunn.dll` | `9509221046842A70F94B98BAB4CB41005DEE026823A7A79345829ED46A30F796` |
| Post-commit Preview DLL | `B99F2205B7D2A6820BD8B9F7D3C2F2C5933312E2A1D4E323D4AA400D124D35BB` |

The installed game DLL differs in hash and size. Both expose the declared members, but the
direct game-logic findings below refer to **`ref/`**, not the installed client's implementation.
Refresh/reconcile the authoritative references before adapter certification and repeat the
relevant behavioral comparisons when those references change.

The approved ZIP was built before the approval commit under Diego's explicit exception;
its watermark names `003ad78`, not `2a1854a`. Its SHA-256 is
`601D29E77F483037CECF0B4D2943F6FB5603BEF6A15E4B5539505084A676E2D6`.
The next test build must use its own committed SHA and a new preview number.

## Findings

**P1**: resolve before modules that depend on the affected boundary. **P2**: important
correctness/hardening work, scheduled alongside the corresponding integration. **P3**:
maintenance or narrower visual issue. No P0 is assigned: this review did not observe a
current catastrophic failure in Diego's client.

Evidence labels distinguish **reproduced**, **source-proven**, **conditional risk**, and
**design gap**. A proposed change is not yet an approved architecture decision.

### S-01 — P1: recovery and owner cleanup run in the wrong order

**Source-proven + pure reproduction.** [ModuleHost.Init/Recover](../../../src/GenesisUI/Host/ModuleHost.cs#L44)
subscribes to `FaultRegistry.Tripped` and synchronously tears down, resets and rebuilds the
same owner. [VanillaVeil](../../../src/GenesisUI/Foundation/VanillaVeil.cs#L39),
[VanillaNudge](../../../src/GenesisUI/Foundation/VanillaNudge.cs#L26) and
[InputLeases](../../../src/GenesisUI/Foundation/InputLeases.cs#L19) subscribe during their
static initialization. When they initialize after the host subscription, their handlers run
after the rebuild and release resources with that same owner name. Plugin startup initializes
the host before GUI builds. The pure reproduction uses the real Core `FaultRegistry` and the
same subscriber order: `NewVeilSurvives=false`, `NewLeaseSurvives=false`.

There is also no per-subscriber exception isolation in `Tripped?.Invoke`: a throwing handler
can prevent later cleanup and the log after `Report`; `Guard.Fault` swallows the resulting
exception. Automatic recovery must not make cleanup contingent on subscriber order.

**Remedy:** one lifecycle coordinator; finish teardown and owner-wide cleanup before rebuilding.
Queue recovery outside fault-event dispatch and use owner generations for resources. Preserve
the bounded automatic recovery behavior Diego requested, including the fault popup.
**Acceptance:** injected Refresh failures leave exactly one new root, the expected veils/leases,
and no stale resources. A throwing cleanup participant cannot suppress the rest or the error log.

### S-02 — P1: partial Build has no complete rollback

**Source-proven.** [ModuleHost.Build](../../../src/GenesisUI/Host/ModuleHost.cs#L207) handles
`Guard.Run(Build)==false` by destroying the host root and releasing region claims. It does not
call module Teardown. The recovery subscriber only handles entries already Active, so it does
not fill this gap. A Build may already have registered events, set static flags, or created a
window on another canvas. `InventoryModule.Build` subscribes to settings and assigns `_active`;
`WindowModuleBase.Build` records a handled tab before later window construction.

Post-Build region resolution/veiling is outside the Build guard. The host also does not retain
and destroy its per-module root on ordinary teardown; modules generally destroy their own
children, leaving empty host roots after repeated toggles/recoveries.

**Remedy:** track a Building state and owned root, register rollback actions as resources are
acquired, and run unconditional best-effort cleanup for every failed stage. Teardown must tolerate
partial initialization. Guard region resolution and veil creation as part of the same operation.
**Acceptance:** faults injected before/after each acquisition restore all flags, events, roots,
claims, skins and input leases; repeated toggles and scene transitions return to baseline counts.

### S-03 — P1: inventory mutation depends on an unchecked patch cohort

**Conditional risk; destructive vanilla behavior verified in `ref/`.**
[Plugin.Awake](../../../src/GenesisUI/Plugin.cs#L167) independently applies size, placement and
equipment patch classes. Their results do not gate `InventoryModule.Build/Apply`.
[Apply](../../../src/GenesisUI/Gameplay/InventoryModule.cs#L95) changes item `m_gridPos`, assigns
Current, then calls `Player.SetInventorySize(layout.Rows)`. It relies on the size prefix to
substitute `TotalRows`.

In inspected vanilla, `Player.SetInventorySize` clamps to 0..9, changes height and calls
`Humanoid.DropInvalidItems`. That method drops stacks outside the resulting width/height through
`DropItem`. If the size patch is unavailable, rolls back or is superseded while relocation remains
enabled, special-row positions exceed the ordinary height. No such item drop was observed in-game.
The current Core cap of nine total rows is correct and should remain.

**Remedy:** treat all layout prerequisites as one capability. Refuse relocation unless contracts
and required patch results are ready. Validate actual resulting height before publishing positions;
plan a failure-safe transition with snapshot, identity/count invariants and explicit rollback.
Do not rely on a later UI fault to make a gameplay transition safe. Define interactions with
other inventory-height patches; keep arbitrary foreign inventories untouched.
**Acceptance:** missing patch, conflicting height, resize exception, full ordinary inventory,
configuration shrink and repeated load never produce out-of-bounds items, changed identity/count,
silent relocation failure or dropped stacks. These tests precede Backpacks and death integrations.

### S-04 — P1: synchronized inventory settings cannot start on a dedicated server

**Source-proven initialization gap.** [Plugin.Awake](../../../src/GenesisUI/Plugin.cs#L56)
returns for `GUIManager.IsHeadless()` before
[InventorySettings.Bind](../../../src/GenesisUI/Gameplay/InventorySettings.cs#L27) constructs
`ConfigSync` and adds locking/synced entries. The settings descriptions promise admin-controlled
server values. Running this startup path on a dedicated server cannot initialize that service.
No dedicated-server test was run.

**Remedy:** reconcile D-001's visual client scope with D-031's configuration authority. If server
sync remains the design, isolate minimal config initialization before the UI-only exit; keep
client rendering and gameplay patches separate. Document installation and version policy.
**Acceptance:** a permitted non-production headless harness/test proves config binding without
GUI initialization; a one-person client test observes locked server values and safe reconnect
transitions. Server deployment requires a separate authorized task.

### S-05 — P1: foreign ownership protection does not cover windows

**Design gap; conflict not yet run in-game.**
[RegionRegistry.ForeignOwners](../../../src/GenesisUI/Host/RegionRegistry.cs#L70) recognizes
SeneaL's HUD ownership. [WindowModuleBase](../../../src/GenesisUI/Modules/Windows/WindowModuleBase.cs#L35)
and Inventory/Crafting windows declare empty Regions and hide/restyle through local skins.
Therefore the host's foreign-region checks cannot arbitrate those windows. The FullPlaythrough
snapshot loads SeneaL UI 1.1.7 and a SeneaL-specific Backpacks bridge.

**Remedy:** explicit inventory, crafting, menu, build, store and map region/capability ownership,
with shared shell dependencies and actionable diagnostic reasons. Establish a staged switch-over
profile after equivalent backpack behavior exists. Do not delete foreign UI objects or silently
disable another mod. The documented foreign-child dock is an F7 design, not an existing guarantee.
**Acceptance:** intentional overlap is Blocked or explicitly supported, not two competing views;
turning a module off leaves the foreign region functional. Unknown foreign children remain reachable.

### S-06 — P1: preview sanitization does not cover later equipment attachments

**Conditional risk, backed by game logic.**
[PreviewStage.Strip](../../../src/GenesisUI/Widgets/PreviewStage.cs#L449) sanitizes the initial
inactive clone, allows `VisEquipment`/`ZNetView`, and tries four removal passes without a final
assertion. [CharacterPreview.Dress](../../../src/GenesisUI/Widgets/ModelPreviews.cs#L230) later
copies equipment fields. It calls only Restage when the descendant count changes.

Inspected `VisEquipment.AttachItem/AttachArmor` instantiate equipment objects, activate them and
enable equipped effects. Restage changes layers, lights and shadow flags; it does not strip
scripts/physics/audio. Foreign hooks or components on later attachments can therefore escape
the initial whitelist. An equal-descendant-count replacement can also escape restaging. This does
not establish that any supplied mod currently performs a network action in a preview.

The build sets global `ZNetView.m_forceDisableInit=true` and finally assigns false instead of
restoring its previous value. Nested/preexisting use of that flag would be overwritten.

**Remedy:** define a visual-copy boundary for dynamically attached gear, inspect identities rather
than only counts, reject unsanitized components and restore global flags exactly. Avoid awakening
foreign behavior before sanitization. If that cannot be guaranteed, use a guarded visual provider
or fall back to 2D for that model. Do not add a global game patch merely to mask the risk.
**Acceptance:** modded gear changes of equal/different counts, component-removal refusal and
activation exceptions leave no live gameplay, network, collider or sound behavior in the stage;
the original global flag is preserved on success and failure.

### S-07 — P2: patches and callbacks are not uniformly guarded

**Source-proven guard gaps.**
[InventoryLayoutPatches](../../../src/GenesisUI/Patches/InventoryLayoutPatches.cs),
[EquipmentPatches](../../../src/GenesisUI/Patches/EquipmentPatches.cs) and
[ShortcutInputPatch](../../../src/GenesisUI/Patches/ShortcutInputPatch.cs) have paths that read
game/foreign-modified objects outside an owner-tripping boundary. Some relocation paths use
`Guard.Try`, which logs but does not disable repeated failing behavior. The host only protects its
Build/Refresh/Teardown calls. Input and independent MonoBehaviour callbacks need their own coverage.

**Remedy:** enumerate every patch and UI callback, define a safe return/result on failure and
associate it with the owning capability. A failure during an inventory transaction must use its
transaction cleanup, not merely swallow an exception. Respect D-030's declared skipping prefix.
**Acceptance:** injected callback/patch faults neither escape into vanilla nor keep firing each
frame; other mods' patches still execute and no foreign patch is removed.

### S-08 — P2: item details bypass vanilla tooltip extensions

**Source-proven integration gap.**
[Inventory details Show](../../../src/GenesisUI/Modules/Windows/InventoryWindowModule.cs#L1164)
uses shared description and a fixed eight-row ItemStats list. In inspected vanilla,
`InventoryGrid.CreateItemTooltip` uses `ItemData.GetTooltip()`. Mods extending that output or
adding tooltip children are not automatically represented by our structured detail panel.

**Remedy:** keep a vanilla-tooltip fallback and add read-only, guarded providers for socket
badges, jewelry effects, custom stats and container descriptions. Render unknown information in
a scrollable bounded section. Use the owning mod's UI entry point for actions.
**Acceptance:** item comparison against the unmodified profile preserves relevant tooltip
information, socket state and mod interaction access, including unknown/unsupported providers.

### S-09 — P2: same-object changes leave item details stale

**Source-proven invalidation gap.**
[UpdateDetails](../../../src/GenesisUI/Modules/Windows/InventoryWindowModule.cs#L635) returns
when the item equals `_shownItem`. Durability, quality, stack and custom data can change while
identity stays the same. A rebuilt item stage is also not redisplayed through this path until
selection changes. CharacterPreview has a generation check; the detail caller must allow the
item equivalent to run. Crafting's identity-based detail caches need the same audit.

**Remedy:** bounded presentation snapshots/change versions including provider data and stage
generation; refresh content independently from rebuilding the model.
**Acceptance:** repair, upgrade, socket mutation and stage recreation update a selected item
without moving the pointer; unchanged state does not trigger rebuilding every frame.

### S-10 — P2: hotkey suppression and activation use different eligibility rules

**Source-proven differences from inspected `Player.TakeInput`.**
[QuickSlots.TakesInput](../../../src/GenesisUI/Modules/Slots/QuickSlotsModule.cs#L116) omits
TextViewer, free-fly camera, barber UI and build-search focus. It can use items in contexts where
vanilla rejects player input. [SlotHotkeys.Suppresses](../../../src/GenesisUI/Gameplay/SlotHotkeys.cs#L57)
checks Active/held shortcuts, not whether the slot exists or the input context permits use. A
shortcut for a disabled slot can still consume the matching vanilla action. The suppression
cache also does not track control rebinding while the same keys remain held.

**Remedy:** one eligibility snapshot for shortcut activation and suppression, including enabled
slot counts and focus/modal state; invalidate on config/control-map changes. Preserve the intended
held-combination behavior where releasing Alt first must not accidentally perform a vanilla action.
**Acceptance:** chat/search/console/barber/free-fly, zero slots, rebinding and modifier-release
orders never consume/use the wrong action. Empty enabled-slot semantics must be explicitly decided.

### S-11 — P2: delegating grid callbacks does not preserve every input hook

**Source comparison, not an observed modpack failure.**
[InventoryWindowModule.OnDown/OnDrop](../../../src/GenesisUI/Modules/Windows/InventoryWindowModule.cs#L420)
invoke `m_onSelected`, `m_onRightClick` and `m_onReleased`. This keeps downstream InventoryGui
handlers. It does not execute every InventoryGrid pointer method, pressed-item state, touch
long-press path or modifier translation. Inspected InventoryGrid also updates gamepad selection
and uses Joy modifiers; our selected-cell drawing does not mirror that full path.

**Remedy:** document the exact preserved entry points, bridge missing input semantics where
required, and use dedicated adapter actions for mods hooking earlier pointer handlers. Retain
the D-032 design board; no new item-transfer implementation.
**Acceptance:** mouse drag/split/move/use, gamepad focus/scroll/selection and foreign pointer hooks
behave correctly. Touch support must be explicitly scoped before claiming full input parity.

### S-12 — P2: contracts prove declared members exist, not complete usage coverage

**Source-proven test/runtime limitation.**
[ContractResolver.Missing](../../../src/GenesisUI/Foundation/ContractResolver.cs#L20) uses
non-inherited attributes. Base-window declarations are not automatically checked for subclasses.
Helper classes with declarations are not necessarily validated before their caller uses them.
For example, CharacterPreview's reflected variant/quality field list exceeds its own declarations.
The L2 test checks declarations, not that every used member was declared. Return/field types,
staticness and unqualified overloads can also drift without failing an existence-only contract.

The supplementary IL scan reports **132 caller/method/target candidates** absent from the global
declaration-name set. They are **not 132 confirmed violations**: property getter aliases and
inherited declaring types create false positives; string-based reflection creates false negatives.

**Remedy:** owner-scoped dependency contracts including bases/helpers, correct accessor and
declaring-type normalization, signature checks for reflected bindings and explicit reflection
inventories. Adapter contracts must bind to plugin GUID + actual assembly identity, not a type
name alone: AzuAutoStore also contains a `Backpacks.API` type in this snapshot.
**Acceptance:** removing/changing a used helper member stops only its dependent capability before
Build; a changed foreign API reports Unsupported without breaking unrelated UI.

### S-13 — P2: preview callbacks and fallback state are incompletely isolated

**Source-proven guard/fallback gap.**
[PreviewStage.LateUpdate](../../../src/GenesisUI/Widgets/PreviewStage.cs#L186) calls Healthy,
EnsureTexture and InspectNow outside the camera Render try/catch and outside a module owner guard.
A repeated exception there does not use the host recovery policy. Missing Keyed shader permits
a raw-alpha path whose visibility failed in the earlier reports; creating a model can still return
success and hide the item icon. That success does not establish a visible render.

**Remedy:** a bounded stage error policy with explicit Ready/Unsupported/Faulted states, owned
by the module. Use a real 2D fallback when composition support is unavailable or rendering fails.
Avoid GPU readback as an every-frame visibility test.
**Acceptance:** bundle/shader absent, device texture loss, scene recreation and render exceptions
retain a usable icon/details panel, log once with context and recover within a bounded budget.

### S-14 — P2: restoration has conflicting owners and incomplete snapshots

**Source-proven restoration weaknesses; overlapping-owner effect is conditional.**
[VanillaSkin.Hidden](../../../src/GenesisUI/Foundation/VanillaSkin.cs#L162) installs one HiddenPin
per object without shared-owner accounting. Different skins can snapshot each other's hidden
values; restoring one can remove the only enforcement pin while the other still relies on it.
[VanillaVeil.Enforce](../../../src/GenesisUI/Foundation/VanillaVeil.cs#L148) skips when alpha is
already zero, even if interactable/raycast flags were changed afterward.

`WindowShellModule.KeyHintsAlpha`/teardown and `InventoryWindowModule.HideVanillaDrag` restore
selected values to 1 instead of a complete original state. Added drag groups/flags need ownership
and restoration too. These paths weaken the project's exact-restoration promise.

**Remedy:** shared ownership/reference accounting for common hidden panels, exact property
snapshots, and enforcement of all protected values. Avoid module-to-module temporal assumptions.
**Acceptance:** toggle/fault modules in either order while another owner modifies alpha/raycast
values; original or surviving-owner values are restored and no invisible panel captures the pointer.

### S-15 — P2: shutdown does not unwind the plugin lifecycle

**Source-proven cleanup gap; long-session impact unmeasured.**
[Plugin.OnDestroy](../../../src/GenesisUI/Plugin.cs#L348) shuts down logging only. GUI/config
subscriptions, static module entries, leases, module roots and shared ThemeRuntime/UiSound native
resources have no coordinated shutdown there. Several widgets correctly destroy their own
materials and RenderTextures, but that does not dispose shared fonts/textures/clips or clear
global references. This is especially relevant to reload and repeated failed initialization.

**Remedy:** an idempotent host/plugin Shutdown with explicit event unsubscription, module cleanup,
shared-asset disposal and static-reference reset. If patch cleanup is needed, remove only our
own methods through the guarded mechanism; never broadly unpatch another owner's work.
**Acceptance:** repeated init/shutdown/scene cycles do not accumulate entries, callbacks, roots,
leases or native assets; shutdown still finishes when an individual cleanup operation throws.

### S-16 — P2: potion routing can remove effects from every visible view

**Source-proven bounded-routing problem.**
[FoodModule](../../../src/GenesisUI/Modules/Food/FoodModule.cs#L35) has five potion tiles and
stops after that count. [StatusModule](../../../src/GenesisUI/Modules/Status/StatusModule.cs#L126)
excludes all PotionEffects unconditionally, even when Food is disabled or not showing overflow.
For a modpack with additional consumable effects, hidden status information is a real integration
gap. The exact number of simultaneous effects on this server was not measured.

**Remedy:** shared routing/ownership that only removes an effect once a visible destination
accepted it; bounded overflow/scrolling and a readable fallback.
**Acceptance:** Food off, module fault and more than five eligible effects retain access to every
non-hidden effect and duration, with no duplicate or missing effect after recovery.

### S-17 — P2: crafting material identity is inferred from its icon

**Source-proven ambiguity.**
[Crafting.UpdateMaterials/NameFor](../../../src/GenesisUI/Modules/Windows/CraftingWindowModule.cs#L655)
finds the resource name by matching the displayed Sprite against recipe resources. Distinct modded
resources sharing one icon resolve to the first match; same-sprite row changes can retain an old
cached name. Parsing the rendered amount also assumes plain numeric text. Craft execution still
uses vanilla; the incorrect result would be presentation/count information.

**Remedy:** stable resource identity/requirement position plus live vanilla values, validated
against single-resource and alternative recipes. Cache hierarchy bindings with recreation/version
checks rather than rediscovering transforms each frame.
**Acceptance:** shared-icon resources, changed requirements, alternate recipes and foreign count
formatting show the correct identity/have/need while vanilla remains the authority for crafting.

### S-18 — P2: allocation and render costs exceed what current diagnostics describe

**Source-proven allocation sites; performance impact unmeasured.** Food.Layout constructs an
array every refresh. SlotHotkeys.ToKey converts names through `ToString`/Enum parsing while keys
are held. Crafting reconstructs entries periodically and searches requirement objects every frame.
PreviewStage periodically allocates renderer arrays, and up to two independent stage cameras
render each visible frame. Each keyed target is capped at a 1024-pixel longest side and can use
2x sampling; that cap is useful, but it is not a measured frame-time budget.

ModuleHost's stopwatch measures module Refresh only, excluding independent stage callbacks,
GPU rendering and canvas rebuild cost. Average refresh cost is not full per-frame UI cost.

**Remedy:** precompute key mappings, reusable buffers, state/version invalidation and cached
bindings; instrument preview CPU/render counts/target sizes plus actual frame distributions and
GC allocation. Establish budgets from the same machine/profile before optimizing visual effects.
**Acceptance:** unchanged visible UI has measured steady-state allocation and CPU/GPU behavior;
warm-up spikes are distinguished from sustained costs. Preserve the already-approved appearance.

### S-19 — P2: diagnostics redaction and storage need explicit limits

**Source-proven scope gaps, not a claim of an actual disclosure.**
[ReportWriter.BuildRedactor](../../../src/GenesisUI/Foundation/ReportWriter.cs#L87) uses `?.` on
Unity objects, contrary to the project's destroyed-object rule, and silently ignores secret
collection failures. Redactor's known-name/minimum-length and IPv4/identifier patterns do not
cover arbitrary secret-bearing foreign logs, hostnames or IPv6. A '(personal data redacted)'
header is therefore a stronger promise than the implementation can guarantee.

Reports have no retention/total-size policy and use second-resolution filenames, which can
overwrite consecutive reports. Existing own-log rotation is a useful separate protection.

**Remedy:** schema/allowlisted diagnostic fields for adapter data, robust Unity checks, explicit
redaction-health information without printing secrets, unique filenames and bounded retention.
**Acceptance:** adversarial synthetic names/addresses/paths and collection failures do not leak
known secrets; repeated F8 creates bounded distinct reports. Do not commit player logs as fixtures.

### S-20 — P2: data hardening should match the strict-reader promise

**Pure reproduction.** [StrictJson.ReadNumber](../../../src/GenesisUI.Core/Data/StrictJson.cs#L144)
delegates grammar to `double.TryParse` after accepting broad numeric characters. It accepts
invalid JSON numbers `01`, `1.` and `-.1`. Current tests do not include those cases. This is a
format-contract issue, not a code execution path. Size/depth limits, duplicate-key rejection and
typed manifest validation are good protections.

**Conditional resource risk.** ThemeRuntime caps compressed sprite files at 4 MiB, but does not
inspect decoded PNG dimensions or enforce an aggregate native-texture budget before LoadImage.
A small compressed local image can request a much larger decoded allocation. No such input was
provided during this review.

**Remedy:** implement JSON number grammar explicitly; validate dimensions and aggregate image
budgets before Unity decoding. Keep data incapable of choosing code/types/external paths.
**Acceptance:** malformed-number fixtures are rejected; oversized dimensions and cumulative
budgets fail locally without allocating the full texture or disabling unrelated regions.

### S-21 — P2: map marker modifications are not restored

**Source-proven.** [MapMarkers.Refresh](../../../src/GenesisUI/Modules/Minimap/MapMarkers.cs#L108)
changes pin icon fields, drawn sprites and displayed label text. Teardown only sets Active=false.
After disabling, existing marker elements can retain our appearance until vanilla recreates or
updates them. This does not prove saved map data was altered: the module does not rewrite the
pin name here.

**Remedy:** preserve and restore original appearance per live pin/element without clobbering later
foreign-owner changes; prune destroyed/recreated objects.
**Acceptance:** disabling/faulting markers restores existing pins immediately and leaves foreign
pins, labels, new pins and saved names intact.

### S-22 — P3: rested effect looks for Vitals in the wrong module root

**Source-proven hierarchy mismatch.**
[Climate.Refresh](../../../src/GenesisUI/Modules/Climate/ClimateModule.cs#L117) uses
`_context.Root.Find("Vitals")`. ModuleContext.Root is Climate's own per-module host root; Vitals
is constructed under a separate Vitals root. This lookup cannot find that sibling view.

**Remedy:** expose a guarded visual anchor/capability rather than reaching into another module's
private hierarchy. **Acceptance:** the effect follows Vitals when available and turns off cleanly
when Vitals is disabled, blocked, faulted or rebuilt.

### S-23 — P2: Core layout plans do not reject duplicate logical IDs

**Pure reproduction; no current production duplicate-ID path identified.**
[LayoutChange.Compute](../../../src/GenesisUI.Core/InventoryModel/LayoutChange.cs#L52) stores
final positions in a dictionary keyed by Id. Duplicate IDs overwrite a result while the plan can
remain Ok. Production InventoryModule assigns unique list indices, so this is an API/input
invariant gap before adapters feed additional data into Core.

**Remedy:** reject duplicate IDs and invalid snapshots explicitly. Verify mapping by stable
identity when applying plans after foreign callbacks, not solely an assumed unchanged list order.
**Acceptance:** duplicate IDs, stale lists and mismatched snapshots are refused without applying
partial moves; randomized valid plans preserve uniqueness, identity and capacity invariants.

### S-24 — P2: compatibility and lifecycle tests lag behind the implementation

**Test coverage gap.** Existing Core tests cover important planning, animation, guards, leases,
redaction, JSON and shipped-art behavior. L2 checks declarations, isolation, merges and forbidden
calls. They do not exercise host subscription ordering, failed Build rollback, the patch cohort,
shared skins, dynamic equipment sanitization, input parity or any supplied adapter DLL.

**Remedy:** add regressions for the mechanisms above and tests against exact adapter reference
DLLs, serially with bounded heaps. Extend static scanning to the relevant gameplay writes and
visual-ownership operations with narrow documented exceptions; do not equate a pattern scan with
complete security proof. Keep game-only verification in a one-person client script.
**Acceptance:** every P1 has an automated regression or a clearly stated client/harness gate;
zero skipped mandatory adapter contracts in a release targeting this modpack.

### S-25 — P3: architecture and release documentation need capability status

**Documentation/reproducibility gap.** Architecture describes API/Adapters and foreign docking
that are not implemented; Extension API still says pre-1.0 although the product is 1.1.1.
The current UI views read game objects directly and the resource/capability design is only partly
realized. Package scripts accept an optional shader bundle without recording which source commit
produced it; the Windows package verification checks lengths rather than content hashes.

**Remedy:** label implemented/designed/deferred features, link this review, and use an explicit
release capability matrix. Record shader source/build provenance and archive contents by hash;
fail a build that requires keyed previews when its necessary bundle is absent. Keep existing
optional lit-sprite fallbacks where they are functional. No dependency upgrade is proposed here.
**Acceptance:** a package and its report identify runtime SHA, shader provenance, game refs and
supported adapter versions; docs do not promise unimplemented integrations.

## Coverage by subsystem

This table records the review's outcome, including areas where no separate finding was raised.
It is not a blanket approval of untested paths.

| Area | Examined behavior | Outcome / next gate |
|---|---|---|
| Core data/theme/formatting/motion | Bounds, validation, invariant formatting, bounded animation | Existing tests pass; S-20; add invalid-number and texture-budget checks |
| Core inventory/plans/sort | Capacity, equipment relocation, row cap, deterministic order | Positive base; S-03/S-23; persistence and foreign mutation need runtime gates |
| Foundation faults/logging/input | Event order, once-context logs, leases, redaction, rotation | S-01/S-07/S-19; preserve bounded logging and idempotent releases |
| Contracts/patcher | Per-class apply, rollback scope, existence/signature checks | Existing scan passes; S-03/S-12/S-24; no broad PatchAll in our code |
| Host/config/plugin | Registration, scheduling, construction, retries, toggles, shutdown | S-01/S-02/S-04/S-15; highest-priority boundary |
| Vitals/Sprint/liquids | HP/stamina/eitr, clocks, masks, materials, loss trails | Approved visual base; long-session/native-resource/performance gates remain |
| Food/Status/Climate | Effect routing, potion bounds, power state, climate/rested placement | S-16/S-18/S-22; mod effects require explicit routing |
| Hotbar/QuickSlots/KeyHints | Vanilla mirroring, hotkey focus, labels, nudges | S-10/S-14/S-18; ZenUseItem/ZenPlayer overlap study |
| Enemy/Boss | Dynamic plates, exact clone names, vanilla parent/movement, levels | Good reuse of vanilla lifetime; StarLevelSystem level/overflow state needs testing |
| Hover/Notice | Vanilla text/timing/icons, bounded notice cards | Useful mirroring base; foreign hover/tooltips and long/modded text need client comparison |
| Minimap/Large map/Markers | Live source texture/material, pins, large-map controls, restoration | S-05/S-14/S-21; level/season/Explorer overlays need visible slots |
| Shell/Inventory/Containers | Design-board layout, callbacks, drag, special cells, detail selection | S-05/S-08/S-09/S-11/S-14; Backpacks first major integration |
| Crafting/Build/Store | Vanilla action delegation, requirement rows, UI recreation | S-05/S-17/S-18; alternative recipes, many pieces, custom traders require tests |
| Skills/Achievements | Vanilla windows/data, dynamic content and list construction | Exact mod content should be compared before adding an adapter unnecessarily |
| Settings/Pause/Dialogs | Vanilla parenting, hide/restore, config reflection, modal input | S-02/S-05/S-14/S-15; config-manager ownership and modal priority |
| PreviewStage/ModelPreviews | Camera path, key filtering, texture cap, clone lifetime, dynamic dressing | Approved composition fix retained; S-06/S-09/S-13/S-18 |
| Shared UI/effect widgets/audio/fonts | Own graphics, UI callbacks, per-instance materials, common native caches | Most instance teardown exists; shared shutdown and complete callback guards needed |
| All shader sources | UI blending/masks, keyed sampling, procedural effects and bundle build | Keyed regression/GPU check already passed; foreign graphics/API tests not run |
| Art generation/fonts/package | Reproducible cuts/shape pipeline, manifest/filename bounds, pinned fonts, merge/icon | Existing shipped-art tests pass; S-20/S-25; no full concept or game asset shipped by review |
| Documentation/testing | Intended vs shipped API, phase status, game references and evidence | S-24/S-25; establish versioned capability certification |

## What is already worth preserving

Core remains separated from Unity/game dependencies. Foundation isolation and forbidden-API tests
pass. Patches are applied one class at a time with owner-filtered rollback. Layout planning before
mutation, bounded row counts, typed manifest validation, mirrored vanilla actions and dynamic
enemy plate parenting are sound building blocks. Preview composition now has an actual GPU
regression check. The review proposes strengthening these boundaries while retaining the approved
gold UI and D-032 assembly.

The next step is the stabilization sequence in [IMPLEMENTATION.md](IMPLEMENTATION.md), followed
by exact-version capability spikes. “100% functional” must mean every **enabled feature in a
versioned server/client configuration** is accounted for and tested; it cannot be inferred from
the presence of DLLs or from passing vanilla contracts.
