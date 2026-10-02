# FullPlaythrough compatibility study

Snapshot: 2026-10-02. Runtime baseline: `2a1854a`.
Read this with the [stability findings](README.md) and [implementation gates](IMPLEMENTATION.md).

## Inventory and limitations

The supplied Gale profile contains **71 package manifests**, **74 plugin-folder DLLs** and
**71 BepInPlugin metadata records**. Those are different sets: libraries, bundled assemblies,
embedded APIs and duplicate plugin copies prevent a one-to-one interpretation.
Its previous client log has **70 Loading entries**. No GenesisUI Loading entry appears there.
The profile was read, not modified or launched.

Exact manifest versions/dependencies are in [modpack-packages.csv](modpack-packages.csv).
DLL versions and hashes are in [modpack-assemblies.csv](modpack-assemblies.csv); authoritative
plugin identities for binding are in [modpack-plugin-identities.csv](modpack-plugin-identities.csv).
The [loaded list](modpack-last-loaded.csv) contains only extracted plugin names/versions,
without player, world, address or account information.

Observed discrepancies:

- ExplorerBiomeCompat 1.0.0 has a duplicate-plugin warning in the old log: one copy was skipped
  because the same GUID `BRKiHeL.ExplorerBiomeCompat` was already loaded from another folder.
- MrDayNight's manifest says 2.0.1; the old Loading entry says 2.0.0. Distinguish manifest,
  plugin and assembly versions; establish which binary/config is authoritative before testing.
- SeneaL UI 1.1.7 and SenealBackpacksCompat 0.1.7 were loaded. The latter's manifest dependency
  names SeneaL UI 1.1.6. That does not establish incompatibility, but it is part of provenance.
- BowsBeforeHoes is not present in this package/DLL snapshot although GAMEPLAY.md proposes a
  shared backpack/quiver slot. A quiver integration is conditional on actual server/client use.
- The installed Steam game DLL differs from the production `ref/` DLL. Both resolve the 562
  current declarations, but only the `ref/` implementation was decompiled for behavioral study.

This is not an inventory of active feature flags or server-only plugins. Before certifying a
module, capture the matching server manifest/configuration through an approved source and compare
it with this client snapshot. Read only the specific non-secret settings needed for that capability.

## What “fully compatible” should mean

For every enabled feature, record its original entry point, state source, UI location, action
route and fallback. The certification unit is **capability + exact mod version + relevant config
+ game build**, not a broad label saying a whole mod is supported.

An integration may be:

1. **Mirrored:** vanilla already renders the mod's data; retain that data and its action path.
2. **Provided:** a read-only adapter gives structured presentation data to GenesisUI.
3. **Delegated:** the owning mod's existing UI/vanilla entry point handles an action.
4. **Preserved:** a foreign panel remains reachable and has explicit region ownership.
5. **Unsupported:** explicit diagnostics and a working fallback; no silent hidden feature.

No mod below is marked runtime-compatible by this study. Candidate classifications describe
the first verification path; they are not implementation promises or approved version ranges.

## Priority integration studies

### 1. Backpacks 1.3.10

GUID `org.bepinex.plugins.backpacks`. Metadata exposes an API and `ItemContainer`.
Selected public signatures were checked in the actual Backpacks DLL:
`GetEquippedBackpack()`, `GetEquippedBackpackInventory()`,
`GetAllBackpackInventories(Inventory)` and the `EquippedBackpackUpdate` event are useful
read/invalidation candidates. `ItemContainer` also exposes title, size and item-acceptance queries.
Presence of a public method does not establish that its semantics are safe for every context.

The DLL also offers mutation methods such as add/delete/resize/save. A visual adapter must not
use those to implement transfers. First prove which owning-mod UI entry point opens the backpack
and which vanilla callbacks its current container bridge supplies. The SeneaL bridge is tied to
the old UI and is not automatically a GenesisUI adapter. Study its behavior without copying it.

The profile's AzuAutoStore DLL also contains a `Backpacks.API` type. Bind using the Backpacks
plugin GUID and its owning assembly, not the first type with that full name in the AppDomain.

Required capabilities: equipment slot identity; backpack open/close action; inventory projection;
weight/title/capacity and acceptance rules; allowed transfers; optional crafting counts;
preview visual attachments; item-instance identity and persistence across unequip/death/reload.

First client test: equip/open/close, ordinary-to-backpack/backpack-to-ordinary transfers through
owner handlers, full-container refusal, resizing/upgrading if enabled, relog with contents intact,
and module failure while open. Use a disposable local character/world.

### 2. Jewelcrafting 2.0.10

GUID `org.bepinex.plugins.jewelcrafting`. Public metadata confirms `GetGems(ItemData)`,
`GetSocketBorder()`, `IsJewelryEquipped(Player,string)`, `GetEquippedJewelry(Player)` and
`OnEffectRecalc`. It also exposes tooltip/container helpers. Some apparent presentation helpers
accept Unity roots and can modify them; investigate side effects before classifying them read-only.

Do not invoke `SetGems`, lock setters, registration APIs or item-container mutations from a visual
provider. Socket interaction and gem cutting must stay with the original mod's existing UI/action
path. Read slot state from the mod, rather than guessing from an ordinary ItemType.

Required capabilities: socket badges, full item/gem information, ring and amulet equipment,
effect invalidation, gem-container projection where enabled, jewelcrafting table/action reachability,
and safe preview representation. InventorySettings currently enables only the six vanilla equipment
slots; declaring Ring/Amulet in Core's enum does not activate them.

First client test: unsocketed/socketed item, effect change on the same item, ring+amulet simultaneously,
table/gem interaction, full-inventory refusal and relog. Verify tooltip parity and original action
rules before claiming the dedicated panel replaces the old one.

### 3. HipLantern 1.1.12

GUID `shudnal.HipLantern`. Metadata identifies separate Humanoid/VisEquipment lantern state and
custom item-type hooks. `HumanoidExtension.GetHipLantern(Humanoid)` is a read candidate;
`SetHipLantern` and VisEquipment state setters are not visual-read APIs.

Required capabilities: independent lantern slot, equipped/off/fuel state where configured,
owning-mod toggle input and preview attachment. Core's current ItemType-based classification
cannot establish custom-slot identity. Keep stage lights isolated and prevent real audio/behavior
on preview attachments.

First client test: simultaneous belt+lantern, original toggle key/focus behavior, attachment
replacement and persistence; compare active light behavior with previews open and closed.

### 4. MagicRevamp 1.5.1

GUID `blacks7ar.MagicRevamp`. Metadata shows spell/status-effect types including eitr additions,
comfort, healing, repair and shield effects. It does not by itself prove a dedicated equipment-slot
API or which optional features are enabled. Inventory categories and the five-potion view need
verification against actual configured spell items/effects.

Required discovery: cast/equip entry points, custom resource/state displays, spell selection,
cooldowns, charges, stack/use restrictions and text-focus behavior. Mirror vanilla eitr and status
data where sufficient; add a resource provider only for state vanilla cannot represent.

First client test: representative configured spells, resource loss/regeneration, overlapping effects,
cooldowns and shortcuts while a text field/menu is focused. Confirm spell behavior remains owned by
MagicRevamp; no new cast/RPC logic in GenesisUI.

### 5. StarLevelSystem 1.20.0

GUID `MidnightsFX.StarLevelSystem`. Metadata exposes minimap/no-map indicators, level-distribution
and quick-config UI types. Existing Enemy/Boss views use vanilla level values and bounded stars;
that does not preserve every added indicator or high-level presentation.

Required discovery: numeric level/range state, level-dependent creature styling, map/no-map
indicator, original configuration window and foreign region ownership. Prefer data that already
flows through vanilla; add a provider for additional fields only.

First client test: levels beyond the displayed star capacity, boss, map and no-map configurations,
and an unsupported-provider fault. No second player is required.

### 6. Seasonality 3.8.3 and MrDayNight 2.0.1 manifest / 2.0.0 old load

Seasonality GUID `RustyMods.Seasonality`. Current climate/day views mirror EnvMan, but the season
identity, transitions or additional indicators may live outside that state. Distinguish base weather,
wet/cold effects, season state and modified day length; do not recompute another mod's time rules.

Required discovery: exact enabled season/time UI, stable read source/event and minimap/clock
placement. Proposed output: bounded HUD widget plus original tooltip, not duplicated scheduling.

First client test: day/time display against original UI, a naturally available/configured season,
local transition where accessible, no-map fallback and a disabled season provider.

### 7. HeimdallSagasClient 0.2.1 and GenesisHeimSuite 1.2.1

Heimdall Sagas GUID `gg.heimdall.sagas.client`. Metadata shows gear/jewelcrafting integration and
portrait/UI components. These are candidates for our own explicit API consumer, with source
review in its repository when available; do not infer a protocol from class names alone.

Required discovery: server-specific resources, portrait/status/attribute presentation, menus,
progression indicators, game-state authority and current region ownership. Avoid duplicate portrait
cameras and duplicate attribute logic. Separate display registration from server/gameplay code.

First client test: each actually enabled Sagas/Suite screen and resource, relevant progression
changes obtainable by one person, unsupported/missing consumer and disconnect/reconnect behavior.

### 8. ZenPlayer, ZenUseItem, ZenCombat and ZenWorldSettings

Relevant snapshot versions: 1.3.0, 1.2.0, 1.0.2 and 1.13.3. Metadata candidates include equipment/death
rules in ZenPlayer and hotbar/hover types in ZenUseItem. Exact enabled rules still require a config
and behavior comparison. The broad Zen library dependency is not a reason to add it to GenesisUI.

Required discovery: additional equipment/use semantics, hovered-item output, shortcut priority,
death/tombstone handling of special positions, combat/resource effects and no-map/world rules.
Leave transformations and restrictions with the owning mod; preserve the vanilla entry points on
which its patches depend. S-03 and S-10 must be resolved before this test batch.

First client test: item use and hotkeys, repeated equip/unequip, one-person local death/recovery,
full inventory and configured world restrictions. Backpacks + Jewelry + ZenPlayer death is an
explicit combined-case gate, not implied by three separate individual passes.

### 9. AchievementEnabler and custom skill mods

AchievementEnabler 0.4.1 GUID `MidnightsFX.AchievementEnabler`; skill package versions are below.
The first path is vanilla Skills/Achievements mirroring. Inspect whether injected rows, names,
icons, ordering, progress and long descriptions already survive our views. Avoid unnecessary
adapters that duplicate the mod's level/achievement rules.

First client test: one custom skill from each family, long/localized names, level/progress update
without reopening, full list scrolling and an achievable local achievement. A missing achievement
feature must remain reachable via its original UI until a provider exists.

### 10. Container/crafting/build/hover and support mods

AzuContainerSizes, AzuAutoStore, AzuWorkbenchTweaks, zzzGenesisItemStacks, crafting/content mods,
PlantEverything, Buildheim and Build Camera need content/action verification, not necessarily new
providers. Large grids, high stack counts, shared recipe icons, many build categories and custom
tooltips are concrete cases. AzuHoverStats and ZenUseItem require a before/after text comparison.

AzuAntiCheat, ServerCharacters and ConditionalConfigSync are mandatory environment checks.
Do not bypass them or add unauthorized network logic. AzuAntiCheat compatibility requires a
legitimate client run with current rules; the forbidden-API scan cannot certify that acceptance.
ConfigManager and dev tools need modal/focus/ownership checks, not replication inside our UI.

## Full manifest matrix

All rows below are **pending runtime certification**. “Mirror” means start by testing existing
vanilla data/actions. “Study” means determine a provider/foreign-panel boundary first. “Environment”
means preserve initialization, ownership and restrictions. Versions are package-manifest facts.

| Package | Version | First path | Verification scope |
|---|---|---|---|
| Advize-PlantEverything | 1.21.3 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| AugusDogus-Buildheim | 1.2.1 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| Azumatt-AzuAntiCheat | 5.2.0 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| Azumatt-AzuAreaRepair | 1.1.8 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| Azumatt-AzuAutoStore | 3.1.6 | Mirror first | Inventory capacity/counts, requirements, original transfers and crafting actions |
| Azumatt-AzuContainerSizes | 1.1.8 | Mirror first | Inventory capacity/counts, requirements, original transfers and crafting actions |
| Azumatt-AzuHoverStats | 1.1.11 | Mirror first | Original hover or biome content; duplicate-plugin provenance where applicable |
| Azumatt-AzuSkillTweaks | 1.0.8 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| Azumatt-Azus_UnOfficial_ConfigManager | 19.5.0 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| Azumatt-AzuWorkbenchTweaks | 1.0.8 | Mirror first | Inventory capacity/counts, requirements, original transfers and crafting actions |
| Azumatt-Build_Camera_Custom_Hammers_Edition | 1.3.3 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| Balrond-balrond_DualMastery | 0.2.8 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| blacks7ar-BeeKeeper | 1.1.0 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| blacks7ar-CookingAdditions | 1.3.3 | Mirror first | Content icons/tooltips, large values, recipes and resource/equipment state |
| blacks7ar-CoreWoodPieces | 1.2.6 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| blacks7ar-Explorer | 1.1.7 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| blacks7ar-Herbalist | 1.5.0 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| blacks7ar-Hunting | 1.4.5 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| blacks7ar-MagicRevamp | 1.5.1 | Study | Equipment / containers / item state / preview; dedicated capability slice |
| blacks7ar-OdinsHares | 1.3.4 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| blacks7ar-OreMines | 1.2.1 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| blacks7ar-Wisdom | 1.0.6 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| Dad_Is_Bored-DadsDevCommands | 1.0.1 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| GenesisMods-ExplorerBiomeCompat | 1.0.0 | Mirror first | Original hover or biome content; duplicate-plugin provenance where applicable |
| GenesisMods-GenesisHeimSuite | 1.2.1 | Study | Additional HUD, progression, time or map state; establish enabled features |
| GenesisMods-HeimdallSagasClient | 0.2.1 | Study | Additional HUD, progression, time or map state; establish enabled features |
| GenesisMods-MrDayNight | 2.0.1 | Study | Additional HUD, progression, time or map state; establish enabled features |
| GenesisMods-SenealBackpacksCompat | 0.1.7 | Environment | Migration ownership and equivalent backpack access before switch-over |
| GenesisMods-zzzGenesisItemStacks | 2.3.2 | Mirror first | Inventory capacity/counts, requirements, original transfers and crafting actions |
| Gurebu-Riverheim | 1.2.1 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| jg224-ModCore | 0.5.1 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| jg224-TrulySmoothLadders | 0.5.8 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| MagicMike-CoreWoodExtras | 2.3.1 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| MagicMike-MyDirtyHoe | 2.2.1 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| Max-SailTrim | 1.11.1 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| MidnightMods-AchievementEnabler | 0.4.1 | Mirror first | Achievements list, progress and original completion behavior |
| MidnightMods-StarLevelSystem | 1.20.0 | Study | Additional HUD, progression, time or map state; establish enabled features |
| MidnightMods-ValheimArmory | 1.35.0 | Mirror first | Content icons/tooltips, large values, recipes and resource/equipment state |
| OdinPlus-BlacksmithingExpanded | 1.2.4 | Mirror first | Content icons/tooltips, large values, recipes and resource/equipment state |
| OdinPlus-OdinArchitect | 1.7.9 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| OdinPlus-OdinBear | 1.5.2 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| OdinPlus-OdinHorse | 1.7.5 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| OdinPlus-OdinsKingdom | 1.6.2 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| RustyMods-Seasonality | 3.8.3 | Study | Additional HUD, progression, time or map state; establish enabled features |
| seneaL-SeneaL_UI | 1.1.7 | Environment | Migration ownership and equivalent backpack access before switch-over |
| shudnal-ConditionalConfigSync | 1.0.9 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| shudnal-HipLantern | 1.1.12 | Study | Equipment / containers / item state / preview; dedicated capability slice |
| shudnal-ProtectiveWards | 2.0.15 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| Smoothbrain-Backpacks | 1.3.10 | Study | Equipment / containers / item state / preview; dedicated capability slice |
| Smoothbrain-ConversionSizeAndSpeed | 1.0.18 | Mirror first | Inventory capacity/counts, requirements, original transfers and crafting actions |
| Smoothbrain-Jewelcrafting | 2.0.10 | Study | Equipment / containers / item state / preview; dedicated capability slice |
| Smoothbrain-Lumberjacking | 1.0.7 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| Smoothbrain-Mining | 1.1.7 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| Smoothbrain-Ranching | 1.1.9 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| Smoothbrain-ServerCharacters | 1.4.17 | Study | Original input / equipment / world / persistence rules; combined-case gate |
| Smoothbrain-StartupAccelerator | 1.0.3 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| Smoothbrain-Vitality | 1.1.6 | Mirror first | Skills, progress, names, combat/stat changes; adapt only missing presentation |
| ValheimModding-Jotunn | 2.30.2 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| ValheimModding-JsonDotNET | 13.0.4 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| ValheimModding-YamlDotNet | 16.3.1 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |
| Xutz-ValheimCuisine | 2.3.2 | Mirror first | Content icons/tooltips, large values, recipes and resource/equipment state |
| ZenDragon-ZenCombat | 1.0.2 | Study | Original input / equipment / world / persistence rules; combined-case gate |
| ZenDragon-ZenPath | 1.1.0 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| ZenDragon-ZenPlayer | 1.3.0 | Study | Original input / equipment / world / persistence rules; combined-case gate |
| ZenDragon-ZenRaids | 1.2.3 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| ZenDragon-ZenRedecorate | 1.5.0 | Mirror first | Build categories, many pieces, camera/search focus and original controls |
| ZenDragon-ZenSign | 1.9.3 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| ZenDragon-ZenTargeting | 1.1.0 | Mirror first | Original world/creature/hover/key-hint UI; detect any foreign panel |
| ZenDragon-ZenUseItem | 1.2.0 | Study | Original input / equipment / world / persistence rules; combined-case gate |
| ZenDragon-ZenWorldSettings | 1.13.3 | Study | Original input / equipment / world / persistence rules; combined-case gate |
| ZenDragon-Zen_ModLib | 1.14.19 | Environment | Initialization, authority, modal focus, update/contract provenance; no new runtime dependency |

## Combined scenarios that individual mod tests cannot replace

| Combination | Required invariant |
|---|---|
| Backpacks + AzuAutoStore + AzuContainerSizes | Owning inventory identity, capacity/rules, no accidental placement in special rows |
| Backpacks + Jewelcrafting + ZenPlayer + ServerCharacters | Equip/death/reload retains every item and original persistence behavior |
| Jewelcrafting + MagicRevamp + custom consumables | Same-item details, effects and resource routing update without hidden overflow |
| HipLantern + Backpacks + Jewelcrafting + preview | Dynamic attachments are sanitized and framed; no gameplay/audio/light leakage |
| ZenUseItem + QuickSlots + Configuration Manager | Only eligible shortcut consumes input; text/modal state wins |
| StarLevelSystem + Explorer + Seasonality + no-map world | Every enabled indicator has one visible owner and an explicit fallback |
| Cooking/content/build mods + zzzGenesisItemStacks | Large values, alternative recipes, shared icons and long text remain readable |
| SeneaL UI + SeneaL bridge + GenesisUI during migration | Ownership conflict is explicit; no hidden functional backpack or duplicate interactive windows |
| Any provider fault + open inventory/container | Local recovery, exact restoration, intact original action and item identities |

## Version-policy proposal

Start with the exact versions/hashes in this snapshot. Keep supported ranges conservative until
a second version is actually checked. Missing/unsupported optional mods should disable their own
providers only. After a game/mod update, invalidate certification for affected contracts and
capabilities and run their focused regression batch. Do not silently widen a range because member
names still exist. The final certification matrix must name relevant server config and source hashes.
