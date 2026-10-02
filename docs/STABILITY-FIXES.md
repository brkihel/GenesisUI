# Stability implementation tracker

Authorized by Diego on 2026-10-02 after the [review](review/2026-10-02/README.md).
Release source: `v1.1.2` / `e378afa`; integrated into `main` with Diego's explicit authorization.
This tracks implementation separately from client verification and modpack certification.

Release target: `1.1.2`; Diego approved preview.3 and authorized packaging for his Hexium upload.
Evidence: [R-067](testing/results/R-067-1.1.2-preview.3.md).
R-065 received partial vanilla feedback: item details/models passed; character/cape, map,
Produce feedback, Esc timing and overlapping lore readers prompted the D-041 follow-up.
R-066 found almost everything satisfactory but rejected the baked character and exposed the
native equip-action bar. The D-041 amendment replaces scale conversion with a visual-only
skinned rig; D-042 mirrors action progress on item borders with reversible HUD veiling.
The fixes are implemented and the preview is approved for Release. Detailed per-step results
were not supplied; FullPlaythrough certification remains pending.
The rows mean the base code correction is implemented, not that every acceptance scenario or
future mod-specific provider is certified. Detailed scope: [CAPABILITIES](CAPABILITIES.md).

| Finding | Correction | State / verification |
|---|---|---|
| S-01 | Deferred recovery and isolated fault subscribers | Implemented; included in approved 1.1.2 |
| S-02 | Partial Build rollback, ownership and roots | Implemented; included in approved 1.1.2 |
| S-03 | Inventory prerequisite cohort and safe transition | Implemented; included in approved 1.1.2 |
| S-04 | Minimal headless config sync initialization | Implemented; included in approved 1.1.2 |
| S-05 | Foreign window ownership and shell dependencies | Implemented; included in approved 1.1.2 |
| S-06 | Visual-only rig/private meshes and sanitized item copies | Implemented; included in approved 1.1.2 |
| S-07 | Owner-scoped callback/patch failure boundaries | Implemented; included in approved 1.1.2 |
| S-08 | Vanilla tooltip fallback and complete details | Implemented; included in approved 1.1.2 |
| S-09 | Presentation/model generation invalidation | Implemented; included in approved 1.1.2 |
| S-10 | Shared hotkey eligibility and rebinding | Implemented; included in approved 1.1.2 |
| S-11 | Vanilla input semantics and gamepad projection | Implemented; included in approved 1.1.2 |
| S-12 | Helper/base/signature/usage contracts | Implemented; included in approved 1.1.2 |
| S-13 | Stage fault policy and explicit 2D fallback | Implemented; included in approved 1.1.2 |
| S-14 | Shared exact CanvasGroup ownership/restoration | Implemented; included in approved 1.1.2 |
| S-15 | Idempotent plugin/host/shared-resource shutdown | Implemented; included in approved 1.1.2 |
| S-16 | Visible effect routing and overflow | Implemented; included in approved 1.1.2 |
| S-17 | Stable crafting resource identity | Implemented; included in approved 1.1.2 |
| S-18 | Cached paths and measured stage/frame diagnostics | Implemented; included in approved 1.1.2 |
| S-19 | Redaction health and bounded report retention | Implemented; included in approved 1.1.2 |
| S-20 | JSON grammar and decoded-image budget | Implemented; included in approved 1.1.2 |
| S-21 | Live map appearance restoration | Implemented; included in approved 1.1.2 |
| S-22 | Explicit Vitals visual anchor | Implemented; included in approved 1.1.2 |
| S-23 | Invalid/stale inventory plan rejection | Implemented; included in approved 1.1.2 |
| S-24 | Meaningful regression/contract gates | Implemented; included in approved 1.1.2 |
| S-25 | Accurate docs and shader/package provenance | Implemented; included in approved 1.1.2 |

Diego approved Release and repository/main synchronization. Server/production changes and
individual scenario certification still require their own evidence. R-065/R-066/R-067 scripts
remain available for regression runs.

## Scope and evidence

- S-08 preserves actual-instance vanilla tooltip text and native pointer handlers. Structured
  read-only providers/foreign interactive children belong to the separate F7 adapter/API work.
- S-18 removes identified avoidable hot-path allocations and adds recent frame/stage samples.
  GPU/GC/native-asset benchmarks and measured budgets still require the client environment.
- S-24 covers recovery ordering, shared claims, stale/invalid/partially failed plans, rollback
  blocked by foreign occupants, strict data/budgets/provenance and compiled native signatures.
  Per-adapter DLL/capability tests follow when each adapter is implemented.
- S-25 records the fresh R-067 keyed/edge/orbit bundle against exact staged source hashes and
  Unity/Direct3D11 probes. Release packaging validates archive contents, references and source SHA.
- Diego's client feedback and approval are recorded in R-065/R-066/R-067. No agent game/server
  process was launched or changed; headless runtime and combined modpack scenarios are not certified.

Automatic gates, approval and remaining scenario scope are recorded in
[R-067 result](testing/results/R-067-1.1.2-preview.3.md) and [Release 1.1.2](releases/1.1.2.md).
