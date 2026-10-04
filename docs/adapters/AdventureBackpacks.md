# AdventureBackpacks inventory adapter

Development: `1.2.0-preview.1`. GUID `vapok.mods.adventurebackpacks`; supported version **2.0.3 only**.
Runtime prerequisite checks GUID, exact version, owning live assembly and declared members;
unknown/missing versions block this module, retaining the other adapters and UI.

Reference: [Official Thunderstore download](https://thunderstore.io/package/download/Vapok/AdventureBackpacks/2.0.3/); DLL extracted only into gitignored ref/adapters. SHA-256 `4CA276A657D1FE79888A298852DE90C5FDD5AFB6049A62BBD17572A9C82BCC06`. DLLs and decompilation scratch never ship.

Uses public ABAPI IsBackpack/IsThisBackpackEquipped/CanOpenBackpack/GetEquippedBackpack/OpenBackpack. Only the owning mod's equipped pack can be opened with Use; project the matching live native grid below the inventory. Original cape/equipment restrictions remain. No API assembly/wrapper is copied or merged. [Author API documentation](https://github.com/Vapok/AdventureBackpacks/blob/main/Docs/AdventureBackpacksAPI.md) can describe newer methods; this adapter pins the actual 2.0.3 DLL members.

Game/foreign members are declared on the adapter and checked against the real DLL by
`Foreign_adapter_contracts_match_the_exact_owning_plugin`. Reflection resolves during Build;
refresh uses cached delegates/accessors. Owner-guarded calls and disposable registration
provide local fault cleanup. No patches target foreign code; equip/input patches retain
native execution and foreign hooks (D-043). The native grid remains the transfer engine.

## Version and client matrix

| Version | Contracts | In-game behavior/visuals |
| --- | --- | --- |
| 2.0.3 | Verified against the above reference DLL | Pending [R-069](../testing/scripts/R-069-modular-inventory.md) |
| Other versions | Unsupported by design | No certification |

This is a client preview, not production/modpack certification. Adventure Backpacks is absent from the selected profile; its optional run is reported separately.
