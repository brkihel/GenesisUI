# Inventory gameplay — requirements and design (F4)

Status: inventory layout shipped in approved Release `1.1.2`, with verified resize prerequisites,
position snapshots/journals and grow-before-move rollback under D-040. R-067 records overall
approval; individual save/reentry/modpack scenarios are not all reported. D-042 progress only
reads native actions; the existing successful-equip slot journal remains unchanged.
Independent foreign equipment/container adapters below remain planned; see CAPABILITIES.md.
Decision: D-028 (same plugin, during F4), D-030 (patches). Plan: F4-PLAN §7.
The first proposal (every special slot kept in rows vanilla shows, driven by "never lose an item
on uninstall") was **rejected**: the concept's layout is the design, and losing items when the
mod is removed is an accepted risk (GenesisUI is part of the server's modpack; not uninstalled).

## 1. Requirements (Diego, 2026-09-29)

1. **Inventory size set by the admin**: 32, 40 or 48 slots (4–6 rows of 8), the 8 hotbar slots
   included, with a scroll bar on the inventory slots only. "Admin" = the server's config, or
   the player's own config in single player / local worlds.
2. **Equipment panel** as in ConceptArt (9): every worn item lives there; weapons and tools stay
   on the hotbar. Clicking a cape in the inventory equips it **and moves it into the cape slot**.
   No weight reduction, no hotkeys. Slots:
   - vanilla: Head, Chest, Legs, Cape, Belt (utility item), Trinket;
   - modpack, each shown only when its mod is present **and** the admin keeps it on:
     Backpack/Quiver (one shared slot: Backpacks + BowsBeforeHoes), Lantern (HipLantern),
     Amulet and Ring (Jewelcrafting), MagicRevamp's slots.
3. **Quick-use slots** (consumo rápido): food, meads, potions. **Utility slots**: ammunition,
   magic items, shields, tools, weapons — never items that have their own equipment slot
   (armour, capes, backpacks). Rings and amulets are accepted for quick swapping: equipping one
   from a utility slot sends it to its equipment slot and puts the one that was worn in its place.
   The admin sets **0–4 of each** (0 = off).
4. **HUD**: quick-use and utility slots appear smaller and **without frames above the hotbar**, as
   a second, lighter row joined to its top, each with its hotkey. Defaults: quick-use
   **Alt+1…Alt+4**, utility **Alt+Z, Alt+X, Alt+C, Alt+V** (Z/X/C/V alone are vanilla's sit, walk
   and auto-pickup); all configurable.
5. **Sort** and a **category filter that dims** items in place.

## 2. What vanilla 1.0.16 does (study, proven in code)

1. `Player.SetInventorySize(rows)` clamps to 0–9 rows, stores them in the character (`invrows`),
   resizes `InventoryGui`'s player panel, then `Humanoid.DropInvalidItems()` **drops on the
   ground** every item outside width × height. `Player.OnSpawned` re-applies `invrows` each spawn.
2. `Inventory.Load` keeps items saved outside the height (`skipValidPositionCheck`), but two items
   on one position lose the second silently.
3. `AddItem(item)`, `CanAddItem`, `GetEmptySlots`, `HaveEmptySlot` look at all `width × height`
   positions (`FindEmptySlot` walks every row).
4. Death: `MoveInventoryToGrave` copies width/height and positions of every non-equipped item;
   `MoveAll` (take all) tries each item's original position first.
5. Vanilla equips one item per kind (one utility item); rings/amulets from Jewelcrafting and other
   modded slot items rely on a slot mod to be worn together.

Current slot mods (ExtraSlots 1.2.12 — on GenesisHeim today —, AzuEPI 2.5.1, InventorySlots
1.5.13; README/CHANGELOG only) all keep special slots as extra rows of the player's inventory
that vanilla does not show, and all spent years on tombstones, sorting and other inventory mods.

## 3. Technical design

### 3.1 Where the items live

The player's own inventory, width 8:

| Rows | Content | Drawn by GenesisUI |
|---|---|---|
| 0 … N−1 | ordinary slots, N = 4–6 (admin), row 0 = hotbar | inventory grid with scroll |
| N | quick-use 0–3 (x 0–3), utility 0–3 (x 4–7) | inventory panel sections + HUD row |
| N+1, N+2 | equipment cells (fixed x per slot kind) | equipment panel |

The special rows are real inventory positions, so saving, loading, graves and `MoveAll` keep them
with no extra storage. A position whose slot is switched off by the admin is never offered; an
item left there is moved to a free ordinary slot at spawn (or stays until one frees up).

### 3.2 Patches (each one a named decision in D-030, guarded, contract-tested)

1. `Player.SetInventorySize` — void prefix raising the requested rows to N + special rows, so
   vanilla's own resize and `DropInvalidItems` never drop a special item.
2. `Inventory.FindEmptySlot` (postfix), `CanAddItem` / `GetEmptySlots` / `HaveEmptySlot`
   (postfix on `__result`): automatic placement and free-space counts only see ordinary rows.
3. `InventoryGui.OnSelectedItem` — the one skipping prefix: it refuses a drop into a special cell
   when the item is not allowed there (message in pt-BR), and passes every other case to vanilla
   untouched. Vanilla then does the move, swap, split and stack itself.
4. `Humanoid.EquipItem` / `UnequipItem` (postfix, local player only): after vanilla equips a
   worn item, swap it into its equipment cell (the item previously there goes to the equipping
   item's old position — the ring/amulet swap); after an unequip from a cell, move the item to a
   free ordinary slot, or leave it if none.
5. Modded slots (Backpacks/Quiver, Lantern, Amulet, Ring, MagicRevamp): wearing them together with
   vanilla's utility item needs the same kind of equip hooks slot mods use. Each is studied against
   the mod's own DLL (members only) before its cell is switched on; until then the cell stays off.

### 3.3 The windows

`InventoryGui`'s grid, weight, and panels are moved and dressed by `VanillaSkin` into the concept
layout; special-row cells of vanilla's grid are placed into the equipment, quick-use and utility
panels, so drag and drop stay vanilla's own. The HUD row above the hotbar mirrors quick-use and
utility cells (icon, amount, hotkey), like the hotbar module.

### 3.4 Settings

`[Inventory]` (server-synced with ServerSync when a server has the mod; local otherwise): Rows
(4–6), QuickSlots (0–4), UtilitySlots (0–4), one toggle per modded equipment slot, hotkeys
(client-side), `[Modules] Inventory` master toggle. The module refuses to start (log + F8) next to
ExtraSlots, AzuEPI, InventorySlots, EquipmentAndQuickSlots or ComfyQuickSlots.

## 4. Steps

1. **F4.2a** Storage rows, admin rows 4–6, placement patches, inventory grid with scroll in the
   concept layout, weight bar, filter (dim), item details panel.
2. **F4.2b** Equipment panel (vanilla slots), equip/unequip moves, drop rules.
3. **F4.2c** Quick-use and utility slots (panel + HUD row + hotkeys), ring/amulet swap, sort.
4. **F4.2d** Modded equipment slots, one mod at a time.
5. Migration from ExtraSlots for GenesisHeim: later, at the switch-over (Diego).

## 5. Item-safety matrix (test script for each step)

Counts before and after: pickup with full inventory; crafting with full inventory; drag into
every special kind (valid and invalid); split into special cells; equip/unequip every worn kind
from grid, panel and hotkey; ring/amulet swap; quick-use and utility hotkeys; sort; die with
items in every row and take the grave back (all and one by one); stack all / take all at a chest;
admin 4→6→4 with rows full; fault injection mid-drag.
