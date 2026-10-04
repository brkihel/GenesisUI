# GenesisUI — Instructions for agents

You are working on GenesisUI, a client-side, visual UI mod for Valheim whose first
two priorities are **security** and **stability**. Read this file completely before
any change. When a rule here conflicts with your habits, this file wins. When a rule
is unclear, ask; do not guess.

## 0. Where the project is (keep this section current)

- **Phase:** HUD, inventory/crafting windows, in-game menus and map are shipped in 1.1.2.
  F4 and F6 are delivered; F5's main menu and native settings screen remain planned.
  F7 inventory adapters begin in the 1.2.0 preview; public providers/resource adapters
  remain planned. Original window plan: [docs/F4-PLAN.md](docs/F4-PLAN.md).
  **Approach since
  2026-09-29: D-032** — GenesisUI draws its own windows on the concept's design board
  (`WindowCanvas.Design`, ConceptArt 9 measured in 1580 x 850 units); vanilla's `InventoryGui`
  stays open, hidden, as the engine, and our cells hand every input to the vanilla grid's own
  callbacks. D-025 ("move and dress vanilla's slots") is replaced; Diego wants fidelity to the
  concept's assembly, not new designs. Original phase sequence is D-024; actual delivery status
  and remaining work are in [docs/ROADMAP.md](docs/ROADMAP.md).
- **Version:** **1.1.2 Release** (2026-10-02; approved after preview.3). Tag `v1.1.2` identifies
  binary source `e378afa`; [release record](docs/releases/1.1.2.md) includes the verified ZIP/hash.
  GitHub Release and main synchronization are authorized; Hexium upload remains Diego's step.
  Release has no overlay/watermark; F8 writes a report. Previous public release: 1.0.1.
  Store page: `store/README.md`; GitHub docs: `README.md`/`README.pt-BR.md`; images:
  `.github/assets/`; support: https://discord.gg/TZ785sYtgx. Shader sources: `unity/`, built
  locally with Unity 6000.0.75f1 by `tools/shaders/build.ps1 -DirectEditor`; proof:
  `art/shaders/provenance.json`. Character/items and the equip border are approved in R-067.
- **Art direction:** gold only (D-023). The carved-wood style was tried and rejected; do not
  propose another style unless Diego asks.
- **Stability review (2026-10-02):** approved preview committed as `2a1854a`.
  [Review](docs/review/2026-10-02/README.md), [FullPlaythrough study](docs/review/2026-10-02/MODPACK.md)
  and [implementation sequence](docs/review/2026-10-02/IMPLEMENTATION.md) record 25 findings
  and 71 package/74 DLL metadata inventory. The 25 base findings are implemented in 1.1.2;
  the immutable review records the earlier source, not current runtime certification.
- **Development test (2026-10-02):** R-062 on `1.1.1-preview.1` confirms the character
  and items appear (`report-20261002-134941.log`), with an unwanted pink outline.
  `1.1.1-preview.2` decodes the key before edge filtering (D-039, R-063); all three
  channels pass automated tests. Diego approved its visual result in R-063 (`2a1854a`).
  He authorized the complete stability review fixes. Release branch: `fix/stability-1.1.2`; current development branch: `feat/modpack-inventory-1.2.0`,
  Release `1.1.2` approved by Diego after preview.3, D-040/D-041 amendment/D-042 and `docs/STABILITY-FIXES.md`.
  R-066: Diego found almost everything satisfactory but rejected the baked character; its local
  report shows 294.12 m bounds against a 2 m frame. Replace it with new transforms/private meshes
  and remapped skin bones; preserve local import-scale/bind-pose relationships, no animator,
  gameplay or cloth components. Exact-helper Unity GPU fixture passed nine scale combinations
  and reproduced the old BakeMesh conversion defect. Native equip progress is mirrored as an
  orbiting border, with a conditional HUD-bar veil and text fallback. R-067 records Diego's
  overall Release approval; individual step results/new F8 were not supplied. Fresh source-bound
  key/edge/orbit GPU checks passed in Unity 6000.0.75f1/Direct3D11
  (key 256/512/256, 3 borders, 4 orbit phases); provenance: `art/shaders/provenance.json`.
  Debug/Preview/Release passed 191 Core + 11 L2 each, zero skips; see R-067 results.
  Commit first and verify ZIP contents by hash.
  Structured providers/mod resource adapters remain F7 work; no full-modpack certification
  or production rollout is implied. Earlier uncommitted packaging was a one-session exception.
- **Current F4.0 art source:** Diego supplied isolated UI sheets and separate background,
  health, stamina and eitr textures at
  `/home/diego/GenesisUI-Concept/GenesisUI-textures/`. `tools/art/sheets.py` cuts every HUD
  piece from them into `art/src/sheets/` (D-027); `tools/art/sheets_preview.py` draws the
  contact sheet for the §2b quality gate. The HUD uses them (frames, `_shape`
  backgrounds with `[Backgrounds]` opacity, `_opening` masks, liquids). Integrated and
  approved in game (R-042). Retouched cuts: `GenesisUI-cuts/edited/<name>.png` (same size as
  `original/`) replace the sheet cut; rerun `sheets.py` then `render.py`. HUD/windows are shipped;
  future art still follows this pipeline and the review gate.
- **HUD modules (13, Plugin.cs registration):** `hud.vitals`, `hud.food`, `hud.hotbar`,
  `hud.minimap`, `hud.boss`, `hud.enemy`, `hud.hover`, `hud.notice`, `hud.status`, `hud.keyhints`,
  `hud.slots`, `hud.climate`, `hud.sprint`. Each has a `[Modules]` toggle. The vanilla
  key-hint nudge remains owned by `hud.hotbar`; the quick-use/action HUD rows are already shipped.
- **Open items from Diego:** boss plate spacing; user positioning of HUD pieces; remaining
  main-menu/native-settings work; exact-version modpack resource adapters and client benchmarks.

- **Development (2026-10-04):** branch `feat/modpack-inventory-1.2.0`, Preview
  `1.2.0-preview.2` keeps the first F7 inventory integrations (D-043/D-044) and adds
  general/window scales, correct default-key backpack classification and station-preserving
  gem editing (D-045). R-069 records Diego's Preview.1 feedback/proven causes; R-070 is the
  pending focused single-client script on Gale `GenesisHeimLocal`.
  Exact DLL contracts: Backpacks 1.3.10, Jewelcrafting 2.0.10, HipLantern 1.1.12;
  optional Adventure Backpacks 2.0.3 is absent from that profile and uses the official
  reference download. Public API/resource adapters and full-modpack certification remain
  planned. Release/main stay at approved 1.1.2 until review of this development phase.

## 1. Read first, in this order

1. [docs/VISION.md](docs/VISION.md) — what GenesisUI is and is not.
2. [docs/DECISIONS.md](docs/DECISIONS.md) — decisions already made. Do not re-open
   them; propose a new entry if you believe one is wrong.
3. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — layers, modules, regions, veil,
   scheduler, input, layout, theme, security model.
4. [docs/regions.md](docs/regions.md) — every vanilla region, its owner module, its veil,
   and what vanilla does to it (answered by test reports).
5. [docs/PATCH-POLICY.md](docs/PATCH-POLICY.md) — before touching Harmony.
6. [CHANGELOG.md](CHANGELOG.md), [docs/ROADMAP.md](docs/ROADMAP.md) and the latest file in
   [docs/testing/results/](docs/testing/results/) — where the project is right now and what
   Diego asked for last.
7. The document for your task: [ART-DIRECTION](docs/ART-DIRECTION.md),
   [EXTENSION-API](docs/EXTENSION-API.md), [ADAPTERS](docs/ADAPTERS.md),
   [DIAGNOSTICS](docs/DIAGNOSTICS.md), [TESTING](docs/TESTING.md),
   [RELEASE](docs/RELEASE.md), [ROADMAP](docs/ROADMAP.md).

The GenesisMods house standard also applies (pillars: security > didactics > Valheim
look): `heimdall-nexus/docs/PADROES-GENESISMODS.md`.

## 2. Hard rules (a change that breaks one of these is rejected)

1. **Visual modules never change game state directly.** No `ClaimOwnership`, no
   `Inventory.AddItem/RemoveItem`, no `ZDO.Set*`, no new RPC. Clicks call the same vanilla
   entry points the vanilla UI calls. The isolated F4 inventory module may plan and change
   saved slot positions under D-028/D-030; item count and identity must stay constant.
   The banned-API test enforces the other limits.
2. **Never `Harmony.Unpatch` anything that is not ours. Never `PatchAll()` the
   assembly.** Patch classes are applied one by one through the Foundation
   guarded patcher.
3. **Postfix first.** Skipping prefixes and transpilers need a decision entry.
4. **Hide vanilla, never destroy or deactivate it.** Every veil is restored exactly.
5. **Every failure is local.** Module, adapter and extension calls go through the
   guard; a fault returns that region to vanilla and is logged once with context.
6. **Every game or foreign member you touch is a declared contract** with a contract
   test.
7. **Core stays pure**: no Unity, Valheim, Harmony or BepInEx in `GenesisUI.Core`.
   **Foundation stays self-contained**: code under `Foundation/` never references
   modules, views, theme or API types (it will be extracted into GenesisModLIB).
8. **Clean room**: never copy code, art, icons, texts or layouts from SeneaL UI or any
   closed mod; never ship game assets; never commit the full concept images. Cleaned UI
   pieces from Diego's own concepts are allowed by D-026. Studying other mods' behaviour
   is allowed; reusing their implementation is not.
9. **No new runtime dependency** without a decision entry. Allowed today: BepInEx,
   Jötunn (latest).
10. **Data files are data**: JSON via `GenesisUI.Data.StrictJson` (D-018) mapped to typed
    classes, size-bounded, validated and clamped in Core, with a test that reads the
    shipped file. Nothing in a data file selects a type, a method or
    a path outside our folders.
11. **Diagnostics are part of the feature.** Anything new must appear in the
    diagnostics overlay (the host gives modules this for free) and log its failures.
    Debug and Preview builds always ship the full diagnostics layer.
12. **Art is reproducible** (D-020, amended by D-026). Generated UI shapes come from
    `tools/art/shapes.py` using `tools/art/style.py` (the gold language, the only art
    direction: D-023); animated textures come from `tools/art/patterns.py`. Concept cutouts
    come from `tools/art/extract.py`; refinements for review from `tools/art/refine_concept.py`.
    The newly isolated source sheets in §2b take priority over rejected crops; update the
    pipeline to produce reproducible individual sprites from them. Never edit a generated SVG
    in `art/src/`. New art is ours
    (`art/LICENSES.md`); fonts are OFL only.

## 2a. Lessons already paid for (do not relearn them)

- **Never resolve reflection in a static initializer or constructor** of a module. Modules are
  constructed in `Awake`; a missing member there takes the whole plugin down before the host
  checks contracts. Resolve `AccessTools` members in `Build` (see `HotbarModule`).
- **Unity null, not C# null.** Use `if (obj == null)` / helpers like `RegionRegistry.FromHud`;
  never `?.` or `??` on Unity objects: a destroyed object is not C#-null.
- **Vanilla may not be ready when you build.** Modules can be built before a vanilla
  component's `Start` (the minimap was, and vanilla then swapped its material: grey map in
  R-030). Read live vanilla objects every refresh, never cache what vanilla may replace.
- **Zero allocation per frame.** No capturing lambdas in per-frame paths (cache delegates,
  use `Guard.Run(owner, action, arg)`), `TMP.SetText` with numbers instead of string
  concatenation, update Unity objects only when the value changed.
- **`Guard.Run` trips an owner on the first exception; `Guard.Try` never trips.** Use `Try`
  for clean-up paths that must always run (teardown, restore, config toggles).
- **Do not trust Unity's JsonUtility** with our types (it silently read an empty list, R-010).
  Data goes through `StrictJson` and a test that reads the shipped file.
- **Texts shrink to fit** (`Ui.Fit`) inside frames; never let a number cross a border.
- **Move vanilla with `VanillaNudge`, never re-parent it**, when it collides with our layout
  (key hints); the original position is restored on teardown or fault.
- **Mirror vanilla instead of re-implementing its logic** when vanilla already decides what
  to show (minimap pins D-019, hover text, message timing, creature plates): it stays correct
  with other mods' content.
- **Never trust an elapsed time blindly.** A fresh build asked for a refresh with
  `float.MaxValue`; the bars' animation clock became NaN and the liquid striped after a fault
  retry (R-040). The host now caps the delta at 0.25 s; keep animation clocks bounded too.
- **Match vanilla clones by exact name** (`template.name + "(Clone)"`). A prefix test on
  `HudBase` would also catch `HudBaseBoss(Clone)`.
- **To follow a vanilla object exactly, live inside it.** Creature plates are children of the
  vanilla plate with their own CanvasGroup (`ignoreParentGroups`): vanilla's positioning,
  show/hide and destruction carry them with no lag and nothing to clean up.
- **Think about what covers what.** Our HUD root sits under vanilla's HUD root; the
  interaction card drew over the open large map (R-040). Check new views against vanilla's
  windows and map.
- **Do not invent causes.** Prove the cause in code or logs before a fix (a result file names
  it). If a log is needed, ask Diego to paste it.

## 2b. Texture integration contract (F4.0, 2026-09-28)

- **Source inventory:** `UI_elements1-upscaled.png` through `UI_elements5-upscaled.png`
  contain isolated gold UI elements; `background_texture.png` is the common dark panel
  material; `texture_example-upscaled.png` illustrates the intended assembled look;
  `hp_texture.png`, `stamina_texture.png` and `eitr_texture.png` are the three coloured
  liquid materials. All are in `/home/diego/GenesisUI-Concept/GenesisUI-textures/`, outside
  this repository. The five element sheets have alpha; the background and three liquid
  images are RGB without alpha. Cut and name **individual UI sprites** before integration.
  Keep full source sheets outside the repository; commit only reviewed, cleaned pieces and
  the recipe/metadata needed to reproduce them (D-026).
- **Visual fidelity:** preserve the source metal's thickness, highlights and ornaments.
  Previous attempts that redrew long rails as thin, flat strokes or changed the central
  stamina bar were rejected. Treat the earlier `dist/art-review/` page and
  `art/src/concept/` crops as historical drafts, not approved replacements. Diego approved
  the **vertical vital-bar shapes** in that review; preserve their distinct sizes and
  proportions. The new sheets and their use in the game still need visual review.
- **No texture deformation:** isolate fixed corners, knots, caps and other ornaments.
  Render fixed-size pieces at their intended proportions, allowing only a uniform scale of
  the whole UI group. For a variable-length straight section, repeat a clean, seamless
  strip or use separate fixed caps and a centre; a Unity 9-slice preserves corners but
  stretches its edge and centre pixels. Do not stretch decorated rails, change a vertical
  bar's width independently of its height, or scale an eight-slot hotbar non-uniformly.
  The eight-slot sheet may be rendered at a fixed aspect ratio with vanilla item icons,
  labels, durability and selection as aligned independent children; use separately cut
  slot/rail pieces if the layout must become variable later.
- **Independent background opacity:** `background_texture.png` belongs behind panel
  frames, not baked into them. Clip/crop it to each panel's inner silhouette so transparent
  corners stay transparent. Give the background graphic its **own** alpha/config value per
  panel (or a shared default with per-panel override); do not put an opacity `CanvasGroup`
  on a parent of the gold frame, text, icons or item controls, because that would fade them
  too. A separate parent fade for the entire UI element remains valid for show/hide.
  The background is not edge-seamless; do not repeat the original PNG unmodified. Crop a
  fixed-area sample without stretching, or prepare a seamless derivative before tiling.
- **Valheim content:** layer the game's own icons, texts, values, maps and interactions
  above the background and within the frame's declared content insets. HUD modules may
  continue reading or mirroring vanilla state while veiling the original drawing. For F4
  windows, draw our own views and hand every input to vanilla's own grid callbacks, with
  vanilla's panels hidden reversibly, so item handlers and other mods' hooks remain intact
  (D-032, which replaced D-025's move-and-skin). Patch only a proven blocking behaviour under `docs/PATCH-POLICY.md`;
  never substitute new item-transfer logic. Precise alignment and input behavior require
  a client test at the target GUI scales, including fault/teardown restoration.
- **Animated liquid in all three resource bars:** apply this to **HP, stamina and eitr**,
  using `hp_texture.png`, `stamina_texture.png` and `eitr_texture.png` respectively. Keep
  each metallic frame stationary; put its liquid behind it, clipped both to that bar's
  shaped opening and to its current resource level. HP is taller/wider than stamina and
  eitr, so use a matching opening mask and content insets for each size without stretching
  the ornaments. Preserve the existing loss trail, surface and low-HP effects in their
  appropriate layers. Animate texture UVs or a supported UI material, not the frame or
  the bar's geometry; resource level and texture motion are independent. The three supplied
  liquids are static images, **not animation frames or seamless loops**. Prepare/test a
  looping version (or another seam-free motion) before continuous UV scrolling; keep
  motion subtle and allocation-free per frame. Apply the same stamina liquid principle
  to the horizontal stamina bar, whose earlier review candidate Diego rejected. Do not
  claim a finished effect for any of the three until it is checked in the running game.
- **Asset quality gate:** inspect each cutout against dark and light backdrops at its
  intended on-screen size for fringe colours, alpha halos, seams, stretched highlights,
  corner leaks and legibility. The isolated sheets still show some coloured edge halos.
  Record the sprite's borders/content insets in `art/out/sprites.json`; verify the package
  with the shipped-art test and Diego's client script. Do not mistake a 4K upscale for
  restored detail or a successful build for visual approval.

## 3. Language

- Code, identifiers, logs, comments, commit messages and repository docs: **English**.
- Player-facing text: **pt-BR** first, English second, through `Translations/`; Norse
  tone, not literal translation.
- Test scripts (`docs/testing/scripts/`): **pt-BR**, because Diego executes them.
- Credit third-party inspirations once, in the README credits. Do not scatter
  mentions in code.

## 4. Environment and constraints

- References: `tools/fill-ref.sh` copies the production server's assemblies (game,
  BepInEx, the exact `Jotunn.dll` that runs there) into `ref/` (gitignored). Refresh the
  source first with the genesisheim `devplugins` copy after any game/BepInEx/Jötunn
  update. We do not use the JotunnLib NuGet package or publicized assemblies (D-017).
- Build: `dotnet build GenesisUI.sln -c Debug|Preview|Release`.
- Tests: `dotnet test GenesisUI.sln -c <channel>`. Contract tests read `ref/` and the
  built plugin; if either is missing, they skip with a message.
- Package: `tools/package.sh [Preview|Release]` runs the tests, checks the merge and the
  icon, and writes `dist/GenesisMods-GenesisUI-<version>[-preview.N].zip`. Commit first:
  the watermark shows the git sha of HEAD.
- Art: `tools/.venv/bin/python tools/art/render.py` regenerates shapes, patterns, the icon,
  the PNGs and `art/out/sprites.json` (venv setup in the script header). Fonts:
  `tools/art/fonts.py` (downloads the OFL sources with pinned SHA-256; sources gitignored).
- **Reading the game**: `dotnet run --project tools/inspect -- <assembly> <Type> [filter]` lists
  members of `ref/*.dll` by metadata; use it before declaring a `[GameContract]`. To read
  vanilla logic, decompile **one type only**, with a heap cap:
  `DOTNET_ROLL_FORWARD=Major DOTNET_GCHeapHardLimit=0x30000000 ilspycmd -t <Type> -r <Managed dir> ref/assembly_valheim.dll`
  into the scratchpad. Never decompile whole assemblies or several at once. Decompiled code
  is for understanding behaviour only; it is never copied into the repository.
- The development VPS has little memory: **run one heavy task at a time** (build,
  decompilation, large test runs) and cap heaps (`DOTNET_GCHeapHardLimit`).
  Parallel `ilspycmd` runs have already taken it down.
- **Never test on a server, never touch production.** Game tests happen only on
  Diego's client: you prepare the package **and** its test script; he runs it.
- One person runs every test: no step may need a second player or client.
- Diego tests on **his own PC**, in the Gale mod-manager profile `test` (logs under
  `...\com.kesomannen.gale\valheim\profiles\test\BepInEx`, our own log in
  `BepInEx\GenesisUI\logs`). The machine behind `ssh win-teste` is a separate test machine
  (user `heimdall-teste`) without his client or logs: do not search it for his logs.
  `win-teste` may be used for GenesisUI client checks only with notice (say what and why
  first, never start Valheim without telling him, nothing unrelated). When he says stop, stop.

## 5. How to do common tasks

**Add a module**
1. Create `src/GenesisUI/Modules/<Name>/` with a class implementing `Host/IUiModule`
   (`Id`, `NameToken`, `Regions`, `RefreshRate` — 0 = every frame, `Build`, `Refresh`,
   `Teardown`) and its views; reuse `Widgets/` (`Ui`, `SlotView`, `TileView`) before
   writing new ones. Regions vanilla creates on demand are declared dynamic in
   `RegionRegistry` and veiled by the module as they appear (see `BossModule`, `EnemyModule`).
2. Put every game member it touches in `[GameContract(...)]` attributes on the class
   (check names with `tools/inspect`). The contract test finds them automatically.
3. Add each vanilla region to `Host/RegionRegistry.cs` (resolver + the region list of
   known foreign owners such as SeneaL UI) and to `docs/regions.md` with the veil and what
   vanilla does to it.
4. Register it in `Plugin.cs` with a `[Modules] <Name>` config toggle; add its name token to
   both `Translations/*/genesisui.json`; give it `[<Name>]` offset/scale config with pt-BR
   descriptions.
5. Put logic (formatting, diffing, layout) in `GenesisUI.Core` with unit tests.
6. Add a pt-BR test script and bump the version (below).

**Add art** — for a piece present in Diego's isolated sheets, follow §2b and preserve that
source's metallic detail; do not redraw it as a flatter generated approximation. For a piece
absent from the sheets, add or change a function in `tools/art/shapes.py` using the gold
style/motifs in `tools/art/style.py` (D-023). Register each shipped sprite with its design
size, borders and, for frames that hold content, its content insets (views read them through
`ThemeRuntime.Content`). Run `render.py`, then `tools/art/mock.py` and inspect the full HUD
before shipping; add the new element to the mock. Generated SVGs are not hand-edited.
Colours used by generated graphics come from theme tokens. The health bar stays the largest;
default positions are minimap top-right and hotbar bottom centre until player positioning
is implemented.

For D-026 art, start with the isolated sheets in §2b and build reproducible per-element
cutouts. The existing `tools/art/extract.py`, `tools/art/refine_concept.py` and
`docs/F4-ART-REVIEW.md` describe the older concept-crop experiments; reuse their code only
where it preserves the new source art and revise their documentation before integration.
Keep full source sheets outside the repository and record each sprite's borders, content
insets and background mask before shipping.

**Deliver a test build (the loop with Diego)**
1. Bump `src/GenesisUI/PluginInfo.cs`: `Version` for new features, `PreviewNumber` for another
   package of the same version.
2. Write `docs/testing/scripts/R-0NN-<topic>.md` in pt-BR from the template: what is tested,
   what is not, numbered steps with "Esperado", what to send back. Focus on what shows on
   screen; foundation behaviour is covered by the automated tests (Diego asked for fewer
   foundation scripts). Ask for the F8 report at the **end** of the run.
3. Update CHANGELOG, commit, then `tools/package.sh Preview` (the watermark shows HEAD's sha),
   push the feature branch.
4. When Diego reports back, record it in `docs/testing/results/R-0NN-<version>.md` (passed,
   bugs with their proven cause, feedback) before changing code. Merge the branch into `main`
   (fast-forward) only after he approves the phase. Pushing `main` requires Diego's explicit
   authorization; he authorized the agent to synchronize this 1.1.2 release on 2026-10-02.
5. Update §0 of this file, the ROADMAP note and the README status line when the state
   changes (phase, last package, open items).

**Add an adapter** — follow [ADAPTERS.md](docs/ADAPTERS.md): declare GUID, version
range and contracts; read-only; contract test against the real DLL in
`ref/adapters/`; `docs/adapters/<Mod>.md` with the version matrix and test script.

**Change the Extension API** — update EXTENSION-API.md in the same commit, note it
under "API" in CHANGELOG.md, keep `[Obsolete]` rules from §5.

**Add a Harmony patch** — go through the checklist in PATCH-POLICY.md and write, in
the commit message or PR, what happens to other mods' patches on that method.


## 6. Definition of done

- [ ] Builds in Debug, Preview and Release; no new warnings in Core.
- [ ] L1 and L2 tests pass (or skip only for missing `ref/`, stated in the report).
- [ ] Banned-API scan passes with no new allow-list entry (or one justified by a
      decision entry).
- [ ] Docs updated in the same change (architecture, regions, API, decisions).
- [ ] New or changed behaviour visible in the diagnostics overlay and logs.
- [ ] Test script written or updated for anything that needs the game to verify.
- [ ] CHANGELOG entry.

## 7. Reporting back

Say what you verified and how, and what you did not verify. A step that was skipped
is reported as skipped. Never describe something as tested in-game unless Diego ran
the script and reported the result. Numbers (performance, counts, versions) come from
a source you checked, with the source named.

## 8. Commits and branches

- Small commits, English messages, imperative mood, explaining why.
- Work on branches; `main` holds reviewed work. Tags `vX.Y.Z` for releases.
- Never commit `ref/`, build output, packages, full concept images, or anything extracted
  from another mod.
