# GenesisUI — Adapters

Status: initial F7 inventory slice implemented in `1.2.0-preview.1`, pending R-069 client
approval. Release 1.1.2 remains the public baseline. Adapters are optional guarded host
modules exposing an internal disposable capability registry, not a published Extension API.
Versions outside the matrix are blocked locally with a diagnostic reason.

| Adapter | Exact plugin version | Reference provenance | Client status |
| --- | --- | --- | --- |
| [Backpacks](adapters/Backpacks.md) | 1.3.10 | GenesisHeimLocal / identical FullPlaythrough DLL | R-069 pending |
| [Jewelcrafting](adapters/Jewelcrafting.md) | 2.0.10 | GenesisHeimLocal / identical FullPlaythrough DLL | R-069 pending |
| [HipLantern](adapters/HipLantern.md) | 1.1.12 | GenesisHeimLocal / identical FullPlaythrough DLL | R-069 pending |
| [AdventureBackpacks](adapters/AdventureBackpacks.md) | 2.0.3 | Official Thunderstore DLL; absent from GenesisHeimLocal | Optional R-069 run pending |

No foreign assembly or assets are packaged. Binding uses the owning plugin assembly only.
Real DLL contract tests report a skip per missing optional DLL; they are all supplied locally
for this preview's checks. Native action entry points remain owned by the foreign mod.

The planned public adapter interface serves mods that do not know GenesisUI.
The initial slice instead uses IUiModule/IModulePrerequisites and InventoryIntegrations. It lives in-tree (`src/GenesisUI/Adapters/<ModName>/`), isolated from
everything else. A separate package for an adapter is an exception that needs a
decision entry.

## Rules

1. **Declare, then verify.** Each adapter declares the plugin GUID, the supported
   version range, and every type and member it touches:

   ```csharp
   [Adapter("org.bepinex.plugins.jewelcrafting", MinVersion = "1.5.0", MaxVersionExclusive = "2.0.0")]
   internal sealed class JewelcraftingAdapter : IAdapter
   {
       public IReadOnlyList<ForeignContract> Requires => ...; // types/members by name
   }
   ```

   Contracts are resolved once at startup with Foundation helpers. Anything
   missing → state `Disabled` with the exact member named. No partial activation.
2. **Read-only by default.** Adapters read the other mod's data to feed tooltip
   sections, badges, slot descriptions and panels. Actions call the other mod's own
   public entry points; never its private logic, never item manipulation.
3. **No Harmony patches on the other mod** unless a decision entry justifies it, and
   never `Unpatch`.
4. **Failure is local.** An adapter fault disables that adapter only.
5. **Tested against the real DLL.** `GenesisUI.Contract.Tests` resolves every
   declared member against the mod DLL in `ref/adapters/` (gitignored, filled from
   the modpack). Out-of-range versions are reported in the overlay.
6. **Documented.** `docs/adapters/<ModName>.md` holds the version matrix, what the
   adapter shows, and the test script that exercises it.

## Candidates from the current GenesisHeim modpack

Verified present in production on 2026-09-28. Each needs a check of what it actually
draws before an adapter is written; some may need nothing at all.

| Mod | Why it matters to the UI | Likely extension points |
|---|---|---|
| Smoothbrain Backpacks | backpack inventory; today bridged to SeneaL by `GenesisMods-SenealBackpacksCompat` | `IContainerPanelProvider`, `IEquipmentSlotProvider` |
| Smoothbrain Jewelcrafting | sockets, gems, jewelry | `IItemTooltipSection`, `IItemBadgeProvider`, `IEquipmentSlotProvider` |
| MidnightMods StarLevelSystem | creature levels beyond vanilla stars | enemy/boss bar data |
| Azumatt AzuContainerSizes | non-default container grids | none expected: grids follow `Inventory` size |
| RustyMods Seasonality | season state | `IHudWidget` |
| MidnightMods AchievementEnabler | achievements | "Conquistas" tab provider (to investigate) |
| Smoothbrain skill mods, blacks7ar skill mods | custom skills | none expected: they appear in vanilla `Skills` |
| HeimdallSagas client (ours) | own HUD pieces | API consumer, not an adapter |
