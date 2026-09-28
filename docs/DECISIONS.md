# GenesisUI — Decisions

Short records of decisions and why. New decisions get the next number; a reversed
decision is marked **Superseded by D-0XX**, never deleted.

## D-001 — Visual and client-only scope

**Decision:** until F7, GenesisUI changes only what the player sees. No RPC, no synced
config, no game-state changes. `NetworkCompatibility(NotEnforced)`.
**Why:** a UI mod that moves items becomes a duplication/loss vector; the SeneaL UI
study showed chest items taken with forced ZDO ownership at up to 100 m. Keeping the
UI visual makes it safe by construction.

## D-002 — GenesisModLIB is a separate runtime plugin

*Deferred by D-015: still the target format, applied when the LIB is extracted.*

**Decision:** shared infrastructure lives in GenesisModLIB, loaded as its own plugin
and consumed with a hard `BepInDependency`, strict semver.
**Why:** fault isolation and diagnostics need one shared registry across all
GenesisMods mods; a copy merged into each mod with ILRepack cannot share state.

## D-003 — Adapters live in-tree

**Decision:** third-party adapters live inside GenesisUI, isolated per folder. A
separate adapter package is an exception with its own decision entry.
**Why:** one package to install and test; the public API covers mods that want to
integrate themselves.

## D-004 — Two public repositories, Hexium packages

*Partially superseded by D-015: only GenesisUI is published for now.*

**Decision:** `GenesisUI` and `GenesisModLIB` are public repositories; packages follow
the Hexium format under `GenesisMods`; GUIDs `Genesis.GenesisUI` and
`Genesis.GenesisModLIB`.

## D-005 — Clean room

**Decision:** no code, art, icon, text or configuration layout copied from SeneaL UI
or any closed mod; no redistributed game assets; concept art is reference only and
stays out of the repository.
**Why:** legal safety for a public project and independence from any author.
Studying how a mod behaves is fine; its implementation is not ours to reuse.

## D-006 — Postfix-first, per-class patching, never unpatch others

See [PATCH-POLICY.md](PATCH-POLICY.md).
**Why:** SeneaL UI 1.1.6 applies ~190 patches with one `PatchAll` (one missing
target breaks all of them), has 50 skipping prefixes, and removes other mods'
patches. Those are the stability failures GenesisUI exists to avoid.

## D-007 — Hide vanilla, never destroy it

**Decision:** vanilla UI is veiled and restored exactly; never destroyed or
deactivated. **Why:** other mods locate and extend vanilla objects; the kill switch
must always work.

## D-008 — The UI acts only through vanilla entry points

**Decision:** clicks call the same vanilla methods as the vanilla UI; the banned-API
scan enforces it. **Why:** vanilla already handles ownership, container locks and
networking correctly.

## D-009 — Art as source

**Decision:** ornaments, frames, logo and nav icons are SVG sources in `art/src/`,
rasterized to an atlas at build time. Fonts are OFL files loaded at runtime. Game
sprites are read at runtime only. **Why:** we have no artist; vector sources are
reviewable, re-colorable by theme tokens, resolution-independent and license-clean.

## D-010 — Diagnostics mandatory outside Release

**Decision:** Debug and Preview builds ship the overlay, inspector, fault injection,
own log file and watermark. **Why:** tests happen on Diego's client; the build must
explain its own failures. See [DIAGNOSTICS.md](DIAGNOSTICS.md).

## D-011 — Client test scripts in pt-BR, executed by Diego

**Decision:** no server testing at this stage; each preview ships a step-by-step test
script in Portuguese, following [the template](testing/ROTEIRO-TEMPLATE.md).
**Why:** Diego runs the tests and must know exactly what is being verified. Scripts
are the one documentation exception to "English in the repository".

## D-012 — Pure Core

**Decision:** `GenesisUI.Core` targets netstandard2.0 without Unity, Valheim, Harmony
or BepInEx, with a build guard (GenesisPlayerBots D-001 pattern).
**Why:** most logic becomes testable with `dotnet test` on a one-person team.

## D-013 — Data files parsed with `JsonUtility`

*Superseded by D-018.*

**Decision:** layout and theme files use Unity `JsonUtility` into typed classes.
**Why:** no extra dependency, no polymorphic deserialization, nothing in a data file
can name a type or load code.

## D-014 — Fonts: Cinzel + Cormorant Garamond

**Decision:** Cinzel SemiBold/Medium for display and labels, Cormorant Garamond for
body, per Diego's font note for the concepts. Numeral legibility at HUD sizes is
checked in F2 and may add a numbers-only exception.

## D-015 — Infrastructure starts inside GenesisUI

**Decision (2026-09-28):** no GenesisModLIB release for now. Guard, guarded patcher,
contracts, input leases and diagnostics plumbing are built in GenesisUI's
`Foundation` layer, which never references UI code (enforced by test). When a second
GenesisMods mod needs any of it, `Foundation` is extracted into GenesisModLIB as a
separate plugin (D-002). Only the GenesisUI repository is published until then.
**Why:** there is no second consumer today (GenesisTooltips was never really started),
so a separate plugin would only add a dependency to install, version and whitelist.
Designing Foundation as extraction-ready keeps the door open at no cost.

## D-016 — The patcher may unpatch its own methods, and nothing else

**Decision:** `Foundation.GuardedPatcher` may call `Harmony.Unpatch` to roll back a patch
class that failed halfway, removing only patch methods declared by that class under our
own Harmony id. It is the one allow-listed caller in the banned-API test.
**Why:** without rollback, a class that patched two of three targets before throwing
would stay half-applied. Filtering by id and declaring type keeps PATCH-POLICY rule 4.

## D-017 — Compile against copies of production, not NuGet

**Decision:** the plugin references `ref/*.dll` copied from the production server by
`tools/fill-ref.sh`, including its `Jotunn.dll`; no JotunnLib NuGet package, no
publicized assemblies. Private members are reached through declared contracts and
`AccessTools`. The package's Jötunn dependency is checked against `ref/Jotunn.dll`.
**Why:** compiling and running against the same binaries removes a class of load-time
`MissingMethodException`s, works on Linux without Jötunn's prebuild task, and keeps
every private access visible as a contract.

## D-018 — Our own strict JSON reader for data files

**Decision (2026-09-28):** GenesisUI's data files (sprite manifest now; layout and theme
later) are read by `GenesisUI.Data.StrictJson` in Core: bounded size and depth, no
duplicate keys, unknown properties reported as errors, never half-parsed. It returns plain
dictionaries and lists; typed readers in Core map them and validate.
**Why:** in 0.2.0-preview.1 (R-010) Unity's `JsonUtility` read `scale` but left the sprite
array empty without any error, so the bars rendered as plain rectangles. The likely cause
(types made internal by the ILRepack merge) could not be confirmed offline, and a silent
partial read is the failure D-013 wanted to avoid. A Core reader is testable, and
`ShippedArtTests` now runs the real `art/out/sprites.json` through it at build time.

## D-019 — The minimap mirrors vanilla's small map

**Decision (2026-09-28):** the round minimap does not redraw the map. It shows vanilla's
small-map texture with vanilla's own material (fog of war included) and uvRect inside our
circular Mask, and mirrors every image under vanilla's small pin root (position, size,
rotation, sprite, colour, one nested image such as the "checked" cross), plus the player,
ship and wind markers and the biome text. Vanilla keeps running underneath, veiled.
**Why:** re-implementing pin placement and visibility would duplicate vanilla logic and
break with every mod that adds pins; mirroring what vanilla already decided shows exactly
what vanilla would, mod pins included, and can never reveal unexplored areas. The Mask
renders a copy of the material, so the four runtime properties vanilla changes
(`_zoom`, `_pixelSize`, `_mapCenter`, `_SharedFade`, read from the decompiled `Minimap`)
are copied every frame. If the map shader has no stencil support, the module logs it and
covers the square corners instead of pretending to be round.

## D-020 — One generated ornament language

**Decision (2026-09-28):** every UI shape SVG is generated by `tools/art/shapes.py` from a
single style module (`tools/art/style.py`): two line systems (containers: gold outer, bronze
inner; cells: bronze outer, gold hairline), one fill, three motifs. Hand-drawn SVGs are no
longer accepted for UI shapes. **Why:** R-030 — the pieces were each approved but did not read
as one family; shared constants make cohesion a property of the build instead of a matter of
care.
