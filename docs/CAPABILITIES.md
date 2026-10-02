# Shipped capability status

Baseline: Release `1.1.2`, tag `v1.1.2`, source `e378afa`, approved by Diego on 2026-10-02.
R-065/R-066 feedback led to the approved R-067 visual rig and progress replacement. Overall
Release approval is separate from individually reported scenarios and modpack certification.
The immutable review remains evidence of its earlier source; see [release record](releases/1.1.2.md).

| Surface | Implementation | Verification / remaining work |
|---|---|---|
| HUD and in-game windows | Shipped, guarded modules, native input/action delegation | Release approved after R-067; detailed scenario results not all supplied |
| Inventory layout | Position-only snapshot/journal, full patch prerequisite, grow-before-move, rollback | Core/L2 gates pass; included in approved Release; save/reentry/foreign combinations not individually certified |
| Headless inventory config sync | Minimal D-031 initialization before visual exit | Metadata/build verified; headless runtime not exercised, no server rollout |
| 3D previews | Character: new transforms/private meshes/remapped bones, preserved bind poses, static cape pose; items: sanitized visual copies; keyed/2D fallback | R-067 approved; exact-helper GPU fixture passes nine scale pairs; fresh source-bound keyed/edge/orbit GPU checks pass |
| Equipment progress | Native active equip/unequip action mirrored by an orbiting border; conditional hud.action veil and text fallback | Included in approved Release; native queue/timing/success hooks remain authoritative |
| Lore and map/menu visuals | One unframed rune reveal, native dismissal, uncovered map, active-root Esc visibility | R-066 feedback almost satisfactory; final preview approved for Release |
| Item details | Actual instance's complete bounded scrollable vanilla tooltip; original pointer handlers | Unknown foreign tooltip children/interactive sections may need an adapter |
| Crafting resources | Native SetupRequirement binding; shared name identity, native formatted amount/color | Same-icon and modded requirement client comparisons pending |
| UI ownership | Logical window/build/map/action reservations; known SeneaL ownership blocks conflicts | Conservative coexistence; original foreign UI stays the owner, no implicit certification |
| Diagnostics/package | Bounded metrics/reports; shader/build evidence; ZIP content hashes | Release ZIP payloads/stamp verified; compiled Overlay/Watermark types absent; F8 report retained; POSIX/remote builds require their own environment |
| Extension API and generic foreign dock | Design only (F7), no public provider registration | Disposable read-only providers/bounds/version contracts planned |
| Backpacks, Jewelcrafting, HipLantern | No independent slot/container/socket adapters shipped | Exact-version resource/action slices described in the modpack study |
| Other FullPlaythrough capabilities | Existing vanilla mirroring only | Per-feature/version/config certification pending, including combined interactions |
| Performance budget | Hot-path caches and measurement support implemented | No benchmark/GPU/GC improvement claim; measure on Diego's client |

Diego explicitly approved Release packaging and repository/main synchronization. Production
installation and full-modpack certification remain separate. Native/foreign logic was inspected
to understand behavior; none is copied or shipped.
