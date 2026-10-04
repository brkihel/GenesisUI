# GenesisUI — Harmony Patch Policy

Every patch is a promise that some other mod's patch on the same method still works.
This policy exists because the common failure of big UI mods is not a bug in their
own screen, but what their patches do to everyone else.

## Rules

1. **Postfix first.** Read state after vanilla ran. Most UI needs nothing else.
2. **Prefix only to observe or to guard**, returning `void`. A prefix that returns
   `bool` (skips the original) is **forbidden** unless a decision in
   [DECISIONS.md](DECISIONS.md) names the method, the reason, and why every other
   postfix/prefix on that method still receives what it expects.
3. **No transpilers** without a decision entry. When one is accepted, it must fail
   closed (`CodeMatcher` that does nothing if the pattern is not found) and have a
   contract test on the IL pattern.
4. **Never `Harmony.Unpatch` a patch that is not ours.** Conflicts are solved by
   region ownership ([ARCHITECTURE.md §3](ARCHITECTURE.md#region-registry)), by
   `HarmonyPriority`/`HarmonyAfter`, or by not taking the region.
5. **One patch class per target area**, applied individually through the
   Foundation guarded patcher. A missing target disables that class and the
   classes that declared it in `[GameContract]`, nothing else. `PatchAll()` over the whole
   assembly is forbidden.
6. **Every patch target is a contract.** It appears as a `[GameContract]` on the patch class and in
   `GenesisUI.Contract.Tests`. A patch without a contract test does not merge.
7. **Patch bodies are thin.** They hand data to the Scheduler or a reader and
   return. No allocation in per-frame patches. Exceptions are caught by the guard;
   a patch body never throws into vanilla.
8. **No patches on other mods' code** except inside an adapter, with a decision
   entry and a contract test against that mod's DLL.
9. **Headless servers get no GenesisUI visual/gameplay patches.** Under D-031/D-040,
   minimal inventory configuration sync binds before the UI-only exit. Its already
   vendored ServerSync config-sync patches remain the separate allowed exception.

## Review checklist for a patch

- [ ] Postfix or void prefix? If not, where is the decision entry?
- [ ] Target declared as `[GameContract]` on the patch class (the contract test then covers it)?
- [ ] What happens to other mods' postfixes on this method? (write it in the PR)
- [ ] Allocation-free if the method runs every frame?
- [ ] Behaviour when the module is `Disabled`, `Suspended` or `Faulted`: the patch
      must be a no-op.

### D-043/D-044 inventory additions

Use remains a void guarding prefix. Equip relocation follows the supported mod owners.
CanAddItem's overflow correction is first priority; normal-priority foreign postfixes still
apply their restrictions afterwards. Both Inventory.Load overloads have observing/metadata
prefixes and restoring finalizers, and retain originals, foreign patches and exceptions.
The D-044 load service intentionally remains active with wallet drawing disabled to avoid
saved coin truncation. No Harmony patch targets a foreign method.

Preview.2 extends only the existing GetHoveredElement postfix: inventory and gem-editor
projections return the exact native element under their visible cell, each guarded by its
own window owner. Original execution and every foreign prefix/postfix remain unchanged;
inactive/faulted projections do nothing. The default-key correction, scale controls and
station-preserving native UI commands introduce no new patch target or skipping prefix.
