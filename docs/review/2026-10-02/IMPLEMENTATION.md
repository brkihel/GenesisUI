# Stabilization and integration implementation sequence

This is a proposed backlog from the 2026-10-02 review, not completed implementation or phase
approval. Preserve D-023's gold direction and D-032's design-board assembly. New decisions are
required where the implementation changes the accepted architecture or gameplay scope.

## Batch 0 — freeze reproducible inputs

1. Keep approved preview commit `2a1854a` as the comparison point.
2. Reconcile production refs and installed-client binary fingerprints; do not replace production
   references silently with a different client's DLLs. Current declared contracts resolve on both.
3. Obtain the server-side package/config snapshot through an approved existing source. The supplied
   FullPlaythrough profile establishes client candidates, not every active server feature.
4. Resolve duplicate ExplorerBiomeCompat provenance and the MrDayNight version discrepancy in a
   future profile-maintenance task. No FullPlaythrough files were changed by the review.
5. Capture a disposable local test character/world and baseline screenshots/tooltips/key behavior.

**Output:** versioned capability inventory and source fingerprints, with enabled/disabled/unknown
flags. Unknown features are discovery items, not automatically a missing adapter.

## Batch 1 — lifecycle foundation

Fix S-01, S-02 and relevant S-07/S-14/S-15 paths before adding foreign providers.

- One owner lifecycle coordinator with Building/Active/Recovering/Unsupported states and generation.
- Resource acquisition/rollback for roots, skins, veils, nudges, leases and subscriptions.
- Cleanup completes before recovery; recovery remains bounded and keeps Diego's requested popup.
- Partial Build and failed cleanup never escape owner isolation; cleanup participants are independent.
- Shutdown is idempotent and clears native/shared resources and static references.

Move pure lifecycle ordering/state decisions into Core for meaningful regressions; use a narrow
Unity harness only where actual destroyed-object/component behavior is necessary. Do not write
tests that merely restate the implementation.

**Exit gate:** partial-failure injection matrix, retry exhaustion, toggle order and scene transition
counts pass; vanilla and surviving foreign-owner state restore exactly. Debug/Preview/Release pass.

## Batch 2 — inventory and input safety

Fix S-03/S-04/S-10/S-11/S-23 and the affected patch paths.

- Declare the inventory-height/placement/equipment patch cohort as a capability; no relocation
  until prerequisites are verified. Record status in F8.
- Transition plans preserve item identities/counts, verify actual height/capacity and roll back
  partial changes. Consider another patch changing the height after ours, not only a missing patch.
- Define minimal server configuration initialization separately from client UI/gameplay behavior;
  reconcile D-001/D-031 before changing headless behavior.
- Share input eligibility between activation and suppression; preserve vanilla/foreign entry points.
- Reject stale/duplicate planning snapshots and document item ownership during callbacks.

**Exit gate:** missing prerequisite, full/shrinking inventory, failed resize, reload and modifier/focus
cases do not alter item count/identity unexpectedly. One-person client script covers real save/load,
drag/use and configured gamepad behavior. No server/production tests are silently added.

## Batch 3 — preview, presentation and contract boundaries

Fix S-06/S-08/S-09/S-12/S-13/S-16/S-17/S-20/S-21/S-22 as the affected features are prepared.

- Sanitize dynamic preview attachments before behavior can awaken, preserve global flags and own
  stage faults. Provide explicit supported/failed state and an accessible 2D fallback.
- Track item/provider presentation versions independently of model creation.
- Expand runtime contracts to bases/helpers and exact reflected signatures; test actual foreign DLLs.
- Preserve vanilla tooltip information and route effects only to views that accepted them.
- Use resource identity in crafting and restore live marker appearance on disable.
- Bound data grammar, decoded images, provider output and total presentation resources.

**Exit gate:** selected item updates in place; unsupported shaders/providers remain usable;
dynamic gear and same-count swaps cannot create live gameplay components; information survives
overflow and disabled modules. Approved shader filtering regression remains green.

## Batch 4 — implement the minimum F7 surface

Start from the existing draft interfaces. Ship a small versioned runtime API in stages, rather
than implementing all proposed hooks before testing a real consumer.

| Surface | First consumer | Boundary |
|---|---|---|
| Item sections/badges | Jewelcrafting | Read-only data, bounded rows/text/icons, vanilla tooltip fallback |
| Equipment slot descriptor | Backpacks, HipLantern, jewelry | Stable slot identity, original state/action owner, explicit capacity policy |
| Container descriptor | Backpacks | Read projection + original UI/vanilla handlers; no transfer/storage reimplementation |
| HUD resource/widget descriptor | Seasonality, Sagas, MagicRevamp if needed | Source-owned values/events, layout region, refresh/cost budget |
| Creature extras | StarLevelSystem | Numeric level/extra fields, original creature lifetime |
| Shell tab / foreign panel | Sagas or remaining foreign UI | Modal ownership, access path, exact hide/restore |

Each registration needs an owner GUID, provider ID, supported versions/contracts, priority/region,
refresh budget, explicit fallback and disposable registration token. Resolution is deferred until
Build/verification; no foreign reflection in module constructors/static initializers. Provider calls
run on the Unity main thread through a guard with local failure and bounded diagnostics.

Separate **read-only providers** from **approved action routes**. A provider must not return arbitrary
type/method/path names from JSON. An action must call a reviewed vanilla or owning-mod UI entry
point, with contracts and a narrow policy; methods that add/delete items or change sockets are not
accepted just because the foreign API is public. Any expanded gameplay scope needs a decision.

Foundation must remain independent of API/modules/theme. Implement lifecycle primitives there;
implement provider orchestration above it. Core receives pure snapshots rather than Unity objects.
Optional integrations must not create a hard runtime dependency or force every modpack client to
install an otherwise optional mod. No new runtime dependency is proposed by this report.

**Exit gate:** one real provider can register before/after a view exists, unregister/rebuild safely,
fail in isolation, and report Unsupported for a deliberately mismatched exact DLL. Schema output
limits are tested and unknown foreign UI stays accessible.

## Batch 5 — first functional integration slices

Implement and test in this order, adjusting only for Diego's actual enabled server features:

1. Backpacks read projection + slot + original open/action path. Establish equipment/container
   contracts and remove dependence on the SeneaL-specific bridge in an isolated migration profile.
2. Jewelcrafting sockets/details + ring/amulet + original socket/table interaction.
3. HipLantern independent state/action/preview.
4. MagicRevamp configured spells/resources/cooldowns; add providers only for missing presentation.
5. ZenPlayer/ZenUseItem interactions and the combined death/persistence/equipment test.
6. StarLevelSystem, Seasonality/MrDayNight, Sagas/Suite widgets/tabs and no-map behavior.
7. Skills/achievements/hover/build/content/container cases: certify mirroring first, adapt gaps.

Every slice includes exact-version adapter docs under `docs/adapters/`, reference DLL contracts in
`ref/adapters/` (never committed), diagnostic visibility, focused Core/L2 tests and a pt-BR client
script. Record results before fixing any newly observed issue. Do not infer safe upgrade support
from a successful load. Keep individual version rows and combined-case results.

## Batch 6 — modpack performance and release certification

Address S-18/S-19/S-24/S-25 and complete certification.

- Measure baseline versus GenesisUI on the same PC/profile/resolution/GUI scale, distinguishing
  warm-up, idle HUD, open inventory with two previews, long lists and active mod effects.
- Capture frame distributions, CPU/GPU time, GC allocations, native assets, stage render counts
  and texture sizes. Current module Refresh averages alone are insufficient.
- Optimize verified hot paths with dirty versions/reused buffers. Choose budgets from measured
  evidence; no performance numbers are invented in this review.
- Add schema-based adapter diagnostics, safe redaction, bounded report retention and exact archive
  hashes/provenance. A full report must identify providers blocked/unsupported/faulted/active.
- Update architecture/API status and the server/client installation contract.

**Release gate:** every enabled capability has a visible location and functioning original action,
tested fallback, exact version/config row, and owner. No unresolved P1; all three channels and
mandatory contracts pass without skips; one-person FullPlaythrough combined script passes on a
disposable client profile. Diego approves the visuals/behavior. Production installation and merge
to main remain separate authorized actions under the project workflow.

## Backlog mapping

| Batch | Findings | Dependency |
|---|---|---|
| 0 | Reference provenance; profile discrepancies | Before support claims |
| 1 | S-01, S-02, S-07, S-14, S-15 | Before provider registration/recovery |
| 2 | S-03, S-04, S-10, S-11, S-23 | Before foreign equipment/containers/persistence |
| 3 | S-06, S-08, S-09, S-12, S-13, S-16, S-17, S-20, S-21, S-22 | Before the respective presentation feature |
| 4 | S-05, S-12, S-24, S-25 | Before certifying optional integrations |
| 5 | MODPACK.md capability slices | Batches 1–4 plus exact feature discovery |
| 6 | S-18, S-19, S-24, S-25 | Combined-client release gate |

This ordering is a proposal for concrete, reviewable small changes. It does not authorize
silent scope expansion, production changes, or claims that untested modpack features are supported.
