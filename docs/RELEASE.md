# GenesisUI — Packaging and Release

## Versioning

- Semantic versioning. `0.x` until the F5 windows are stable; the Extension API
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
    Translations/English/genesisui.json
    Translations/Portuguese_Brazilian/genesisui.json
    fonts/               (F2) Cinzel, Cormorant Garamond + OFL.txt
    art/                 (F2) genesisui.atlas.png + genesisui.atlas.json
```

Dependency in the manifest: `ValheimModding-Jotunn-<version in ref/>`, checked
against `ref/Jotunn.dll`. BepInExPack is added by Hexium on upload. Built and packaged
by `tools/package.sh`, which runs every test first. The manifest `version_number` is
always `MAJOR.MINOR.PATCH`; the preview number lives in the file name, the watermark
and the logs.

## Preview delivery (current stage)

1. L1–L3 green.
2. Package built with the `Preview` configuration; watermark shows the version.
3. A test script exists for this preview in `docs/testing/scripts/`.
4. Package + script handed to Diego. **Not** published on Hexium, **not** installed
   on any server.
5. Results recorded in `docs/testing/results/`.

## Release checklist (later stages)

- [ ] All previews of the milestone passed their scripts.
- [ ] Contract tests run against the game build currently in production.
- [ ] Jötunn: compiled against the latest version; production updated to it in the
      same rollout.
- [ ] CHANGELOG written for players (pt-BR summary on the Hexium page).
- [ ] Release build has no diagnostics overlay and no watermark.
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
