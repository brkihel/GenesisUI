# Jewelcrafting inventory adapter

Development: `1.2.0-preview.1`. GUID `org.bepinex.plugins.jewelcrafting`; supported version **2.0.10 only**.
Runtime prerequisite checks GUID, exact version, owning live assembly and declared members;
unknown/missing versions block this module, retaining the other adapters and UI.

Reference: GenesisHeimLocal DLL; identical FullPlaythrough hash. SHA-256 `92D39BF1627BED53F7BFA69C62B4A8A38622696010E3E24CBCB70A25BE0B36D1`. DLLs and decompilation scratch never ship.

Native configured ring/neck predicates select equipment cells. The original Socket tab/button and recipe row buttons drive crafting. The actual RecipeDataPair list supplies Socket candidates/CanCraft; native warnings remain visible with enabled buttons. OpenFakeSocketsContainer.Open handles the gem inventory; sealed containers and inventorySocketing/inventoryInteractBehaviour restrictions are retained. Native ItemBag inventory Use outside the Socket station is not newly generalized by this slice; ordinary Socket station and configured inventory socketing are supported.

Game/foreign members are declared on the adapter and checked against the real DLL by
`Foreign_adapter_contracts_match_the_exact_owning_plugin`. Reflection resolves during Build;
refresh uses cached delegates/accessors. Owner-guarded calls and disposable registration
provide local fault cleanup. No patches target foreign code; equip/input patches retain
native execution and foreign hooks (D-043). The native grid remains the transfer engine.

## Version and client matrix

| Version | Contracts | In-game behavior/visuals |
| --- | --- | --- |
| 2.0.10 | Verified against the above reference DLL | Pending [R-069](../testing/scripts/R-069-modular-inventory.md) |
| Other versions | Unsupported by design | No certification |

This is a client preview, not production/modpack certification. The selected Gale profile is GenesisHeimLocal; its native settings are not changed by packaging.
