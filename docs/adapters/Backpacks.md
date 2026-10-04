# Backpacks inventory adapter

Development: `1.2.0-preview.1`. GUID `org.bepinex.plugins.backpacks`; supported version **1.3.10 only**.
Runtime prerequisite checks GUID, exact version, owning live assembly and declared members;
unknown/missing versions block this module, retaining the other adapters and UI.

Reference: GenesisHeimLocal DLL; identical FullPlaythrough hash. SHA-256 `8B87E770095001271EED051065A83E589F8AECCDA46FB7BB0298B92323BA43F4`. DLLs and decompilation scratch never ship.

Classifies native IsEquipable ItemContainers, respects AllowOpeningByKeypress and invokes the public OpenFakeItemsContainer.Open UI bridge. Projects its live inventory below the player grid, with all native cell callbacks and save subscriptions retained.

Game/foreign members are declared on the adapter and checked against the real DLL by
`Foreign_adapter_contracts_match_the_exact_owning_plugin`. Reflection resolves during Build;
refresh uses cached delegates/accessors. Owner-guarded calls and disposable registration
provide local fault cleanup. No patches target foreign code; equip/input patches retain
native execution and foreign hooks (D-043). The native grid remains the transfer engine.

## Version and client matrix

| Version | Contracts | In-game behavior/visuals |
| --- | --- | --- |
| 1.3.10 | Verified against the above reference DLL | Pending [R-069](../testing/scripts/R-069-modular-inventory.md) |
| Other versions | Unsupported by design | No certification |

This is a client preview, not production/modpack certification. The selected Gale profile is GenesisHeimLocal; its native settings are not changed by packaging.
