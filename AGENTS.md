# GenesisUI — Instructions for agents

You are working on GenesisUI, a client-side, visual UI mod for Valheim whose first
two priorities are **security** and **stability**. Read this file completely before
any change. When a rule here conflicts with your habits, this file wins. When a rule
is unclear, ask; do not guess.

## 0. Where the project is (keep this section current)

- **Phase:** F3 (HUD) approved by Diego on 2026-09-28. **F4 — inventory and crafting
  windows** is next, on branch `f4-windows`; its plan (approach "dress vanilla", steps
  F4.0–F4.5) is [docs/F4-PLAN.md](docs/F4-PLAN.md): target ConceptArt (9)/(12), rearrange
  and dress vanilla's windows (behaviour stays vanilla), art extracted from the concept;
  Diego approved the revision and confirmed the concept's rights (D-025/D-026). Order after that: F5 menus (main and Esc),
  F6 map, F7 Extension API and adapters, F8 optional gameplay package, F9 switch-over
  (D-024). Do not start API or adapter work before the vanilla UI is done.
- **Version:** 0.5.0, last package `0.5.0-preview.4` with script R-041 (R-040 fixes), not
  yet run by Diego. `main` holds F0–F3 (fast-forwarded by Diego to `479a11e`);
  `f4-windows` branches from it. Fast-forwarding `main` needs Diego: the agent's permissions block merges
  into `main`, so give him the command.
- **Art direction:** gold only (D-023). The carved-wood style was tried and rejected; do not
  propose another style unless Diego asks.
- **HUD modules (11):** `hud.vitals`, `hud.food`, `hud.hotbar`, `hud.minimap`, `hud.boss`,
  `hud.enemy`, `hud.hover`, `hud.notice`, `hud.status`, `hud.sprint`, plus the key-hint nudge
  owned by `hud.hotbar`. Each has a `[Modules]` toggle and its own config section.
- **Open items from Diego:** boss plate has unused space (refine later); player positioning
  of HUD pieces (after F3, not scheduled yet); modpack performance check at the first F4 run.

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

1. **The UI never changes game state directly.** No `ClaimOwnership`, no
   `Inventory.AddItem/RemoveItem`, no `ZDO.Set*`, no RPC. Clicks call the same vanilla
   entry points the vanilla UI calls. The banned-API test enforces it.
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
    Never edit a generated SVG in `art/src/`. New art is ours
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

**Add art** — add or change a function in `tools/art/shapes.py` using only the style and
motifs of `tools/art/style.py` (containers vs cells, diamond / volute / bead). There is one art
direction, gold (D-023); do not start another without Diego asking. Register the sprite in the
`SPRITES` table with its design size, 9-slice border and, for frames that hold something, its
content insets (views read them through `ThemeRuntime.Content`). Run `render.py`, then
`tools/art/mock.py` and look at the full-HUD mock before shipping to judge cohesion (add the new
element to the mock). The only hand-written source is `art/src/bar_fill.svg`, a white gradient
texture tinted in game, not a shape. Colours in code come
from theme tokens. Diego's direction: **delicate, subtle, refined and memorable; never as busy
as the concept art**; the health bar stays the largest; default positions: minimap top-right,
hotbar bottom centre (the player will be able to move them later).

For D-026 concept cutouts, use `tools/art/extract.py` and review the candidate through
`tools/art/refine_concept.py` and `docs/F4-ART-REVIEW.md`. Keep the full concepts outside the
repository and update the extracted sprite's 9-slice borders before integration.

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
   (fast-forward) only after he approves the phase; the agent cannot push to `main`, so
   hand him the command.
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
