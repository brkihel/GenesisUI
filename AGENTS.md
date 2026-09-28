# GenesisUI — Instructions for agents

You are working on GenesisUI, a client-side, visual UI mod for Valheim whose first
two priorities are **security** and **stability**. Read this file completely before
any change. When a rule here conflicts with your habits, this file wins. When a rule
is unclear, ask; do not guess.

## 1. Read first, in this order

1. [docs/VISION.md](docs/VISION.md) — what GenesisUI is and is not.
2. [docs/DECISIONS.md](docs/DECISIONS.md) — decisions already made. Do not re-open
   them; propose a new entry if you believe one is wrong.
3. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — layers, modules, regions, veil,
   scheduler, input, layout, theme, security model.
4. [docs/PATCH-POLICY.md](docs/PATCH-POLICY.md) — before touching Harmony.
5. The document for your task: [ART-DIRECTION](docs/ART-DIRECTION.md),
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
   closed mod; never ship game assets; never commit the concept art. Studying
   behaviour is allowed; reusing implementation is not.
9. **No new runtime dependency** without a decision entry. Allowed today: BepInEx,
   Jötunn (latest).
10. **Data files are data**: JSON via `JsonUtility` into typed classes, size-bounded,
    validated and clamped in Core. Nothing in a data file selects a type, a method or
    a path outside our folders.
11. **Diagnostics are part of the feature.** Anything new must appear in the
    diagnostics overlay (the host gives modules this for free) and log its failures.
    Debug and Preview builds always ship the full diagnostics layer.

## 3. Language

- Code, identifiers, logs, comments, commit messages and repository docs: **English**.
- Player-facing text: **pt-BR** first, English second, through `Translations/`; Norse
  tone, not literal translation.
- Test scripts (`docs/testing/scripts/`): **pt-BR**, because Diego executes them.
- Credit third-party inspirations once, in the README credits. Do not scatter
  mentions in code.

## 4. Environment and constraints

- Build: `mods/devplugins/compilar.sh <csproj> [Release|Debug]` from the genesisheim
  repository (it sets `VALHEIM_INSTALL`, `BEPINEX_PATH` and publicizes assemblies).
  Preview is a separate configuration; see RELEASE.md.
- Tests: `dotnet test` from the repository root. Contract tests need `ref/` (gitignored);
  if it is missing, they skip with a message.
- The development VPS has little memory: **run one heavy task at a time** (build,
  decompilation, large test runs) and cap heaps (`DOTNET_GCHeapHardLimit`).
  Parallel `ilspycmd` runs have already taken it down.
- **Never test on a server, never touch production.** Game tests happen only on
  Diego's client: you prepare the package **and** its test script; he runs it.
- One person runs every test: no step may need a second player or client.
- Never run anything on Diego's personal PC unless he asks.

## 5. How to do common tasks

**Add a module**
1. Create `src/GenesisUI/Modules/<Name>/` with the module class, its views and readers.
2. Declare `Regions` and `Requires`; add the contracts to `GenesisUI.Contract.Tests`.
3. Put logic (formatting, diffing, layout) in `GenesisUI.Core` with unit tests.
4. Add the regions to `docs/regions.md` with the veil strategy.
5. Add default layout, theme tokens used, config entries (with pt-BR descriptions).
6. Write the test script (pt-BR) from the template and link it in the PR.

**Add an adapter** — follow [ADAPTERS.md](docs/ADAPTERS.md): declare GUID, version
range and contracts; read-only; contract test against the real DLL in
`ref/adapters/`; `docs/adapters/<Mod>.md` with the version matrix and test script.

**Change the Extension API** — update EXTENSION-API.md in the same commit, note it
under "API" in CHANGELOG.md, keep `[Obsolete]` rules from §5.

**Add a Harmony patch** — go through the checklist in PATCH-POLICY.md and write, in
the commit message or PR, what happens to other mods' patches on that method.

**Add art** — SVG source in `art/src/`, colors from theme tokens, license noted in
`art/LICENSES.md`, rebuild the atlas with `tools/`.

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
- Never commit `ref/`, build output, packages, concept art, or anything extracted
  from another mod.
