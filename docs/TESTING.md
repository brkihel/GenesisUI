# GenesisUI — Testing

## Where tests run

- **Nothing is tested on a server** at this stage, and never in production.
- Automated tests (L1–L3) run on the development machine with `dotnet test`.
- Game tests (L4) run **only on Diego's client**, in a separate mod profile, in a
  local single-player world. The developer prepares the package and a **test
  script**; Diego installs, follows the script, and reports.
- One person runs every test: no script may assume two clients or a second player.

## L1 — Core unit tests

`tests/GenesisUI.Core.Tests` (xUnit). Everything in `GenesisUI.Core`: formatting
(invariant culture), layout solving and clamping, layout/theme file validation,
view-model diffing, module state machine, settings models. Fast, no game.

## L2 — Contract and banned-API tests

`tests/GenesisUI.Contract.Tests`, following the pattern of GenesisPlayerBots
`EngineContractTests` (metadata-only load with `MetadataLoadContext`):

- **Game contracts**: every patch target and every member declared in a module's
  `Requires` exists in `ref/assembly_valheim.dll` with the expected signature. The
  verified game build is written in the test file.
- **Adapter contracts**: every declared foreign member exists in the mod DLL under
  `ref/adapters/`.
- **Banned-API scan**: the plugin assembly's IL is scanned. Calls to
  `ZNetView.ClaimOwnership`, `Inventory.RemoveItem`, `Inventory.AddItem`,
  `ZRoutedRpc.Register`/`InvokeRoutedRPC`, `ZDO.Set*`, `Harmony.Unpatch`/`UnpatchAll`,
  `Harmony.PatchAll`, and file writes outside the diagnostics writer fail the test.
  An exception needs a decision entry and an allow-list line naming the calling
  method.

`ref/` is gitignored; `tools/` fills it from `devplugins/referencias` and the
modpack. Missing refs → tests are skipped with a clear message, not failed.

## L3 — Build checks

- Core purity guard (build error).
- `TreatWarningsAsErrors` in Core; analyzers (`BepInEx.Analyzers`,
  `Microsoft.Unity.Analyzers`) in the plugin.
- Package check: the zip contains exactly the expected files, manifest fields valid
  for Hexium, icon 256×256.

## L4 — Client test scripts ("roteiros")

Every Preview package comes with a script in `docs/testing/scripts/`, written in
**Brazilian Portuguese** (Diego runs them), from
[ROTEIRO-TEMPLATE.md](testing/ROTEIRO-TEMPLATE.md). A script:

- states in one sentence **what is being tested** and lists what is **not**;
- lists preparation exactly (profile, mods, world, config values);
- has numbered steps, each with **Faça** (what to do) and **Esperado** (what should
  happen), plus what to note if it differs;
- uses the diagnostics overlay and fault injection to prove the failure paths;
- ends with **what to send back** (report file, log files, named screenshots);
- takes at most ~20 minutes. Bigger scopes are split into several scripts.

Results are recorded by the developer in `docs/testing/results/<script-id>.md`
(date, package, pass/fail per step, notes, follow-ups). A failed step becomes an
issue before the next preview.

### Standing scripts (run on every preview)

- **R-000 Smoke**: game starts, main menu, enter world, overlay opens, no errors in
  the session log, exit to menu, re-enter, quit.
- **R-001 Kill switch**: master toggle off → full vanilla UI; on → GenesisUI; each
  active module off/on individually.
- **R-002 Fault isolation**: inject a fault into each active module; its region
  returns to vanilla, the others keep working, the report names the fault.
