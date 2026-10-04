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

## D-025 — Windows: rearrange and dress vanilla, keep its behaviour (replaced by D-032)

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

## D-031 — ServerSync for the admin's inventory settings

**Decision (2026-09-29):** `ServerSync` (the house standard for server-to-client config) is
vendored in `lib/ServerSync.dll` (SHA-256 `166956302a294e224474b26f4c7d58409084ad3f48bd0af1feb7551f229c8f60`,
the copy used by zzzGenesisItemStacks) and merged into GenesisUI.dll by ILRepack; `tools/package.sh`
refuses a DLL without it. It syncs only the `[Inventory]` entries marked [Servidor], with
`ModRequired = false` so clients without GenesisUI can still join. Its own Harmony `PatchAll` runs
only over its nested classes (read in its decompiled source), never over GenesisUI's patch classes.
The banned-API test allows the merged `ServerSync.` namespace its RPC, PatchAll and file calls.
**Why:** Diego's admin must set the inventory size and slot counts for everyone on a server; the
house standard for that is ServerSync (Jötunn does not ship it). The visual modules still register
no RPC of their own.

## D-032 — Windows: GenesisUI draws its own window; vanilla stays the engine (replaces D-025)

**Decision (2026-09-29):** the F4 windows are GenesisUI's own views, laid out on the concept's
design board (ConceptArt 9 measured: 1580 x 850 units, `WindowCanvas.Design`, scaled uniformly to
the screen). The inventory tab draws every cell itself from the player's inventory — grid with
scroll, quick-use and action slots, equipment, details, the chest panel — and no longer moves or
reskins vanilla's slots. Vanilla's `InventoryGui` still opens, updates and owns every item
operation, with its panels hidden (CanvasGroup, restored exactly): a press, right click or drag
release on our cell invokes the vanilla grid's own `m_onSelected` / `m_onRightClick` /
`m_onReleased` callbacks with the cell's inventory position, exactly as vanilla's slot would, and a
drag released over the world presses vanilla's drop button. Vanilla's split dialog stays vanilla's,
lifted above our window. Our code still never adds, removes, splits or transfers an item; only the
inventory module's position plans (D-028/D-030) and "Organizar" (positions only, Core
`InventorySort`) move items.
**Why:** Diego chose replacement over skins because GenesisUI's features do not fit vanilla's
window; D-025's "move and dress vanilla's slots" never reached the concept (giant, empty panels
around small vanilla cells, alignment drift, a vanilla flash on close). Handing input to vanilla's
callbacks keeps D-025's safety (item loss and duplication live only in vanilla code, and other
mods' patches on those handlers still run) without its layout limits.
**Cost:** gamepad navigation of the inventory is not wired to our cells yet (vanilla's gamepad
selection lives on its hidden grid); vanilla tooltips are replaced by the details panel.

## D-033 — Thin-line metal frames, two custom shaders, a darker theme

**Decision (2026-09-29):** GenesisUI's frames become thin lines with small ornaments (window
panels and bars, cards, slots and their states, key caps, rules), generated by
`tools/art/metal.py` from the silhouettes of Diego's concept, and drawn as polished metal:
- each frame ships a lit sprite (the look, without motion) and a linear relief map
  (`<name>_relief`, manifest `"color": "linear"`: normal, height, coverage);
- the `GenesisUI/Metal` UI shader lights the relief map at run time (bronze-to-gold ramp, light
  from the top left, specular, a slow glint sweeping the screen); `GenesisUI/Burn` draws a bar's
  loss as light (white-hot core, halo past the frame, cooling tail, embers);
- the shaders are our own source in `unity/`, compiled with Unity 6000.0.75f1 (Valheim's exact
  version, read from its `globalgamemanagers`) on the Windows build machine by
  `tools/shaders/build.sh` into `art/shaders/genesisui.shaders`, an AssetBundle (assets only, no
  code) loaded with `AssetBundle.LoadFromFile`, size-capped, from our art folder;
- without the bundle, on an unsupported GPU/API or with `[Theme] MetalShader = false`, frames keep
  their lit sprites and the bars their previous burn: the UI never depends on the shader.
The panel material (Diego's stone), the liquids, the creature plate and the minimal hotbar stay.
The whole theme is darker: stone at 55 % brightness, older gold, softer text, the world dimmed
behind an open window.
**Why:** Diego (after preview.9): the sheet frames were "grosseiro", he wanted "traços finos,
shaders metálicos, delicado e detalhista mas clean", the burn as real light, and everything
darker. He liked exactly the thin generated pieces (creature plate, minimal hotbar). A shader is
the only way to get moving light on metal; the lit fallback keeps rule 5 (every failure local).

## D-034 — Light effects and 3D previews in the UI

**Decision (2026-10-02):** two additions, both visual only and both optional.

1. **Light effects** (`[Theme] LightEffects`): our own UI shaders in the same bundle as D-033 —
   `Beam` (the selected tab's lantern light; a glint on the hovered tab's rail), `Reveal` (light
   running along a panel's frame once when its window opens), `Shine` (a recipe that has just become
   craftable), `Backdrop` (a warm vignette with faint grain behind open windows, replacing the flat
   veil; not gated, falls back to the veil), `Edge` (a light hugging a piece's border: drag target,
   craft progress, food and timed effects about to end), `Ring` (a new map pin), `Embers` (near the
   carry limit; at a forge). Each one is a quad disabled while dark; without the bundle or with the
   option off, the UI looks as before.
2. **3D previews** (`[Theme] Models3D`): the item turning in the inventory's details panel and the
   player's character between the equipment slots, each drawn by a `PreviewStage`: its own camera
   renders only a free layer into a render texture shown by a RawImage, far below the world
   (y = -10000), lit by its own lights restricted to that layer, rendering only while shown.
   What stands on it is a **picture only**:
   - the item is the visual part of its drop prefab (`ItemStand.GetAttachPrefab`, what vanilla's
     item stands show);
   - the character is a copy of `Game.m_playerPrefab` instantiated **under an inactive parent**, so
     no `Awake` runs; every component that is not a transform, renderer, mesh filter, LOD group,
     light, animator, particle system or `VisEquipment` is removed **before** it is activated (so
     `Character.s_characters` / `Player.s_players`, physics and the network never see it); its
     `ZNetView` is kept only through activation with vanilla's own `ZNetView.m_forceDisableInit`
     (the main menu's preview switch), and removes itself. Vanilla's `VisEquipment` dresses it from
     its own fields, copied from the local player's `VisEquipment` four times a second.
   Nothing networked is created, no ZDO is read or written, no game state changes (hard rule 1).
   The world's fog is switched off only while the stage's camera renders and restored right after.
**Why:** Diego (2026-10-02) asked for interface effects that are "delicados e elegantes" and for the
3D item and character. A copy that is never a character is the only way to show the real equipment
without touching the game; the inactive-parent instantiation is what makes it safe, and every part
fails locally (the icon and the empty panel stay).

## D-035 — The interface feels the world

**Decision (2026-10-02):** a module `hud.climate` (`[Modules] Climate`) reads the player's weather
and health and shows them on the interface, never changing anything:
- the metal shader (D-033) gets two **global** shader values, zero by default (no change):
  `_GenesisUIClimate` (frost from the frame's corners while Freezing, a little while Cold; drops
  running down while Wet; an ember glow while Burning; fine ash in the Ashlands) and
  `_GenesisUIDayShift` (the gold a few percent cooler at night, warmer at dawn and dusk). Globals,
  not material properties, so the copies the UI makes of the material for stencil masks follow;
  reset to zero on teardown;
- `GenesisUI/Heartbeat`: below 25 % health, an ember vignette (deep red to orange) at the screen's
  edges swells with each heartbeat (a strong beat and a softer one), 62 to 130 beats a minute as
  health falls; behind the HUD;
- while Rested, a faint gold halo (`GenesisUI/Edge`) around the vital bars.
The art direction stays gold only (D-023): the weather is a skin over the same gold.
**Why:** Diego (2026-10-02) asked for these after the light effects (D-034); they make the
interface feel alive without adding any new piece to the screen.

## D-036 — Depth, runes and sound

**Decision (2026-10-02):**
- **Parallax** (`[Windows] Parallax`): the window canvases have no perspective, so a real tilt would
  only squash the picture; depth comes from layers instead. While a window is open the boards
  drift against the pointer (`WindowCanvas.Parallax`, up to 5 design units at depth 1: panels 1,
  the shell's bars 0.4, the map's chrome 0 since vanilla's map under it does not move), and the metal
  shader's light leans towards the pointer through a global `_GenesisUILightShift` (zero by default).
- **Runic titles**: each panel title is written in Elder Futhark first and turns into its letters in
  under half a second when its window opens (`RuneTitle`). The runes come from **Noto Sans Runic**
  (SIL OFL 1.1, shipped with its license in `art/fonts/`), loaded only as a fallback font after
  Cinzel and Cormorant; without it the titles appear as before.
- **Sound** (section `[Sound]`, the Som category of the settings window: all on/off, volume, each
  sound on its own, applied at once): GenesisUI's own interface sounds —
  hover on a tab, a tab chosen, a window opening, an item made — **synthesized at start-up** from
  sine partials and a little noise (`UiSound`): no audio file, nothing from the game or another mod.
  Played through the game's GUI mixer group (`AudioMan.m_guiMixer`), so the game's volume applies.
  The build references the game's `UnityEngine.AudioModule` and its `netstandard` facade (ref/,
  `tools/fill-ref.sh`).
**Why:** Diego (2026-10-02) asked for depth, runes and sound after D-034/D-035; the interface had no
sound at all.

## D-037 — Slot hotkeys that take their key, and the HUD beside the vital bars

**Decision (2026-10-02):**
- **Hotkeys** for the 4 quick-use and 4 action (utility) slots, section `[Hotkeys]` (the player's own,
  never synced; Settings → Inventory → Slot hotkeys, which now captures combinations: Alt, Ctrl and
  Shift held are taken with the next key). Any key with any modifiers; defaults Alt+1…Alt+4 and
  Alt+Z…Alt+V (Z/X/C/V alone are free for vanilla). A hotkey uses its item through
  `Humanoid.UseItem(null, item, false)` — exactly what vanilla's hotbar keys do — under the
  conditions of vanilla's `Player.TakeInput`.
- **The hotkey takes its key from vanilla** while held with exactly its modifiers: postfixes on
  `ZInput.GetButtonDown` / `GetButton` / `GetButtonUp(string)` turn the result false for the
  buttons bound to that key (found once through `ZInput.m_buttons` and each button's Input System
  controls whenever the set of held hotkeys changes). Alt+X uses an action slot and does not sit;
  X alone still sits. Postfixes on the result only (PATCH-POLICY rule 1): vanilla and every other
  patch run as before; with the slots module off or no hotkey held the patch changes nothing, and
  its common path is two comparisons without allocation. A modifier's own vanilla action (Ctrl is
  crouch) is not taken: that choice is the player's.
- **HUD** (Diego, 2026-10-02): the three food slots become the quick-use row (as many as the server
  allows), with the smaller action row above it, each cell with its hotkey (`hud.slots`); foods and
  potion/mead effects become two vertical columns beside the vital bars (`hud.food`), potion effects
  being those some consumable applies (`m_consumeStatusEffect`, modded potions included; they leave
  the status tiles). Everything lines up after the bars' right edge (`HudAnchor`, published by the
  vitals module; it moves as the eitr bar fades in or out) and glides and fades there (`Glide`).
**Why:** the Z/X/C/V on the action slots were drawn labels with no code behind them (GAMEPLAY §1.4
planned hotkeys and a HUD row; neither was built), and Diego wants combinations that never trigger
the vanilla action on the same key.

## D-038 — Vanilla never shows through; the 3D stage renders itself

**Decision (2026-10-02, after Diego's report: a flash of vanilla on the first inventory opening,
fragments of vanilla while moving through the windows, and the 3D previews stopping without any
error):**
- **Hidden means pinned.** `VanillaSkin.Hidden(go)` hides a vanilla object (its group at alpha 0, no
  pointer) and adds a small component of ours that puts those values back at the end of every frame,
  after vanilla's animators (some Valheim panels and dialogs fade their own group in, which undid the
  hiding). Removed on restore. Used by every module that hides vanilla UI: inventory panels, Esc
  menu, build menu and placement card, small dialogs, trader, map chrome.
- **The inventory panels stay hidden for the session** while GenesisUI's inventory and crafting
  windows both run (a hold of the window shell, `win.shell:always`), instead of being given back and
  taken again on every opening, tab switch and closing. Either window turned off or faulted gives
  them back (every failure local).
- **The 3D stage renders itself.** Its camera is never left enabled; the stage calls `Camera.Render()`
  once per frame while shown, switching its lights on and the fog off around that one call (restored
  in a `finally`). A watchdog checks the scene, camera, culling mask and render texture every frame,
  repairs what it finds (rebuilding the scene if it was destroyed; the previews then rebuild their
  model) and logs each kind of problem once, so a report names the cause. The character preview tries
  again after a failure and only gives up, with a warning, after three in a row.
**Why:** stability first: hiding that an animation can undo, panels exposed at every hand-over, and
a camera whose rendering nobody watched were each a class of bug, not one bug.

## D-039 — The 3D stage renders in the path its model needs

**Decision (2026-10-02):** the brightness fix (D-034) set the stage camera to Forward so a light's
culling mask would hold; the item kept showing but the character vanished, without any error:
Valheim's character and armour shaders have a deferred pass only, and a forward camera draws
nothing of them. The stage now looks at the model's materials (`UsePathFor`): a material with a
DEFERRED pass and no FORWARD one switches the stage to the game's deferred path. Deferred cannot
keep a transparent background in the texture, so the stage clears to a key colour (magenta) and
the picture uses `GenesisUI/Keyed`, a UI shader that turns the key back into transparency and takes
it out of the edge pixels. Antialiasing is off in that path (a deferred camera cannot render into a
multisampled texture). The lights stay switched on only around the stage's own render (D-038), so
the world is never lit by them in either path. The choice and the shaders that made it are logged.
**Why:** both kinds of model must show, and the choice must follow the model, not an assumption.
**Amended (same day):** the character still did not show and some items (the hammer) did not either,
with every material reporting a forward pass. The stage sat 10 km below the sea; Valheim's own shaders
change what lies under the water level. It now stands at an ordinary height just above the sea
(y = 40), beyond the world's edge (x = z = 15 000), still seen by no world camera (its layer). A few
renders after a model is put on it, the stage logs once what it really draws (renderers, enabled,
drawn by its camera, bounds, camera, path, shaders with their passes), so the next report settles it.
**Amended after the 13:27 report:** the character (6/6 enabled/visible) and hammer (1/1)
still appear empty on Diego's client. `Renderer.isVisible` proves culling accepted a renderer,
not that the UI received usable pixels. The stage now clears to magenta and uses the existing
`GenesisUI/Keyed` material for both forward and deferred rendering, so model colour is
composited independently of the alpha its game shader writes. One 48×48 readback per inspected
model logs how many pixels differ from the clear colour and how many of those have near-zero
alpha. This is a candidate fix; R-062 must confirm the visual result on the client.
**Amended after R-062:** Diego confirms the character and items are visible in preview.1,
but reports a pink outline. MSAA and bilinear filtering mixed the clear colour into the
silhouette before key removal; the distance-derived alpha is not the geometric coverage
of that mixed sample. The keyed source now uses single-sample rendering and point filtering
at a bounded 2x resolution (longest side at most 1024). The UI shader decodes four texel
centres first, interpolates premultiplied colour and coverage, then converts to straight
colour for the UI blend. Shader compilation and a GPU edge regression check precede
packaging; R-063 verifies the real model silhouettes on Diego's client.

## D-040 — Stability boundaries and safe inventory transitions

**Decision (2026-10-02, authorized by Diego after the complete review):** fault recovery is
queued until every cleanup subscriber has returned. Failed/partial construction receives the
same owner cleanup as normal teardown. Own objects, subscriptions and shared CanvasGroup claims
have explicit lifetimes; the original group snapshot is restored after its last borrower leaves.

Inventory relocation requires the entire size/placement/equipment/transition patch capability.
The transition snapshots item references/counts/positions, grows capacity before moves, validates
the complete plan and resulting bounds, and restores remaining original positions on failure.
Only this isolated transaction may restore `Inventory.m_height` to undo its own resize, without
rerunning foreign resize callbacks. It never adds/removes/replaces items or overrides foreign
membership/count changes. A stale snapshot refuses the operation and faults its owner.

**Approved skipping prefix:** `Humanoid.DropInvalidItems` skips the original only for the local
player while our resize transaction is on the stack. Vanilla `Player.SetInventorySize` invokes
destructive cleanup before the caller can validate the result; this prevents item drops when a
foreign height hook reduces the requested capacity. Outside that synchronous transaction it is
a no-op. Other postfixes still run; side-effecting prefixes after a skipped original may be skipped
by Harmony's normal semantics. This narrow scope is required to validate/roll back safely and is
covered by a declared target contract. No foreign patch is removed.

Minimal inventory configuration sync initializes on headless servers under D-031; all GenesisUI
visual modules and gameplay patches remain client-only. The vendored ServerSync library retains
its existing, separately allowed config-sync patches. Headless initialization is not tested on
production and does not authorize server installation.

**Implementation amendment:** inventory rollback journals only our writes and preserves later
foreign position changes as well as foreign membership/count changes. Diagnostics registration
cleanup removes only entries still referencing our own Jotunn button/native definition; no
foreign entry or patch is removed. Report retention may delete only checked, owned report names
in our report folder (10 retained). Shader provenance is size-bounded typed StrictJson data;
bundle/source hashes and the keyed GPU check gate packaging. Missing runtime evidence uses the
existing sprite/2D fallback. These changes add no runtime dependency or public provider API.

## D-041 — Stable visual snapshots and a single lore presentation

**Context (2026-10-02):** Diego's partial vanilla R-065 run found pinned cape geometry,
character shrinkage after equipment changes, obscured map content, an Esc opening flash and
overlapping lore/raven readers. Item models/details worked. The Desktop screenshot is recorded
in R-065. Native behavior was read from production `Menu`, `TextViewer`, `RuneStone` and
`VisEquipment` types individually; no decompiled code is included here.

**Decision:** the character preview contains owned baked meshes of the current visible pose,
native read-only materials/property blocks and the highest LOD. It never copies a gameplay
object, bones, an animator or MagicaCloth's initialized solver. The cape is a static pose snapshot;
the complete picture keeps its gentle sway. Bake only on the existing equipment/model invalidation,
not every frame, and release generated meshes on rebuild/teardown. Unity's documented
[BakeMesh](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html)
returns renderer-relative vertices; transform/scale is applied once in the visual copy.

Frame against the actual target aspect before first render and rebuild; character framing uses
a fixed upright box rather than equipment bounds. Resize refits that same box. The map chrome
has no panel material over the native map. Edge's border distance field uses UV-derived design
units so Canvas batching cannot change its coordinate space; Produce has only a narrow breathing
border, with a thin bottom progress rule when the shader is unavailable.

Drawing uses actual menu root activity, not native input hysteresis. Module LateUpdate runs at
30000, preview rendering at 31000 and shared veil enforcement at 32000. This places source reads
after ordinary native LateUpdates and final preview composition after module changes. These are
Unity execution-order attributes, not Harmony patches; foreign patches remain untouched.

Lore readers use TextViewer's requested animator flags. Its rune and raven roots remain active
even when their readers are closed, so root activity cannot identify the style. Veil native roots
and text graphics reversibly for the module's lifetime, including native fade-out; intro remains
native. Show one unframed localized lore text, preserve rich-text tags/line breaks and reveal runes
over 2.5–8 seconds after a 0.35-second pause. Reuse a bounded character buffer (16,384 characters).
The hover card suppresses itself during reading. Keep native dismissal (E/Esc/movement), discovery,
known texts and guardian interactions unchanged. Raven guidance retains its existing single card.
`UnityEngine.AnimationModule` is a compile-time reference to Unity already shipped with Valheim;
no additional runtime DLL or mod dependency is packaged.

**Gate:** all channels' Core/L2 tests; source-bound keyed and narrow-border GPU checks; package
hash verification; focused client R-066. Shader probes are not Valheim visual approval.

**Amendment (2026-10-02, R-066 rejected / R-067):** replace the baked-mesh portion above with
a visual-only skinned rig. The local `report-20261002-193046-957-d86de72177674bfb8c477304e2db4ab6.log`
(preview.2, `fa5571b`, line 412) reports a 294.12 m high picture against the fixed 2 m frame,
with all six renderers visible and supported shaders. An exact Unity 6000.0.75f1 GPU fixture
reproduces the erroneous scale conversion: source skin and remapped copy both occupy 176 pixels,
while the old flattened bake occupies 2/176/232 pixels for renderer scales 0.01/1/100. The
100-scale flattened bounds are 206.404 m high despite the two-metre native silhouette.
Source runtime import scales were not logged in preview.2; no exact 100x value is claimed for
Diego's character. The conversion mechanism is demonstrated independently in nine scale pairs.

Create transforms only, copy local relationships, privately copy meshes and preserve skinning
bind poses. Remap every bone/root bone; reject outside references. Copy native materials,
property blocks and blend weights. Never instantiate gameplay/animator/cloth components.
Rebuild on the existing visual fingerprint and dispose partial/full rigs and owned meshes.
The cape remains a static pose; native physics never runs in the stage. The same frame/camera
persists after equipment changes. `tools/preview/verify-rig.ps1` compiles the exact runtime
helper against synthetic meshes and checks GPU silhouette, source isolation and cleanup.

## D-042 — Mirror native equipment action progress in the inventory

**Context (2026-10-02):** Diego noticed the yellow native progress bar behind the inventory,
and requested a rotating gold border until equipment completion, then the equipment-slot move.
Metadata and individually decompiled production Player/Hud show that the HUD bar is outside
InventoryGui's veiled panels. Player.GetActionProgress exposes the first queued action;
native completion removes that action and calls EquipItem/UnequipItem.

**Decision:** read that public progress/action once per inventory refresh. Draw a narrow orbiting
segment on its visible item cell, advancing twice around the border with native progress.
Paused/cancelled/completed actions follow native state. If the cell is offscreen or effects
are disabled, show native localized action text and percentage in the inventory's lower gap.
Hide Hud.m_actionBarRoot with an owner-scoped shared lease only during this replacement;
restore exactly on cancel/end, close/tab change, replaced HUD, fault or teardown. Reload remains
native. On completion/cancel retain the veil until Hud hides its potentially stale bar from
earlier in that frame; the glow stops immediately. Log transitions and reserve hud.action for
diagnostics/foreign ownership.

Reuse the existing successful EquipItem postfix and validated position journal (D-030/D-040)
for the slot move. Do not move early, write action data, alter timing or change item identity/count.
No new Harmony patch or runtime dependency; foreign patches continue to execute unchanged.
Extend Edge's GPU/source provenance gate with four orbit phases and clear-interior checks.
Focused client gate: R-067, including cancel, queue order, close and effects-off restoration.

## D-043 — Exact-version inventory integrations through native UI commands

**Date:** 2026-10-04. Diego requested Jewelcrafting's Socket workflow, accessory equipment
cells, backpack contents below the inventory, and Use taking precedence over next-tab.

**Decision:** deliver internal disposable equipment/container capabilities as guarded host
modules, bound only to the assembly of a loaded plugin with the declared GUID and exact
version. This is an initial F7 slice; the public extension API remains planned. Verify every
foreign contract against the owning DLL. No foreign code, assets, DLLs or API wrappers ship.
No patches target foreign methods and no hard/runtime assembly dependency is added.
Soft BepInDependency metadata orders already-installed owning plugins before GenesisUI,
so the initial prerequisite check sees their initialized config/API; absent mods stay optional.

Project the live native container grid even when InventoryGui.m_currentContainer is null.
Backpacks and Adventure Backpacks select the below-inventory pane; unknown containers use
the existing right pane. All cells retain native grid callbacks; close/take/stack invoke
native UI commands and retain their visibility restrictions. The complete board scales
uniformly when the backpack pane opens. Equipment positions are planned under D-028/D-030;
mod predicates classify gear, while successful native equip completion decides relocation.

Jewelcrafting's Socket tab projects its actual RecipeDataPair list and CanCraft values.
Selecting a row invokes the native row Button without changing to the ordinary upgrade tab.
Adding sockets and opening gem slots invoke the original buttons. Native warning text remains
visible even with an enabled craft button; no costs, success chances or unsocket rules are
reimplemented. The Use guard forwards a supported hovered item's owning-mod command first,
then suppresses the shell tab change for that frame; vanilla Update and all patches still run.
EquipItem's postfix follows the supported owning mods. Existing special-cell skipping guards
also reject invalid pocket swaps; D-030's valid-input native execution remains unchanged.

Style the live UITooltip reversibly, preserving foreign children, dimensions, text and timing.
Use a dark flat background, the theme font and four one-pixel gold lines. Replace the rested
rectangular vitals EdgeLight with an alpha-preserving tint over each existing shaped frame.
Visual approval requires R-069; this is not full-modpack certification.

## D-044 — Native coin and key pockets, with safe coin reload

**Date:** 2026-10-04. Diego clarified these are native items: one wallet and two key cells
above the model; coins should share a practically unlimited stack.

**Decision:** append Wallet/KeyOne/KeyTwo to the existing saved equipment enum, preserving
old bit indices. Pockets accept exact Coins, CryptKey and DvergrKey identities; they do not
equip items. Only position plans move existing items, with identity/count snapshots and
rollback. New pickups and manual stack merges remain native. Existing multiple coin stacks
can be consolidated with native drag/drop; no automatic count manipulation is introduced.

Pockets and WalletCapacity are server-synced inventory settings. Default coin capacity is
Int32.MaxValue (2,147,483,647), the native saved count's technical ceiling, not mathematical
infinity. Weight, purchasing, key use, ownership and RPCs remain native. Metadata capacity
is owned reversibly by Gameplay, not a visual module; if another mod changes it, do not fight.
Abbreviate large visible counts; hover/native details retain item data.

Individual native Inventory decompilation proves CanAddItem's Int32 addition/multiplication
can overflow with this capacity, and the private serialized AddItem path clamps the saved
stack with Mathf.Min(stack, shared.m_maxStackSize). Correct the proven capacity overflow in
CanAddItem's first-priority postfix, before other mods' normal-priority restrictions. Only
correct a result matching the raw native arithmetic and only for the owned Coins SharedData;
never revive a skipped original or a differing foreign result. Other prefixes/postfixes run.

Add void prefixes/finalizers to both native Inventory.Load(ZPackage) overloads. Temporarily
raise only the Coins prefab's metadata limit to Int32.MaxValue while native deserialization
runs; restore the prior limit in the finalizer only if still owned. No package bytes or item
counts are edited; originals and other patches run and native exceptions propagate unchanged.
This persistence safeguard is deliberately independent of the wallet drawing/config toggle:
turning off a UI feature must not truncate a previously saved coin stack. It is the explicit
exception to visual feature no-op policy, with its own guarded-patch diagnostics/fault owner.
Headless servers still receive no gameplay patches. The wallet feature requires this patch
capability; failure to bind prevents activating expanded stacks.

Removing GenesisUI (and every mod that preserves the enlarged limit) can expose vanilla's
reload clamp. Reduce enlarged coin stacks before uninstalling; this proven persistence
limitation is included in the client script and gameplay documentation. Single-client
save/reload, toggle, transfer, capacity and modpack validation remain mandatory in R-069.

## D-045 — Independent visual scales and station-preserving gem editing

**Date:** 2026-10-04. Diego's Preview.1 client feedback: backpack drops are refused, the
expanded backpack layout is too small, and gem editing forces leaving the crafting view.

**Decision:** bind ItemDataManager containers with the owning method's actual default
string parameter, checked against both exact foreign DLLs; null addresses a different entry
and is not equivalent to omitting the parameter. Preserve native equip/transfer execution.

Add `[General] Scale` (0.5–1.5, default 1) for owned HUD/windows and `[Windows] Scale`
(0.5–2, default 1) for owned design boards. Compose with the existing local scales/viewport
fractions, use uniform transforms and keep boards within 98% of the screen for parallax.
Keep HUD edge anchors by dividing its logical canvas size by the general multiplier.
Native UI geometry/scale stays game-owned; dressed native tooltips/dialogs retain game scale.
Both settings are client-only, localized in our settings screen and included in F8/logs.

Keep station gem editing on the Crafting tab. Invoke the original Socket gem button, then
project the live native container and player grids into an owned panel at Item Details.
Hide the entire Details panel, preserving its content, to prevent transparency bleed.
Pause recipe clicks until native CloseContainer restores Details on the same station/item.
Close the editor on tab exit/teardown; guarded cleanup releases hover and drag-icon leases
even if a native close call fails. Do not close a different container that replaced ours.

Wrap native cell addresses in a bounded pure layout; every pointer, tooltip, touch and
drag/drop action forwards to native elements/callbacks. No item acceptance, socket cost,
unsocket chance, save rule, inventory count or identity is implemented by the view. The
existing GetHoveredElement postfix accepts this additional owner-guarded projection;
original/foreign patches continue running. Native split dialogs render above our view.
No new Harmony target, foreign patch, dependency, public API or art asset is introduced.
Automated contracts/math tests are distinct from the focused R-070 client/visual gate.
