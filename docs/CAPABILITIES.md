# Shipped capability status

Baseline: `1.1.2-preview.1`, stability implementation authorized on 2026-10-02.
Implementation is separate from client verification. The approved visuals are R-063/1.1.1-preview.2;
R-065 tests the new runtime behavior. The immutable review remains evidence of the earlier source.

| Surface | Implementation | Verification / remaining work |
|---|---|---|
| HUD and in-game windows | Shipped, guarded modules, native input/action delegation | Earlier visuals approved; stability regression R-065 pending |
| Inventory layout | Position-only snapshot/journal, full patch prerequisite, grow-before-move, rollback | Core/L2 gates; native callbacks and save/reentry need R-065 |
| Headless inventory config sync | Minimal D-031 initialization before visual exit | Metadata/build verified; headless runtime not exercised, no server rollout |
| 3D previews | Inactive sanitized visual snapshots, generation/content invalidation, keyed/2D fallback | Unchanged shader GPU result reused; new C# clone/scene behavior needs R-065 |
| Item details | Actual instance's complete bounded scrollable vanilla tooltip; original pointer handlers | Unknown foreign tooltip children/interactive sections may need an adapter |
| Crafting resources | Native SetupRequirement binding; shared name identity, native formatted amount/color | Same-icon and modded requirement client comparisons pending |
| UI ownership | Logical window/build/map reservations; known SeneaL ownership blocks conflicts | Conservative coexistence; original foreign UI stays the owner, no implicit certification |
| Diagnostics/package | Bounded metrics/reports; shader/build evidence; ZIP content hashes | Windows package path exercised; POSIX/remote build paths require their own environment |
| Extension API and generic foreign dock | Design only (F7), no public provider registration | Disposable read-only providers/bounds/version contracts planned |
| Backpacks, Jewelcrafting, HipLantern | No independent slot/container/socket adapters shipped | Exact-version resource/action slices described in the modpack study |
| Other FullPlaythrough capabilities | Existing vanilla mirroring only | Per-feature/version/config certification pending, including combined interactions |
| Performance budget | Hot-path caches and measurement support implemented | No benchmark/GPU/GC improvement claim; measure on Diego's client |

No installation on production, merge to main or full-modpack certification follows from automatic
tests. Native/foreign logic was inspected to understand behavior; none is copied or shipped.
