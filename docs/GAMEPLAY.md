# Inventory gameplay — study and design (F4.G0)

Status: **study done 2026-09-29; design proposed, waiting for Diego's OK before any item code.**
Decision: D-028 (same plugin, built during F4). Plan: F4-PLAN §7.

What Diego asked for: an inventory whose size the **server admin** sets, up to **48 slots**
(the 8 hotbar slots included), with a scroll bar on the inventory slots only; **4 quick-use**
slots (food, a potion); **4 utility** slots (anything that is not equipment, as SeneaL UI
does); an **equipment panel where every equipped item lives** (weapons and tools stay on the
hotbar); **sort**; a category **filter that dims** in place.

## 1. Sources

- **Vanilla, game 1.0.16** (`ref/assembly_valheim.dll`, one type at a time with a heap cap,
  read only): `Inventory`, `Player`, `Humanoid`, `InventoryGui`.
- **Current inventory mods on the Hexium store** (catalog of 2026-09-24, packages downloaded
  and their README/CHANGELOG read; no code read or reused):
  - `shudnal-ExtraSlots` 1.2.12 (2026-09-19) — **runs on GenesisHeim today**, with
    `shudnal-ExtraSlotsCustomSlots`.
  - `Azumatt-AzuExtendedPlayerInventory` 2.5.1 (2026-09-17), 15 370 downloads; ~1 900 mods
    declare it as a dependency (its slot API).
  - `sighsorry-InventorySlots` 1.5.13 (2026-09-24).
- **GenesisHeim modpack** (`dados/mods.lock.current.json`): items that ask for their own slot
  come from `Smoothbrain-Backpacks`, `Smoothbrain-Jewelcrafting` (neck, ring),
  `Azumatt-BowsBeforeHoes` (quiver), `blacks7ar-MagicRevamp`; ExtraSlotsCustomSlots gives them
  slots today.

## 2. What vanilla 1.0.16 does (proven in code)

1. **Native inventory rows.** `Player.SetInventorySize(rows)` clamps to 0–9 rows, sets the
   inventory height, stores it in the character as the unique key **`invrows`**, and asks
   `InventoryGui.SetInventorySize(rows)` to grow the player panel by one grid row per row.
   On every spawn (`Player.OnSpawned`) the game reads `invrows` back and applies it (default 4).
   → A size set this way **survives uninstalling GenesisUI**: vanilla keeps showing all rows.
2. **Shrinking drops items on the ground.** `SetInventorySize` ends with
   `Humanoid.DropInvalidItems()`: every item whose grid position is outside the new width/height
   is **dropped at the player's feet** (not deleted; quest items cannot be dropped).
3. **Loading keeps out-of-grid items.** `Inventory.Load` adds saved items with
   `skipValidPositionCheck = true`: an item saved in a row beyond the current height is kept in
   the list (invisible) — until the next `SetInventorySize` drops it (point 2).
4. **Two items saved on the same slot: the second is silently lost at load.** The internal
   `AddItem(item, amount, x, y)` returns false when the slot holds a different item, and `Load`
   ignores the result. Any design must never let two items share a position.
5. **Automatic placement scans every row.** `AddItem(item)` (pickups, crafting results, take
   all overflow) uses `FindEmptySlot`, which walks all `m_height` rows; `CanAddItem` and
   `GetEmptySlots` count `width × height`. Rows we reserve for special slots would be filled by
   ordinary pickups unless those three are taught to skip them.
6. **Death.** `MoveInventoryToGrave` copies width and **height** into the tombstone and moves
   every non-equipped, non-quest item with its grid position (the player unequips first).
   Taking all back (`MoveAll`) tries each item's **original position first**, then any free slot.
7. **Stack all / take all** use `AddItem(item)` (point 5).

## 3. What the current mods learned the hard way (their changelogs)

- **Item loss when the mod is removed** was the central problem for years. ExtraSlots keeps
  its extra items in rows the game does not show, so "if you run the game without the mod the
  items will be lost"; 1.2.0 (2026) added deferred items, backups and recovery, and 1.2.1 had
  to follow "the native inventory size" once 1.0 introduced it. AzuEPI and InventorySlots tell
  users to **empty the extra slots before uninstalling**.
- **Tombstones**: repeated fixes for items in extra slots lost or duplicated on death,
  dungeons and container ownership changes (ExtraSlots 1.0.30, 1.0.36, 1.1.17, 1.2.0, 1.2.1).
- **Other inventory mods fight over the same rows**: all three declare each other
  incompatible. Sorting, quick stack, auto store, EpicLoot sacrifice, crafting that destroys
  items (Jewelcrafting) and ServerCharacters each needed a compatibility fix.
- **Custom equipment slots are an ecosystem**: AzuEPI's API is used by ~1 900 mods;
  ExtraSlots has its own. Without one of them, modded items that expect a slot (backpacks,
  rings, necklaces, quivers) have no dedicated place.

## 4. Proposed design

### 4.1 Everything lives in vanilla rows that vanilla can see

All slots — ordinary inventory, quick-use, utility and equipment — are **positions of the
player's own inventory**, and the inventory height set through vanilla's own
`SetInventorySize` **includes the reserved rows**:

| Rows | Content | Shown by GenesisUI as |
|---|---|---|
| 0 … N−1 | ordinary inventory, N = admin setting (4, 5 or 6 → 32/40/48 slots); row 0 is the hotbar | the scrolling grid |
| N | 4 quick-use + 4 utility | the two rows under the grid |
| N+1 | equipment (head, chest, legs, cape, utility item, trinket, and modded slots) | the equipment panel |

With N = 6 that is 8 rows (vanilla allows 9). Consequences:

- **Uninstalling GenesisUI never loses an item**: vanilla keeps `invrows` and simply shows every
  row, special ones included, as ordinary slots. Equipped items stay equipped (the flag is on
  the item, not on the slot).
- **Death and tombstones need no special code**: the grave copies the height and positions;
  taking all puts each item back on its own position first (§2.6).
- **Nothing new is saved and nothing on the server stores items.** ServerCharacters keeps
  working as it saves the normal profile.

### 4.2 Vanilla keeps every item operation

The reserved cells are vanilla `InventoryGrid` elements, moved on screen into the quick,
utility and equipment panels by `VanillaSkin` (reversibly). Drag, drop, split, swap and
right-click stay vanilla's own handlers. GenesisUI adds only:

1. **Placement rules** (Harmony, guarded, postfix/prefix per PATCH-POLICY): a reserved cell
   accepts only its kind (quick: food and potions; utility: anything that is not equipment;
   equipment cell: its item type), and `FindEmptySlot`, `CanAddItem` and `GetEmptySlots` skip
   reserved rows so pickups and crafting never land there (§2.5).
2. **Equip moves to the panel**: after vanilla equips an armour piece (any source: click,
   hotkey, auto-equip), the item is swapped into its equipment cell; unequipping moves it back to
   a free ordinary slot, and if there is none it stays where it is (never dropped).
3. **Quick-use keys** call vanilla's `Player.UseItem` for the item in that cell, as the hotbar
   does.
4. **Sort** reorders the ordinary rows only (never the hotbar row unless asked, never reserved
   rows), merging stacks through vanilla, in one pass that is checked to keep every item.

### 4.3 Admin authority and changing the size

- The admin sets **ordinary rows (4–6)** in the server's config, synchronised to clients with
  **ServerSync** (house standard). A client cannot raise it; without the server's value the
  module uses 4 (vanilla) and shows no extra rows.
- **Growing** moves the special rows down first (so no two items ever share a position, §2.4),
  then calls `SetInventorySize`.
- **Shrinking never drops anything**: items in rows that would disappear are first repacked into
  free ordinary slots; if they do not fit, the size is kept until they do and the player is told
  (pt-BR message). Vanilla's `DropInvalidItems` is only ever reached with nothing left outside.
- Every size change, move and repack is logged with a before/after item count; a mismatch trips
  the module and returns everything to vanilla.

### 4.4 Isolation inside the plugin

- `src/GenesisUI/Gameplay/`, own `[Modules] Inventory` toggle (off → vanilla 32 slots, special
  rows shown as ordinary rows), own guard owner, diagnostics lines (rows, reserved cells,
  last repack, counts).
- The banned-API test keeps its rule for the UI modules; the gameplay module gets a short,
  named allow-list (grid position swaps, `SetInventorySize`) justified by this document.
- **Refuses to start** (log + F8 line, stays vanilla) when ExtraSlots, AzuEPI, InventorySlots,
  EquipmentAndQuickSlots or ComfyQuickSlots is loaded — except for the one-time migration below.
- Network: the plugin keeps `NotEnforced` for the visual part; the server-synced settings are
  reviewed with the house standard when the module lands. AzuAntiCheat whitelist as usual.

## 5. Open questions (need answers before F4.G1 code)

1. **Migration from ExtraSlots on GenesisHeim.** Players have items in ExtraSlots today. Its
   storage layout must be read from a real character file (Diego's own `kihel.fch`, a copy;
   never the production server) before the switch-over; the migration then moves every item
   into GenesisUI's rows once, with counts checked, and never runs while ExtraSlots is loaded.
   Timing: with F9 (switch-over), but designed now.
2. **Modded slot items** (Backpacks, Jewelcrafting neck/ring, BowsBeforeHoes quiver,
   MagicRevamp). Options: (a) GenesisUI offers AzuEPI's slot API shape so those mods register
   their slots with us, (b) a fixed list of extra equipment cells per known mod, (c) they go to
   utility cells. Needs a look at how each behaves with no slot mod present.
3. **SeneaL UI's utility slots**: Diego's reference for their exact rules (weight? hotkeys?).
4. **Hotbar row** in sort: excluded by default?

## 6. Item-safety matrix (the F4.G1 script)

Every row checked with item counts before and after: pick up with full inventory; craft with
full inventory; drag into each reserved kind (valid and invalid); split into reserved cells;
equip/unequip every armour type from grid, panel, hotkey; quick-use keys; sort with full
stacks; die with items in every row and take the grave back (all, one by one); stack all / take
all at a chest; admin grows 4→6 and shrinks 6→4 with rows full; uninstall GenesisUI and load
the character in vanilla; reinstall; fault injection in the module mid-drag.
