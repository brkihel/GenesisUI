# Stability implementation tracker

Authorized by Diego on 2026-10-02 after the [review](review/2026-10-02/README.md).
Branch: `fix/stability-1.1.2`. Preserve the approved preview as the visual baseline.
This tracks implementation separately from client verification and modpack certification.

Target: `1.1.2-preview.3`; [focused client script R-067](testing/scripts/R-067-character-rig-and-equip-progress.md).
R-065 received partial vanilla feedback: item details/models passed; character/cape, map,
Produce feedback, Esc timing and overlapping lore readers require the D-041 follow-up.
R-066 found almost everything satisfactory but rejected the baked character and exposed the
native equip-action bar. The D-041 amendment replaces scale conversion with a visual-only
skinned rig; D-042 mirrors action progress on item borders with reversible HUD veiling.
The fixes are implemented; focused client approval and FullPlaythrough certification remain pending.
The rows mean the base code correction is implemented, not that every acceptance scenario or
future mod-specific provider is certified. Detailed scope: [CAPABILITIES](CAPABILITIES.md).

| Finding | Correction | State / verification |
|---|---|---|
| S-01 | Deferred recovery and isolated fault subscribers | Implemented; client checks pending |
| S-02 | Partial Build rollback, ownership and roots | Implemented; client checks pending |
| S-03 | Inventory prerequisite cohort and safe transition | Implemented; client checks pending |
| S-04 | Minimal headless config sync initialization | Implemented; client checks pending |
| S-05 | Foreign window ownership and shell dependencies | Implemented; client checks pending |
| S-06 | Inactive visual-only dynamic preview copies | Implemented; client checks pending |
| S-07 | Owner-scoped callback/patch failure boundaries | Implemented; client checks pending |
| S-08 | Vanilla tooltip fallback and complete details | Implemented; client checks pending |
| S-09 | Presentation/model generation invalidation | Implemented; client checks pending |
| S-10 | Shared hotkey eligibility and rebinding | Implemented; client checks pending |
| S-11 | Vanilla input semantics and gamepad projection | Implemented; client checks pending |
| S-12 | Helper/base/signature/usage contracts | Implemented; client checks pending |
| S-13 | Stage fault policy and explicit 2D fallback | Implemented; client checks pending |
| S-14 | Shared exact CanvasGroup ownership/restoration | Implemented; client checks pending |
| S-15 | Idempotent plugin/host/shared-resource shutdown | Implemented; client checks pending |
| S-16 | Visible effect routing and overflow | Implemented; client checks pending |
| S-17 | Stable crafting resource identity | Implemented; client checks pending |
| S-18 | Cached paths and measured stage/frame diagnostics | Implemented; client checks pending |
| S-19 | Redaction health and bounded report retention | Implemented; client checks pending |
| S-20 | JSON grammar and decoded-image budget | Implemented; client checks pending |
| S-21 | Live map appearance restoration | Implemented; client checks pending |
| S-22 | Explicit Vitals visual anchor | Implemented; client checks pending |
| S-23 | Invalid/stale inventory plan rejection | Implemented; client checks pending |
| S-24 | Meaningful regression/contract gates | Implemented; client checks pending |
| S-25 | Accurate docs and shader/package provenance | Implemented; client checks pending |

No task in this tracker permits server/production changes or a claim of in-game verification
without Diego's result. Focused client steps will accompany the new committed Preview package.

## Scope and evidence

- S-08 preserves actual-instance vanilla tooltip text and native pointer handlers. Structured
  read-only providers/foreign interactive children belong to the separate F7 adapter/API work.
- S-18 removes identified avoidable hot-path allocations and adds recent frame/stage samples.
  GPU/GC/native-asset benchmarks and measured budgets still require the client environment.
- S-24 covers recovery ordering, shared claims, stale/invalid/partially failed plans, rollback
  blocked by foreign occupants, strict data/budgets/provenance and compiled native signatures.
  Per-adapter DLL/capability tests follow when each adapter is implemented.
- S-25 records the unchanged approved bundle against exact staged source hashes and its original
  Unity/Direct3D11 check. Packaging validates the archive contents, references and source SHA.
- Headless initialization, GUI lifecycle, native action hooks and the new preview C# code have
  not been exercised in a game/server process. No server process was launched or changed.

Automatic gates and remaining client work are recorded in
[R-065 result](testing/results/R-065-1.1.2-preview.1.md).
