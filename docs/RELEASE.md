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
GenesisMods-GenesisUI-<version>.zip
  manifest.json          name, version_number, website_url, description (≤ 256 chars), dependencies
  icon.png               exactly 256×256
  README.md
  CHANGELOG.md
  LICENSE
  GenesisUI.dll
  fonts/                 Cinzel, Cormorant Garamond + OFL.txt
  Translations/English/genesisui.json
  Translations/Portuguese_Brazilian/genesisui.json
  art/genesisui.atlas.png + genesisui.atlas.json
```

Dependency in the manifest: `ValheimModding-Jotunn-<latest>`. BepInExPack is added by Hexium on upload.
Built with `devplugins/compilar.sh` and packaged with a GenesisUI-aware version of
`devplugins/empacotar.sh` (it must include the extra folders above).

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
