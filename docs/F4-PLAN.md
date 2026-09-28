# F4 — Inventory and crafting windows: plan

Status: **proposed, waiting for Diego's approval** (2026-09-28). Branch `f4-windows`.

## 1. Scope

From the roadmap (D-024): the window shell, inventory and equipment, containers, crafting
and every station (craft, upgrade, repair), skills, texts, trophies, achievements, and the
build piece browser. Main/Esc menus are F5, the map is F6, mod integration is F7.

## 2. What vanilla does (study of `InventoryGui`, `InventoryGrid`, game l-1.0.16)

- **One object, many panels.** `InventoryGui` owns the player panel (`m_player`: grid,
  name, armour, weight, PvP), the container panel (`m_container`: grid, name, weight, Take
  all / Stack all), the crafting panel (`m_crafting`: station name/icon/level, Craft and
  Upgrade tabs, recipe list, recipe details, requirements, quality and variant, craft and
  repair buttons, progress bar), the info panel (`m_info`), and dialogs for skills, texts,
  trophies, achievements, split and variant. It opens and closes itself (Tab, E, Esc,
  gamepad) and shows or hides the crafting tabs depending on the station.
- **Every item operation lives in its private handlers.** `InventoryGrid` only reports
  clicks, drags and right-clicks (`m_onSelected`, `m_onRightClick`, `m_onReleased`);
  `InventoryGui.OnSelectedItem` then moves, swaps, stacks, splits and drops items, keeps
  equipped items equipped across swaps, refuses quest items across inventories, and moves
  to or from the open container. Crafting (`DoCrafting`), upgrading, repairing and the
  split dialog are private too.
- **The grid rebuilds its slots only when the inventory size changes.** Every frame it sets
  each slot's icon, amount, quality, durability, equipped/queued/no-teleport/food marks and
  selection, but never the slot's frame, background or fonts.
- **The recipe list is rebuilt** from `m_recipeElementPrefab` on every crafting refresh;
  the requirement slots are fixed objects that `SetupRequirement` fills (and blinks red when
  something is missing).
- Many mods patch `InventoryGui` and `InventoryGrid` (backpacks, jewelcrafting, crafting
  from containers, sorting). Whatever we do must leave those patches working.

## 3. Approach: dress vanilla, do not replace it (proposed D-025)

Rebuilding the windows as our own interactive UI would mean re-implementing the item
handlers above, the exact place where item loss and duplication happen, and would break
every mod that patches them. Instead:

- **Vanilla keeps all behaviour and input.** Clicks, drags, splits, equip swaps, container
  transfers, crafting and repair stay 100 % vanilla code. GenesisUI never calls an item
  operation of its own for these windows.
- **We restyle vanilla's own objects**, reversibly, through a new Foundation piece,
  **`VanillaSkin`**: it records and replaces an `Image`'s sprite, type and colour, a TMP
  text's font, size and style, and a `RectTransform`'s layout, re-applies them to objects
  vanilla creates later (grid slots on resize, recipe entries on every refresh, tooltips),
  and restores everything exactly when the module is switched off or faults, like the
  veil and the nudge. Vanilla's per-frame state colours (red for missing materials, grey
  for not craftable, durability colours) are kept: they carry information.
- **We add our own frames and read-only panels around them**: window frame and headers in
  the gold language, the tab bar, a weight bar, total armour, and an item details panel
  mirrored from the hovered item (vanilla's tooltip data). The tab bar calls vanilla's
  public entry points only (`OnOpenSkills`, `OnOpenTexts`, `OnOpenTrophies`,
  `OnOpenAchievements`, `OnTabCraftPressed`, `OnTabUpgradePressed`).
- **Where the concept asks for something vanilla does not have**, it waits for its phase:
  48 slots, quick-use and action slots (gameplay, F8); amulet/ring/gloves/boots slots
  (adapters, F7); the 3D character figure (research). A dedicated equipment panel is shown
  **read-only** first (the equipped items and their durability); unequipping from it would
  call `Player.UseItem`, the same entry vanilla's right-click uses, in a later step.
- Recipe **search and category filters** do not exist in vanilla; hiding recipe entries
  would change vanilla's selection indexes. Left out of F4 unless Diego wants them; they
  would need their own decision.

## 4. Steps (one preview and one pt-BR script each)

| Step | Delivers | Script focus |
|---|---|---|
| F4.1 | `VanillaSkin` (with Core tests of the record/restore bookkeeping), window regions in the registry, the window shell: frame, headers, fonts, tab bar, dragged item, split dialog, item tooltip | open/close by every key; nothing looks vanilla; fault + retry returns the vanilla window intact |
| F4.2 | Inventory: slot frames, player info (name, armour, weight bar), equipment panel (read-only), item details panel; containers: chest, cart, ship, tombstone, Take all / Stack all | **item-safety matrix**: move, swap, stack, split, drop, equip swap, container in/out, take all, stack all, death and tombstone — every count checked before and after |
| F4.3 | Crafting at the workbench, forge, cauldron, stonecutter, artisan table, black forge, galdr table, food preparation table, mead ketill and the no-station list: recipe entries, station header, requirements with have/need, quality, variant, craft progress, upgrade tab, repair | each station opens in our look; crafting consumes exactly what vanilla would; upgrade and repair |
| F4.4 | Skills, texts, trophies and achievements dialogs | all four open from the tab bar and from vanilla's buttons |
| F4.5 | Build piece browser (hammer, hoe, cultivator): categories and piece grid | select, rotate, place, cancel; requirements shown |

The modpack performance check (moved from F3) runs with F4.1.

## 5. Safety

- No new write path to any inventory: the banned-API scan keeps passing without a new
  allow-list entry; the only calls into the game are the public entry points listed above.
- Skinned objects keep their raycast targets and navigation; the script checks that every
  button and slot still reacts, with mouse and gamepad.
- Other mods' additions under these windows keep working (we restyle, we do not re-parent);
  anything they add stays vanilla-looking until F7.

## 6. Questions for Diego

1. Approve the approach (D-025: dress vanilla, keep all behaviour vanilla)?
2. Tab bar: only the screens that exist in this phase (Inventário, Criação when at a
   station, Habilidades, Textos, Troféus, Conquistas), with Mapa and Configurações added in
   F6/F5?
3. Equipment panel read-only in F4.2, unequip-by-click later?
4. Recipe search and category filters: out of F4?
