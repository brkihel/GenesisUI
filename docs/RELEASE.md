# GenesisUI — Packaging and Release

## Versioning

- Semantic versioning. 1.0.0 shipped on 2026-09-30 with the HUD and every in-game window
  (the main menu and the game's settings screen follow in 1.x); the Extension API
  follows [EXTENSION-API.md §5](EXTENSION-API.md#5-versioning).
- The version lives in one place (`PluginInfo.cs`) and is stamped into the assembly,
  the manifest and the diagnostics header.
- Channel suffixes: `-dev+<sha>` (Debug), `-preview.N+<sha>` (Preview). Release has
  none.

## Package (Hexium format)

Author namespace `GenesisMods`, package name `GenesisUI`, BepInEx GUID
`Genesis.GenesisUI`.

```
GenesisMods-GenesisUI-<version>[-preview.N].zip
  manifest.json          name, version_number, website_url, description (≤ 256 chars), dependencies
  icon.png               exactly 256×256
  README.md
  CHANGELOG.md
  LICENSE
  plugins/
    GenesisUI.dll        GenesisUI.Core merged in (ILRepack); the package refuses a DLL without it
  build-evidence.json    source commit, channel, reference assembly hashes, packaged file hashes
    Translations/English/genesisui.json
    Translations/Portuguese_Brazilian/genesisui.json
    fonts/               Cinzel, Cormorant Garamond + OFL.txt
    art/                 sprites.json + its PNGs + genesisui.shaders + shader-provenance.json
```

Dependency in the manifest: `ValheimModding-Jotunn-<version in ref/>`, checked
against `ref/Jotunn.dll`. BepInExPack is added by Hexium on upload. Built and packaged
by `tools/package.sh`, which runs every test first. The manifest `version_number` is
always `MAJOR.MINOR.PATCH`; the preview number lives in the file name, the watermark
and the logs.

On Windows, `pwsh -File tools/package.ps1 Preview` or `Release` produces the same layout and
validates the completed archive. It uses restored dependencies and serial MSBuild
to keep packaging bounded on development machines.

Both package paths require committed tracked/new project files, the keyed bundle and matching
source/bundle provenance. Every completed ZIP entry is compared by SHA256, including generated
metadata. Runtime shader loading checks the provenance and bundle hash; missing/incompatible
evidence keeps sprite/2D fallbacks and names the reason in the log/F8 report.

For shader changes on Windows, `pwsh -File tools/shaders/build.ps1 -DirectEditor`
builds with Unity 6000.0.75f1 in `dist/shader-build`, checks the shipped keyed shader on
the GPU, and only then replaces the bundle and packages the Preview. The licensing
service must be accessible to that process. A failed shader build never replaces the
last working bundle. Commit the shader bundle/provenance/source changes, then run the package command.
`-PackagePreview` is usable only when that command's commit gate is already satisfied.

Release 1.1.2 reuses the exact shader bundle verified for R-067 from source `cfb8841`:
Direct3D11 keyed composition, three border shapes and four equipment-orbit phases in Unity
6000.0.75f1. Its source/bundle hashes remain unchanged when selecting Release; no shader rebuild
is needed. The isolated runtime-helper fixture also verifies nine rig scale pairs. Evidence and
Diego's overall Release approval are recorded in `docs/testing/results/R-067-1.1.2-preview.3.md`.

## Approved Release 1.1.2 (2026-10-02)

Diego approved preview.3 and authorized the Release ZIP for his Hexium upload. The binary
version is already 1.1.2; selecting Release removes the preview suffix, watermark and overlay.
F8 still writes a report. The manifest requires the checked reference Jotunn 2.30.2. Commit
the approval/player notes, then run `tools/package.ps1 Release` and tag that source `v1.1.2`.
The verified archive/source/hash are in [releases/1.1.2](releases/1.1.2.md). Diego also explicitly
authorized the agent to push main and update the repository. Publish the existing tagged ZIP on
GitHub; documentation synchronization may follow the release tag without replacing it.
Hexium upload and production rollout remain separate steps.

## Preview delivery workflow

1. L1–L3 green.
2. Package built with the `Preview` configuration; watermark shows the version.
3. A test script exists for this preview in `docs/testing/scripts/`.
4. Package + script handed to Diego. **Not** published on Hexium, **not** installed
   on any server.
5. Results recorded in `docs/testing/results/`.

## Release publication checklist

- [x] Diego approved the final preview for Release; detailed unreported scenarios are tracked separately.
- [x] Contract tests passed against the production references recorded in R-065/R-067.
- [x] Manifest and compiled reference agree on Jötunn 2.30.2; production upgrades need their own rollout.
- [x] Player CHANGELOG and pt-BR Hexium notes prepared.
- [x] Release metadata confirms no diagnostics overlay and no watermark.
- [ ] Published on Hexium under `GenesisMods`.

## Production rollout on GenesisHeim (only after Diego's go)

- [ ] **AzuAntiCheat**: add `GenesisUI.dll` to
      `BepInEx/AzuAntiCheat/Whitelist` **before** players update. The server runs
      with `Instant Ban = true` and `Enforce Loaded Code Integrity = Enforce`; a
      missing entry bans the player on join.
- [ ] Decide coexistence with SeneaL UI: while both are installed, GenesisUI modules
      whose regions SeneaL owns stay `Blocked`.
- [ ] Removing SeneaL UI also removes `GenesisMods-SenealBackpacksCompat`, and the
      Backpacks adapter must be active first.
- [ ] Config files created as the `valheim` user, never root.
