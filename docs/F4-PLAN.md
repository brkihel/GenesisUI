# F4 — Inventory and crafting windows: plan

Status: **approved in its revised form; F4.0 in progress** (2026-09-28). Branch `f4-windows`.

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

## 3. Target and approach (revised after Diego's answers, 2026-09-28)

**Target:** ConceptArt (9) is practically the final inventory: top tab bar (Inventário,
Habilidades, Mapa, Criação, Conquistas, Configurações, with Q/E), the inventory grid with a
filter and view buttons, the equipment panel beside it (a 3D character in the middle if the
research works), item details on the right, weight bar, footer with key hints.
ConceptArt (12) is the crafting target, with search and category filters. SeneaL UI shows the
same kind of layout, built as its own UI (studied by screenshots only; nothing reused).

**Approach: rearrange and dress vanilla, keep its behaviour** (proposed D-025). Diego
rejected "only restyle in place" because it would not reach the concept. The layout freedom
comes from moving vanilla's panels, not from rewriting them:

- **Vanilla keeps every item operation.** Clicks, drags, splits, equip swaps, container
  transfers, crafting, upgrade and repair stay 100 % vanilla code (§2): the place where item
  loss and duplication happen is never rewritten, and mods that patch those classes keep
  working.
- **Free layout.** `InventoryGui`'s panels (`m_player`, `m_container`, `m_crafting`,
  `m_info`, the dialogs) and the grids are re-anchored, resized and re-spaced into our window
  (`RectTransform` and the grid's public `m_elementSpace`), reversibly, through a new
  Foundation piece **`VanillaSkin`**: it records and replaces layout, an `Image`'s sprite,
  type and colour, and a TMP text's font, size and style; re-applies them to objects vanilla
  creates later (grid slots on resize, recipe entries on every refresh, tooltips); and
  restores everything exactly when the module is switched off or faults. Vanilla's state
  colours (missing materials, not craftable, durability) are kept.
- **Our own panels for what vanilla has not**, calling only vanilla entry points:
  - the **tab bar**, all six tabs from the start (Diego): Inventário, Habilidades
    (`OnOpenSkills`), Mapa (opens vanilla's large map until F6), Criação (the crafting
    panel; Craft/Upgrade through `OnTabCraftPressed` / `OnTabUpgradePressed`), Conquistas
    (`OnOpenAchievements`), Configurações (vanilla settings until F5), plus Textos and
    Troféus;
  - the **equipment panel**, clickable from F4.2 (Diego): slots for head, chest, legs,
    cape, utility, weapons, shield, ammo; clicking an equipped item unequips it through
    `Player.UseItem`, the same call vanilla's right-click makes; dropping an item on it
    equips it the same way;
  - **item details**, weight bar, total armour: read-only, from the hovered or selected item;
  - **recipe search and category filters** in F4.3 (Diego): vanilla rebuilds its recipe
    entries on every refresh; the filter hides non-matching entries and packs the rest,
    after each rebuild. Selection stays correct because vanilla selects by the clicked
    entry; gamepad up/down over hidden entries is handled in the same step. The search box
    holds a text-focus input lease so typing never triggers hotkeys.
- **Out of F4, by phase:** 48 slots, quick-use and action slots (they change the inventory:
  gameplay package, F8); amulet/ring/gloves/boots slots (adapters, F7). The **3D character**
  is a research spike in F4.2 (a second camera rendering the local player into a texture);
  if it is not reliable, the panel keeps the slots around an ornament instead.

## 3a. Art taken from the concept (proposed D-026)

Diego wants the concept's own ornaments (the chest card of ConceptArt (8), the minimap with
the biome name above and wind and day/time below, the bars, hotbar, stamina readout, window
frames) instead of our generated approximations. A feasibility test on the chest card worked:
the card's frame was cut out, the gold ornament connected to the border line was kept and
everything else (icon, texts, arrow) removed, the interior refilled with the panel's own dark
tone, and the result stretches as a 9-slice at any size without deforming the corners.

- **Pipeline:** `tools/art/extract.py` (to write) takes a crop box and a recipe per piece from
  a concept image in Diego's local folder, cleans it and writes the sprite into `art/out/`
  with its 9-slice border and content insets, like the generated sprites. The concept images
  themselves never enter the repository; the extracted, cleaned pieces do.
- **Limits:** the concept is 1672×941, so pieces are at roughly 1:1 for 1920×1080 and softer
  at 1440p/4K; pieces drawn over the scene (minimap ring over the map, bar frames over grass)
  need their background removed piece by piece; item and game icons in the concept are never
  used (game icons come from the game).
- **Rights confirmed (D-026):** the images are Diego's own (ChatGPT, edited by him in
  Photoshop). All twelve are upscaled to 3840×2160 first with Real-ESRGAN on the CPU
  (`tools/art/upscale.py`, output outside the repository), so pieces stay sharp at 4K.
- **Vital bars fill their frame's whole opening** (Diego): the liquid is clipped by the shape
  of the frame's inner opening (a mask sprite extracted with the frame) instead of a
  rectangle, so it reaches into the arch and the point and leaves no black corners; burn,
  veins and embers live inside the same shape.
- **HUD first:** the HUD pieces are redone from the concept (chest card, minimap layout,
  bars, hotbar, stamina readout) as step F4.0, then the window frames come from ConceptArt
  (7), (9), (12) and (1).

## 4. Steps (one preview and one pt-BR script each)

| Step | Delivers | Script focus |
|---|---|---|
| F4.0 | `extract.py`; HUD pieces from the concept: interaction card, minimap (biome name above, wind and day/time below), vital bars, hotbar, food slots, stamina readout | the HUD against the concept, side by side |
| F4.1 | `VanillaSkin` (Core tests of the record/restore bookkeeping), window regions, the window shell from the concept: frame, header, six tabs + Textos/Troféus, footer hints, dragged item, split dialog, tooltip | open/close by every key; every tab; fault + retry returns the vanilla window intact; modpack performance check |
| F4.2 | Inventory in the concept layout: grid, filter/view buttons, weight bar, equipment panel (click to unequip, drop to equip), item details; containers (chest, cart, ship, tombstone); 3D character spike | **item-safety matrix**: move, swap, stack, split, drop, equip and unequip from both panels, container in/out, take all, stack all, death and tombstone, counts checked before and after |
| F4.3 | Crafting in the concept layout at every station (workbench, forge, cauldron, stonecutter, artisan table, black forge, galdr table, food preparation, mead ketill, no station), search and category filters, requirements have/need, quality, variant, progress, upgrade, repair | each station; crafting consumes exactly what vanilla would; search/filters never craft the wrong recipe |
| F4.4 | Skills, texts, trophies, achievements | all open from the tabs and from vanilla's buttons |
| F4.5 | Build piece browser (hammer, hoe, cultivator) | select, rotate, place, cancel |

## 5. Safety

- No new write path to any inventory. The only calls into the game are vanilla's public entry
  points named above; `Player.UseItem` is the one vanilla's own right-click uses. The
  banned-API scan keeps passing without a new allow-list entry.
- Moved and restyled objects keep their raycast targets and navigation; the scripts check
  every button and slot with mouse and gamepad.
- Other mods' additions under these windows keep working (we move and restyle, never
  re-parent or destroy); they stay vanilla-looking until F7.

## 6. Answers so far (2026-09-28)

1. Approach: "restyle only" rejected as not reaching the concept → revised to §3 (rearrange
   and dress, behaviour stays vanilla). Waiting for Diego's OK on the revision.
2. Tab bar: **all tabs from the start**.
3. Equipment panel: **clickable already in F4.2**.
4. Recipe search and filters: **in F4**.
5. Concept-derived art: wanted; images are Diego's own (D-026); upscale to 4K first.
6. Vital bars: the liquid fills the whole opening of the frame, effects inside it.
