# F4 — Inventory and crafting windows: plan

Status: **F4 shipped** in 1.0.0 and stabilized in approved Release 1.1.2 (R-067).
This document preserves the 2026-09-28/29 plan and original `f4-windows` branch context.
Revision 3 (§7) supersedes §3–§5 where they differ; D-032 supersedes moving/skinning vanilla
slots. Current capabilities and remaining adapters are in [CAPABILITIES](CAPABILITIES.md).

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

> **Superseded on 2026-09-29 by D-032:** GenesisUI draws its own window on the concept's design
> board; vanilla stays the hidden engine. The text below records the earlier approach.

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

## 3a. Art taken from the concept (D-026)

Diego wants the concept's own ornaments (the chest card of ConceptArt (8), the minimap with
the biome name above and wind and day/time below, the bars, hotbar, stamina readout, window
frames) instead of our generated approximations. A feasibility test on the chest card worked:
the card's frame was cut out, the gold ornament connected to the border line was kept and
everything else (icon, texts, arrow) removed, the interior refilled with the panel's own dark
tone, and the result stretches as a 9-slice at any size without deforming the corners.

- **Pipeline:** `tools/art/extract.py` takes a crop box and a recipe per piece from
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
- **Refinement review:** `tools/art/refine_concept.py` builds symmetric, color-normalized
  candidates and a standalone before/after page in `dist/art-review/`. It leaves shipped
  sprites intact pending visual review. See `docs/F4-ART-REVIEW.md`.
- **Vital bars fill their frame's whole opening** (Diego): the liquid is clipped by the shape
  of the frame's inner opening (a mask sprite extracted with the frame) instead of a
  rectangle, so it reaches into the arch and the point and leaves no black corners; burn,
  veins and embers live inside the same shape.
- **Superseded for the HUD by Diego's isolated sheets (D-027):** `tools/art/sheets.py` cuts
  every HUD piece from `~/GenesisUI-Concept/GenesisUI-textures/`; F4.0 ships them
  (0.6.0-preview.1, R-042). The window frames of F4.1+ come from the same sheets.
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

## 7. Revision 3 — window sheets, gameplay inside F4 (2026-09-28, after F4.0)

Diego supplied 13 window sheets (`~/GenesisUI-Concept/windows-textures-genesisui/`) drawn for
ConceptArt (9) inventory and (12) crafting, all without background (the shared stone material
goes behind, with its own opacity), and answered three questions.

### 7.1 What changed

- **Gameplay moves into F4** (D-028). Diego wants the inventory complete in this phase: a slot
  count set by the server admin up to **48** (the 8 hotbar slots included), **4 quick-use**
  slots (food, a potion), **4 utility** slots (anything that is not equipment), an
  **equipment panel where every equipped item lives** (weapons and tools stay on the hotbar)
  and **sort**. They are built **inside the GenesisUI plugin** as an isolated `Gameplay/`
  module with its own toggle (Diego, 2026-09-29: one plugin, non-negotiable). Switched off,
  the UI works with vanilla's 32 slots.
- **Filter = dim** (Diego): items outside the chosen category are dimmed in place; nothing
  moves, drag and drop stays exact. Visual, UI plugin.
- **One gold for all art** (D-029): every sheet piece is recoloured by `tools/art/sheets.py`
  from a ramp taken from Diego's reference (`elements-color.jpg`): darker, discreet, one tone
  for HUD and windows; shape, alpha, symmetry and highlights untouched. Pieces with a colour of
  their own (green equipped, grey disabled, red) are exempt. Diego's hand retouching is no
  longer needed; he approves the tone. Applied to the HUD in 0.6.0-preview.4.
- **No ready-made grids** (Diego): the inventory grid is built from single slot sprites, sized
  from the configured count, with a scroll bar on the inventory slots only. Prefer the finest,
  least busy pieces of the sheets when composing.
- **Configurações** is GenesisUI's own settings page (our config, in pt-BR, live), not
  vanilla's settings; vanilla settings stay on the Esc menu until F5.
- **Item details window** replaces the tooltip: the selected (or hovered) item's details in
  their own panel, as in ConceptArt (9).

### 7.2 Gameplay module: how it stays safe

Research first (step F4.G0), clean room, deep (Diego): study current post-1.0 extra-slot mods
from the Hexium store (behaviour, docs, changelogs: what broke and how they fixed it; never their
code) and vanilla's `Inventory`, `Player`, `Humanoid` save/load.
Open questions the spike must answer before any item code:

1. **Where extra slots live.** Proposal: extra rows of the player's own inventory (vanilla
   saves every item's grid position in the character file), so nothing new is saved and the
   server needs no storage. Equipment, quick and utility slots are reserved positions in rows
   the vanilla grid never shows.
2. **Removing the mod or joining a server with a smaller limit must never lose items.** What
   vanilla's `Inventory.Load` does with a position outside its grid is the first thing to
   prove; the package must repack overflow into free slots or drop nothing silently.
3. **Admin authority.** The slot count comes from the server (ServerSync, as in the house
   standard; Jötunn does not ship it). Clients cannot raise it. AzuAntiCheat whitelist entry.
4. **Moves go through vanilla** (`Inventory.MoveItemToThis`, `Humanoid.EquipItem`,
   `UnequipItem`) wherever vanilla has an entry point; the package's own writes are few,
   named and covered by the item-safety matrix.
5. **Compatibility**: detect other extra-slot mods (and SeneaL UI) and keep the gameplay
   module off when one is present, with a clear log line, instead of fighting over rows.

### 7.3 Steps (each: one preview, one pt-BR script)

| Step | Delivers |
|---|---|
| F4.1 | **Shell done in 0.7.0-preview.1** (bars, tabs, hints; `VanillaSkin` moves to F4.2). Window art cut from the 13 sheets (toned, `_shape`, ornaments, states); `VanillaSkin`; the window shell: top bar with the six tabs (Q/E), bottom key-hint bar, frame, open/close by every key, fault → vanilla window intact |
| F4.2 | Inventory in the ConceptArt (9) layout with **vanilla's 32 slots**: grid from single slots, filter (dim), weight bar, item details panel, containers; equipment panel (click to unequip, drop to equip through vanilla) |
| F4.G0 | Gameplay research spike (§7.2) → `docs/GAMEPLAY.md` for Diego's approval; no item code before it |
| F4.G1 | Gameplay module (same plugin): admin slot count up to 48 with scroll, equipment slots, 4 quick-use, 4 utility, sort; item-safety matrix incl. uninstall and smaller-limit servers |
| F4.3 | Crafting in the ConceptArt (12) layout at every station, search and category filters, upgrade, repair |
| F4.4 | Skills, texts, trophies, achievements; **Configurações** (our settings page) |
| F4.5 | Build piece browser (hammer, hoe, cultivator) |

### 7.4 Decided (2026-09-28)

1. Gameplay: **with F4, in the same plugin** (Diego, non-negotiable, D-028).
2. Filter: **dim in place**.
3. Colour: **automatic normalisation of every piece** (D-029).

