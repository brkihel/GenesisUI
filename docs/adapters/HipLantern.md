# HipLantern inventory adapter

Development: `1.2.0-preview.1`. GUID `shudnal.HipLantern`; supported version **1.1.12 only**.
Runtime prerequisite checks GUID, exact version, owning live assembly and declared members;
unknown/missing versions block this module, retaining the other adapters and UI.

Reference: GenesisHeimLocal DLL; identical FullPlaythrough hash. SHA-256 `85D279F81D49AD2F8F30F9213516B1CD9057997CBC8A2CF296FEF8E29E45462E`. DLLs and decompilation scratch never ship.

IsLanternItem classifies the dedicated lantern cell only when itemSlotUtility is false. Utility mode retains native equipment behavior and does not create another lantern capacity. Light/fuel/equip operations remain in the original mod.

Game/foreign members are declared on the adapter and checked against the real DLL by
`Foreign_adapter_contracts_match_the_exact_owning_plugin`. Reflection resolves during Build;
refresh uses cached delegates/accessors. Owner-guarded calls and disposable registration
provide local fault cleanup. No patches target foreign code; equip/input patches retain
native execution and foreign hooks (D-043). The native grid remains the transfer engine.

## Version and client matrix

| Version | Contracts | In-game behavior/visuals |
| --- | --- | --- |
| 1.1.12 | Verified against the above reference DLL | Pending [R-069](../testing/scripts/R-069-modular-inventory.md) |
| Other versions | Unsupported by design | No certification |

This is a client preview, not production/modpack certification. The selected Gale profile is GenesisHeimLocal; its native settings are not changed by packaging.
