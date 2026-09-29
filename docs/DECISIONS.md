# GenesisUI — Decisions

Short records of decisions and why. New decisions get the next number; a reversed
decision is marked **Superseded by D-0XX**, never deleted.

## D-001 — Visual and client-only scope

**Decision:** until F8 (renumbered by D-024), GenesisUI changes only what the player sees. No RPC, no synced
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

*The circular rendering mechanism below was superseded by D-021; the mirroring rule remains.*

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

## D-021 — Render the map with a circular mesh

**Decision (2026-09-28):** draw the terrain through a circular UI mesh using the
live vanilla material directly. Keep a standard Unity mask only around the pin and
player-marker images. Continue mirroring vanilla's UV, material, textures and pins.
**Why:** R-031 still showed grey terrain after the module followed the live material.
The previous Unity `Mask` created a stencil copy of that material; its game-managed
map textures could become stale on the copy. A circular mesh makes that copy
unnecessary while retaining the same fog-of-war shader. Client confirmation is
required before treating the grey-terrain bug as resolved.

## D-022 — Selectable art styles (superseded by D-023)

**Decision (2026-09-28):** GenesisUI ships two complete art styles, `carved` (carved wood
with iron rivets and a V rune; the default while Diego evaluates) and `gold` (the D-020
filigree), selected by `[Theme] Style` and switched live. Both draw the same sprite names;
each has its own `art/out/<style>/sprites.json` with sizes, 9-slice borders and content
insets, read by views through `ThemeRuntime.Border/Content`. D-020 now applies per style:
one generated ornament language inside each style. Two supporting mechanisms come with it:
`VanillaNudge` (a reversible offset for vanilla elements that collide with our layout, used
for the key hints) and dynamic regions (vanilla objects created on demand, such as boss
HUD clones, veiled as they appear).
**Why:** Diego (after R-032) designed a carved-wood bar that reads more like Valheim and
asked for it as a selectable style rather than a replacement, to decide later which
direction continues. Shared names and manifest-driven measurements keep modules
style-agnostic, so a second style costs art, not code.

## D-023 — Gold is the only art direction

**Decision (2026-09-28):** the `carved` style is removed (code, art, config and docs) and the
style selector with it. GenesisUI has one art direction, the gold language of D-020; art ships
flat in `plugins/art/` with one `art/out/sprites.json`. What D-022 introduced and stays useful
is kept: content insets in the manifest, `VanillaNudge`, dynamic regions. The liquid effects
and the compact value plate from the R-040 comparison apply to gold.
**Why:** Diego, after the 0.5.0-preview.2 mock: the carved rendition did not reproduce his
reference and was unpleasant; focus on gold from now on. A second style doubles every art
change for no benefit while one direction is being finished.

## D-024 — The whole vanilla UI before mod integration

**Decision (2026-09-28):** phases are reordered. After the HUD (F3): F4 inventory and crafting
windows (inventory, equipment, containers, crafting and every station, skills, texts, trophies,
build browser), F5 menus (main menu, pause menu, settings), F6 map, and only then F7 Extension
API, foreign-element dock and adapters. The optional gameplay package becomes F8, the switch-over
F9.
**Why:** Diego: GenesisUI, like SeneaL UI, is a complete rework of the vanilla UI, main and Esc
menus included; adapting other mods only makes sense once the vanilla screens they plug into
exist in our form.

## D-025 — Windows: rearrange and dress vanilla, keep its behaviour

**Decision (2026-09-28):** the F4 windows reach the concept layout by moving, resizing and
restyling vanilla's own panels (`VanillaSkin`, reversible like the veil) plus our own panels
that call only vanilla's public entry points. Every item operation stays vanilla code.
**Why:** all item moves, equip swaps, container transfers and crafting live in
`InventoryGui`'s private handlers, the exact place where item loss and duplication happen,
and many mods patch them. Diego asked for the concept's layout (ConceptArt 9 and 12); moving
vanilla's pieces gives that freedom without rewriting the dangerous part. See F4-PLAN.md.

## D-026 — Art cut from Diego's concept images

**Decision (2026-09-28):** UI pieces may be cut from Diego's concept images (the images are his:
generated with ChatGPT and edited by him in Photoshop), upscaled to 4K with Real-ESRGAN, cleaned
by `tools/art/extract.py` and committed as sprites with their 9-slice borders and content
insets. The full concept images and their 4K copies stay outside the repository. Game icons are
never taken from the concept. This amends D-020 (shapes generated only) for these pieces;
patterns and anything the concept does not show are still generated.
**Why:** Diego wants the concept's exact ornaments; a test on the chest card showed they can be
isolated and stretched cleanly.

## D-027 — HUD art from Diego's isolated texture sheets

**Decision (2026-09-28):** every HUD piece that Diego's sheets
(`~/GenesisUI-Concept/GenesisUI-textures/`) contain is cut from them by `tools/art/sheets.py`
into `art/src/sheets/` and replaces the generated or concept sprite of the same role. The
script resamples the metal (one uniform, premultiplied scale per piece; the three vital
frames share one) and never redraws it. Variable-size frames are 9-sliced only across plain
straight rails; an ornament in the middle of an edge is cut out as its own sprite (`inset` in
the manifest) and put back at the edge's midpoint. Each frame ships `<name>_shape` (its
silhouette: the panel material is clipped to it and has its own opacity per panel,
`[Backgrounds]`) and, when it has a window, `<name>_opening`. The minimap ring is rebuilt at
250 from the sheet's small ring by sampling its rail at the same angle and rail distance, with
the four diamonds cut whole. The liquids and the panel material are made seamless by
cross-fading their wrap before they scroll or tile. The manifest gains two optional integers,
`inset` and `gap` (the hotbar's cell spacing). Concept crops (D-026) remain only for pieces
the sheets lack (value plate, wind disk and arrow, stars, badge).
**Why:** Diego drew isolated, clean pieces after the concept crops kept scenery and the
generated redraws flattened the metal; AGENTS.md §2b sets the rules (no deformation,
independent background opacity, animated liquid in all three bars).
**Amended after R-042:** eitr shares the stamina frame; the horizontal stamina readout is sheet
3's slim bar; hotbar cells are measured from the art (`cells`), with Diego's selected and
equipped cell frames. Every element is exported unscaled to `~/GenesisUI-Concept/GenesisUI-cuts/
original/`, and a same-size retouched copy in `edited/` replaces the cut: Diego adjusts colour
there without anything being cut again. The creature plate is **not** from the sheets: it keeps
the generated boss plate drawn small, as approved in F3 (Diego, after R-042).

## D-028 — Inventory gameplay is built inside F4, in the same plugin

**Decision (2026-09-29, amending the proposal of 2026-09-28):** the inventory gameplay Diego
wants (admin-set slot count up to 48 including the hotbar, 4 quick-use and 4 utility slots, an
equipment panel holding every equipped item, sort) is built in F4, **inside the GenesisUI
plugin** — no separate package (Diego: non-negotiable). It is an isolated module
(`Gameplay/`) with its own `[Modules]` toggle, guard and diagnostics; the admin's settings come
from the server through ServerSync; the plugin's network compatibility and the AzuAntiCheat
whitelist are reviewed when it lands. A deep study of current (post-1.0) inventory mods on the
Hexium store and of vanilla's inventory save/load comes first (`docs/GAMEPLAY.md`), before any
code that moves items. This supersedes VISION's "not a gameplay mod" for the inventory.
**Why:** Diego wants the inventory whole in this phase and one plugin to install. Safety comes
from isolation inside the plugin and from the study, not from a second DLL.

## D-029 — One gold for every piece of art

**Decision (2026-09-28):** `tools/art/sheets.py` recolours every piece cut from Diego's sheets
through one ramp taken from his colour reference (`tools/art/gold_ramp.json`, from
`elements-color.jpg`): each pixel keeps its luminance rank and alpha and takes the ramp's colour
at the reference's (darkened) luminance. Pieces with their own colour (green equipped, grey
disabled, red) are exempt; a cut Diego retouches by hand (`GenesisUI-cuts/edited/`) is kept as
he coloured it.
**Why:** the sheets were drawn in different tones and too bright (R-042); Diego asked for one
darker, discreet gold without the art being redrawn, deformed or de-symmetrised.


## D-030 — Inventory gameplay patches

**Decision (2026-09-29):** the inventory module (docs/GAMEPLAY.md) keeps quick-use, utility and
equipment slots in extra rows of the player's inventory that GenesisUI draws in its own panels,
and applies these patches, each its own guarded class with contract tests:
`Player.SetInventorySize` (void prefix raising the rows to include the special rows),
`Inventory.FindEmptySlot` / `CanAddItem` / `GetEmptySlots` / `HaveEmptySlot` (postfixes on the
result: automatic placement ignores special rows), `Humanoid.EquipItem` / `UnequipItem`
(postfixes moving worn items into and out of their cells), and **one skipping prefix** on
`InventoryGui.OnSelectedItem` that refuses a drop into a special cell when the item is not
allowed there; in every other case it returns true and vanilla — and every other mod's patch on
that method — runs as before. Losing special-slot items when GenesisUI is removed is accepted
(Diego: part of the server modpack, not uninstalled).
**Why:** Diego's requirements (GAMEPLAY §1) need slots vanilla does not have; these are the
smallest set of hooks that let vanilla keep doing every item move itself. The earlier
"uninstall-safe rows" proposal was rejected as breaking the concept's design.
