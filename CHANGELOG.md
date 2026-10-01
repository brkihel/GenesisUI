# Changelog

## 1.0.1 — A page for players

No change in the game. The mod page now tells players what GenesisUI gives them, with screenshots,
in English and Portuguese, and points to the GenesisMods Discord for help (F8 report → #valheim-bugs).

## 1.0.0 — GenesisUI 1.0

The first release. GenesisUI replaces Valheim's interface with its own, in one fine gold-metal
language, while vanilla keeps running hidden underneath (other mods' items, pins and messages still
work):

- **HUD:** vital bars with living liquid and burn, food, hotbar, status effects and guardian power,
  round minimap, stamina readout, boss and creature plates, interaction card, notifications, key hints.
- **Windows** with tabs (Q/E): inventory with quick-use and utility slots, equipment and details;
  crafting and upgrading at every station; skills and character; achievements; GenesisUI settings.
- **Build menu** for hammer, hoe and cultivator, with a placement card.
- **Trader, small dialogs, framed map** with filters and GenesisUI's own markers, and a clean,
  blurred **Esc menu**.
- **Self-healing:** a part that fails closes its window, rebuilds itself and tells you what happened.

Release build: no diagnostics overlay and no watermark; F8 writes a support report.

## 0.8.0-preview.5 — Workbench tab, map leftovers, button feedback

### Added
- Every window button and tab answers the pointer: a soft gold light on hover, brighter while
  pressed, and a slight sink under the press (`Widgets/PressFeedback`); disabled buttons stay still.

### Fixed
- A workbench (E) opened the Achievements tab: since preview.4 the shell saw the window open in the
  frame E was pressed, and E is also the next-tab key. Tab keys are ignored in the opening frame.
- Vanilla's boss/death filters and a dark hint panel showed under the map's chrome: everything in
  vanilla's large map except the map, pins, names, markers and name input is now hidden.

## 0.8.0-preview.4 — Map clicks, no vanilla flash, E through the map, clean Esc shade

### Fixed
- The framed map took no clicks (drag, pin, remove, ping); only the wheel zoomed. The panel around
  it drew in front with a click blocker, and vanilla's map takes its clicks only through UI
  raycasts on its image. The map's panel no longer blocks.
- Vanilla's windows flashed behind GenesisUI's: modules now refresh in `LateUpdate`, after vanilla
  opened its window in its own `Update`, and the four vanilla inventory panels are hidden by one
  shared owner (`VanillaPanels`) instead of one per window, which let a tab switch (Q) undo the
  new window's hiding.
- Map → E showed vanilla's inventory and crafting with the Map tab lit: `Minimap.IsOpen()` stays
  true two frames after the map closes, and the shell took the new window for the map. The shell
  now checks the map is really on screen; going to the Map tab no longer lights Inventory for a frame.
- The Esc menu's left shade ended in a hard vertical line; it now fades out to nothing.

## 0.8.0-preview.3 — The Crafting fault, found in Diego's log

### Fixed
- The Crafting window faulted on every opening with craftable and non-craftable recipes mixed:
  it reordered a list inside a `foreach` over that list ("Collection was modified"). The order
  is now `Core/Collections/ListOrder.StablePartition`, index-based and tested.
- Crafting rows tried to add a second CanvasGroup (Unity warning per row); buttons now reuse it.
- The Skills tab rebuilt vanilla's texts list on every opening, and vanilla logs a long stats block
  each time; it now reads it at most once a minute.

## 0.8.0-preview.2 — Self-healing, fault popup, Esc menu, key hints, framed map, custom markers

### Added
- **Self-healing:** when a part faults, the vanilla window it drew over closes first (never
  vanilla's look in its place), the part is rebuilt in the same frame (HUD veils back before
  anything draws), up to three times per five minutes; a popup apologises, explains, shows the
  exact error with Copy, and a Report button kept disabled until the Discord system exists.
- **Esc menu** as Diego pictured it: the game blurred (new `GenesisUI/Blur` shader, dark veil as
  fallback, `[Theme] MenuBlur`), clean options on the left in the display font, eased hover.
- **Key hints** in GenesisUI's fonts and key caps, at half size (`[KeyHints] Scale`).
- **Map** inside a panel like every window (vanilla's map fitted into it), switches for visible
  to others and group markers, and **custom markers** (masmorra, minério, covil, base, portal,
  tesouro, comerciante, perigo) saved as vanilla point pins tagged in their name.

### Changed
- Build menu: pieces that cannot be built now fade as a whole with a greyed icon.

### Open
- The Crafting fault after using E through the map: waiting for Diego's log (the report did not
  reach the conversation); self-healing now rebuilds it and never leaves vanilla's window.

## 0.8.0-preview.1 — Every in-game window in GenesisUI (D-032)

All with the same rule: vanilla stays the engine (invisible, still deciding and acting), GenesisUI
draws the window and hands every press to vanilla's own buttons and methods.

### Added
- **Habilidades** (ConceptArt 1, real data): character (name, day, total skill ring, guardian
  power, attributes, vanilla's PvP switch), skills by group with gear bonus and progress, active
  effects, and the texts vanilla keeps (compendium, log, effects, stats).
- **Conquistas**: achievements in vanilla's order with the count, earned trophies, details.
- **Configurações** (ConceptArt 2): GenesisUI's options by category — switches, sliders, key
  capture — with an explanation, default and restore per option; server-locked inventory options
  are read-only.
- **Construção** (hammer, hoe, cultivator): vanilla's 1.0 build menu mirrored — lists, tags,
  favourites, recent, search, other mods' pieces — with piece details (materials, station) and a
  "Posicionar" card while placing.
- **Loja** of the traders: goods, details, coins, buy and sell (vanilla's own sell choice shown).
- **Diálogos**: split stack, style, rune stones and ravens, naming signs and portals.
- **Mapa** (ConceptArt 11): vanilla's map framed with title and day, biome under the cursor, pin
  filters, marker palette, visible-to-others, zoom and back-to-player; the window bars show over
  the map and lead to the other tabs.
- **Menu Esc**: vanilla's entries and confirmations in a GenesisUI card.
- `WindowModuleBase`, `ScrollArea`, the ring pieces: shared by the new windows.

### Not yet
- The main menu (character and world selection) and the game's own settings screen keep
  vanilla's look; they are the next step, studied separately.

## 0.7.0-preview.15 — Stations open the Crafting tab

### Changed
- Using a workbench, forge or any crafting station opens the windows on Criação instead of
  Inventário. The game keeps the station only while the window it opened is open, so Tab near a
  station and chests still open on Inventário (R-057 feedback: everything else worked).

## 0.7.0-preview.14 — Crafting tab in Diego's layout: details left, Criar | Aprimorar

### Changed
- The Crafting tab now has the item details on the left (like the inventory's): icon, name,
  type, description, the stat table — current → next for an upgrade, the gain in green —, the
  required materials with have/need, the reason when it cannot be made ("Falta: 3 Bronze",
  "Precisa de Forja nível 2"), Estilo and the craft button.
- The crafting panel on the right has the search, the category chips and two columns side by
  side, Criar and Aprimorar, with a separator. Recipes that can be made now come first; the
  others are dimmed with "Faltam materiais". Aprimorar lists your items that can go up a level
  ("Nível 2 → 3"); an empty column says why.
- Both columns follow vanilla's own rules; picking a row switches vanilla to that mode and
  presses vanilla's own row, so selection, requirement checks and crafting stay vanilla's.
- The hint bar shows the active tab's hints; Crafting teaches Shift + Criar (craft several,
  vanilla's own multi-craft) instead of a − 1 + stepper.

## 0.7.0-preview.13 — R-056 fixes: vanilla under Crafting, discovery notices, stat rows

### Fixed
- Switching Inventory → Crafting showed vanilla's inventory and crafting panels under our
  window: in the same frame the inventory window restored vanilla (a deferred Destroy of its
  CanvasGroup) and the crafting window re-hid it by reusing that dying group. VanillaSkin now
  removes groups it added immediately.
- Discovery messages ("Nova peça para construção", new recipes) now appear as GenesisUI notice
  cards (gold title, name, icon); vanilla still decides what is announced and when.
- The stat table no longer shows armour on weapons (the game's world-level bonus, which its own
  tooltip hides): armour only on armour, damage on weapons/tools/ammo, block on weapons/shields.

## 0.7.0-preview.12 — Crafting tab on ConceptArt (12) (F4.3, D-032)

### Added
- The Crafting tab is GenesisUI's own window (`win.crafting`), laid out from ConceptArt (12):
  the Criar/Aprimorar sub-tabs, the station name and level, Reparar when the station repairs,
  a recipe search, category chips, the recipe list (unavailable recipes dimmed, upgrade level
  shown), a details card (icon, name, type, description, stat table, required station level,
  vanilla's upgrade note), the required materials with have/need, the style button and Criar
  with its progress.
- Vanilla stays the engine: its recipe list (other mods' recipes included), requirement checks,
  crafting, upgrading, repairing and style dialog. Our row presses vanilla's row; Criar, the tabs,
  Reparar and Estilo press vanilla's buttons. A postfix on `UpdateRecipeList` (counts rebuilds,
  changes nothing) tells the window when to re-read the list.
- Typing in the search takes an input lease and keeps Tab/E from closing the window.
- `WindowParts` and `ItemStats`: the panel, label, button, rule and stat-table pieces shared by
  the inventory and crafting windows.

### Not yet
- The Construção panel on the right of ConceptArt (12) (the hammer's pieces) is the next step;
  the multi-craft stepper (− 1 +) is not wired yet.

### Verification
- Core and contract tests pass; a mock of the tab was compared with ConceptArt (12). In game:
  Diego's R-056.

## 0.7.0-preview.11 — R-054 fixes: picking up a single item, re-enable after a fault

### Fixed
- Picking up an item with a stack of 1 faulted the inventory window: the held-item icon read
  the length of a TMP text that is null until first assigned. The diagnostic HUD dump had the
  same pattern and is fixed too.
- After a fault and an F8 re-enable, the rebuilt window kept the previous build's layout key and
  chest size, so quick-use and action cells never bound (they refused every item) and the
  equipment cells stayed hidden. All window state now resets on build and teardown.

### Verification
- Core and contract tests pass; Diego's client test R-055 covers pick-up, special cells and the
  F8 re-enable path.

## 0.7.0-preview.10 — Thin-line metal, burn as light, darker theme (D-033)

### Changed
- Window panels and bars, cards, item slots and their selected/equipped states, key caps and
  rules are thin lines with small ornaments (`tools/art/metal.py`), lit as polished metal by
  the new `GenesisUI/Metal` shader: bronze-to-gold ramp, light from the top left, specular, and
  a slow glint that sweeps the screen every few seconds.
- The resource bars' loss is light (`GenesisUI/Burn`): a white-hot core at the level, a halo
  past the frame, a cooling tail and a few embers, replacing the bright band and rising grains.
- Darker theme: stone at 55 % brightness, older gold, softer text; the world dims behind the
  inventory window.
- The heavy sheet hotbar is retired: the hotbar is the minimal slot row again, in the new
  thin slots.

### Added
- `art/genesisui.shaders` (our shaders, built with Unity 6000.0.75f1, Valheim's version, by
  `tools/shaders/build.sh`). Without it, on an unsupported GPU, or with
  `[Theme] MetalShader = false`, frames use their pre-lit sprites and the bars their previous
  burn.

### Verification
- Core (141) and contract tests pass; both shaders compile without errors for Direct3D 11,
  Direct3D 12 and Vulkan. The look in game needs Diego's client test (R-054).

## 0.7.0-preview.9 — Own inventory window on the concept's layout (D-032)

### Changed
- The Inventory tab is GenesisUI's own window instead of vanilla's slots moved and reskinned
  (D-032, replaces D-025). It is laid out on ConceptArt (9)'s design board (1580 x 850,
  measured from the concept) and scaled uniformly to `[Windows] Width` of the screen, so its
  proportions are the concept's at any resolution: 8-column grid filling the inventory panel
  (four rows in view, scroll for the admin's fifth and sixth), quick-use and action slots
  (Z/X/C/V) under it, weight at the bottom; equipment in two columns with labels and total
  protection; details with icon, name, type, description and a stat table (weight,
  durability bar, quality, armour, damage, block, food, value); the chest panel over
  equipment and details with Take all / Stack.
- Every click, right click, drag and drop on our cells is handed to the vanilla grid's own
  callbacks; vanilla's panels are hidden, not moved. Vanilla's split dialog stays on top.
- Tab bar 90 and hint bar 62 units like the concept; the hint bar is laid out from measured
  widths (no more hints piled up in the middle) and shows R Organizar.

### Added
- Quick-use slots accept consumables; action slots accept weapons, shields, tools and ammo
  (Core `SlotRules`, enforced in the existing D-030 selection prefix, swaps included).
- Organizar (button and `[Windows] SortKey`, R): packs the rows below the hotbar by category,
  name and stack. Positions only; hotbar, quick-use, action and equipment never move.

### Verification
- Core (140) and contract (8) tests pass; the window mock (`tools/art/mock.py`) was compared
  with ConceptArt (9). Everything in game needs Diego's client test (R-053).

## 0.7.0-preview.8 — R-051 window lifecycle fixes

### Fixed
- The first inventory opening places each footer hint in a fixed cell. The hint row no
  longer depends on nested size fitters resolving their widths during Unity's first
  canvas layout pass.
- The inventory art and its controls follow the shell fade on close. Vanilla panel
  graphics remain hidden through the closing animation and are restored after the
  fade and a short settle period, avoiding the visible vanilla flash. A quick reopen
  reuses the dressed window.

### Verification
- Core and contract tests pass; first open, rapid reopen and close still need Diego's
  client check (R-052).

## 0.7.0-preview.7 — F4.2b equipment panel

### Added
- Six real Valheim inventory cells in the equipment panel: head, chest, legs, cape, belt
  and trinket. Vanilla still owns the items and their clicks. Equipping moves a worn item
  into its cell; replacing it swaps the old item into the source cell. Unequipping uses a
  free ordinary cell. Invalid drops into equipment cells are refused before vanilla acts.
- Already worn items move into the panel when an existing character loads. When the
  inventory window is closed, disabled or faulted, the extra rows stay accessible in the
  vanilla inventory for recovery.

### Fixed
- The inventory panel art now has an explicit lower canvas sorting order than the
  game's inventory grid, so its translucent textures cannot cover item slots.
- Horizontal stamina burn now pulses with a bright core, halo and moving sparks. An
  additive particle shader is used when Unity provides one, with an alpha glow fallback.

### Verification
- Core and contract tests pass. Slot draw order, the shader and all equipment actions
  need Diego's client test (R-051); no in-game pass is claimed.

## 0.7.0-preview.6 — R-049 inventory and HUD corrections

### Fixed
- The inventory's vanilla slots are realigned after Unity's canvas layout. The vanilla
  panel graphics are hidden individually, leaving the slot hierarchy active and clickable;
  slot hover and selection are cleared when the skin is restored.
- The actual vanilla slot button now wears the GenesisUI sprite, with its icon, stack count
  and durability on top. The status-effect icons use the same quiet slot as food buffs.
- The horizontal stamina bar has its own burn texture with a bright leading line and sparks,
  a shorter trail, and smoothed spending and recovery.

### Changed
- Windows use 75% of the screen by default. Existing 65% defaults migrate once; later
  changes to the size remain under the player's control.
- F8 reports now use `.log` instead of `.txt`; the text format and redaction stay the same.
- The HUD fade helper now compiles in Release as well as Preview.

### Verification
- Automated Core and contract tests pass. Slot interaction, hover restoration and the new
  burn still require Diego's in-game check (R-050).

## 0.7.0-preview.5 — R-048 inventory and HUD corrections

### Changed
- The inventory shell and panels occupy 65% of the screen width and height by default, on
  vanilla's inventory canvas. The tab and hint bars are shorter. Panel mid-edge ornaments are
  removed; each title has the full tab marker beneath it, and small marker knots divide tabs.
- Inventory cells now receive a separate thin frame below vanilla item icons. The filter opens
  a category list on a control layer in front of vanilla; selecting a category dims other items
  in place. All changed vanilla objects are restored when the module is turned off or faults.
- Diego's neutral replacement art is used for the hotbar, slot states, vertical bars, central
  stamina bar and minimap pieces. The new eight-cell hotbar is being tested; the approved thin
  slot version is preserved at git tag `hotbar-minimal-v1`. Equipped cells use gold, without green.
- The central stamina bar uses its larger drawn width, a shorter and fainter trail, and a burn
  edge. Hostile enemy health bars use the health liquid and fill more of the plate's channel.

### Verification
- Automated Core and contract tests pass; the new canvas, clicks and art still need an in-game
  check (R-049).

## 0.7.0-preview.4 — F4.2a (part 2): the inventory window

### Added
- **Inventory window** (`win.inventory`, `[Modules] InventoryWindow`): in the Inventário tab,
  ConceptArt (9)'s three panels — Inventário, Equipamento, Detalhes do item — in Diego's finest
  panel frame (its mid-edge diamonds as separate pieces), drawn behind vanilla's window. Vanilla's
  own slots are moved onto the panel's grid and wear the thin slot; every click, drag, split and
  equip is still vanilla's. Vanilla's panel frame, texts and tooltips are hidden (never destroyed)
  and come back exactly on another tab, on close or on a fault (`VanillaSkin`, D-025).
- Slots in use (`17/32`), a filter that **dims** other categories in place (click to cycle:
  Todos, Armas, Armaduras, Ferramentas, Consumíveis, Materiais, Munição, Diversos), the weight
  bar, total protection, and the hovered item's icon, name and vanilla text in the details panel.
- A chest, cart or ship opens vanilla's container panel over the equipment and details panels
  (dressing it comes next).

## 0.7.0-preview.3 — R-046 fixes

### Fixed
- Window bars drew their 9-slice ends at the wrong size on Jötunn's GUI canvas (reference pixels
  per unit 50): frames now compensate for the canvas they are on.
- With a window open, the HUD (bars, hotbar, food, minimap, vanilla's key hints) fades out, and
  back in when it closes.
- From the large map opened by the Mapa tab, Q/E go back to the tabs.

### Changed
- No "GENESISUI" title in the tab bar.
- Hotbar: eight single thin slots, no plate; selected and equipped are the thin slot lit in gold
  and in green. Food slots use the same slot.
- Minimap: the day/time plate is the same plate as the biome's, over the ring's top edge.
- Status tiles closer together.
- One darker, quieter gold for all sheet art.
- Defaults: `[Backgrounds] Default` 0.6, `[Minimap] OffsetY` 1, `[Status] OffsetY` 315.

## 0.7.0-preview.2 — F4.2a (part 1): the admin's inventory size

### Added
- **Inventory slots module** (`inv.slots`, `[Modules] Inventory`, docs/GAMEPLAY.md): the
  inventory has the size the admin sets in `[Inventory] Rows` — 4, 5 or 6 rows (32/40/48 slots,
  the hotbar included) — plus reserved rows for quick-use (`QuickSlots`, 0–4), utility
  (`UtilitySlots`, 0–4) and worn equipment. Their panels come in the next packages; for now the
  reserved rows are hidden and nothing can enter them.
- Changing the size moves items with a plan that keeps every item in its place when it can,
  sends the rest to free slots, never puts two items on one position, and refuses (keeping the
  old size, with a message) when things would not fit.
- Server-synced settings through **ServerSync** (D-031, merged into the DLL): on a server with
  GenesisUI, `[Inventory]` values marked [Servidor] come from the server
  (`LockConfiguration`); in single player they are the player's own.
- Patches (D-030): `Player.SetInventorySize` holds the reserved rows (vanilla never drops their
  items); pickups, crafting results and chest overflow only use ordinary slots.

## 0.7.0-preview.1 — F4.1: the window shell

### Added
- **Window shell** (`win.shell`, `[Modules] Windows`): with the inventory open, a top bar with
  GENESISUI and six tabs — Inventário, Habilidades, Mapa, Criação, Conquistas, Configurações —
  and a key-hint bar at the bottom, cut from Diego's window sheets (toned to the one gold,
  D-029; the bars' middles rebuilt from clean rail so nothing stretches). Tabs are clicked or
  cycled with `[Windows] PreviousTabKey` / `NextTabKey` (Q/E). Habilidades and Conquistas open
  vanilla's dialogs, Mapa opens vanilla's large map; Criação only marks the tab until F4.3;
  Configurações shows where GenesisUI's settings will live (F4.4).
- First Harmony patch: `InventoryTabKeyPatch`, a void prefix on `InventoryGui.Update` that
  clears "Use" for the frame when E switches the tab (vanilla would close the inventory).
  Other patches on that method are untouched; with the shell hidden it does nothing.
- `[Backgrounds] Windows` for the shell's panels.
- Art pipeline reads the window sheets (`win` pieces, `KW` scale), mirrors pieces, rebuilds a
  bar's middle from clean rail (`clean_middle`).

### Docs
- `docs/GAMEPLAY.md`: study of vanilla 1.0.16's inventory and of the current inventory mods,
  and the proposed design for 48 slots, quick/utility/equipment slots (waiting for Diego).

## 0.6.0-preview.4 — one darker gold

### Changed
- Every piece cut from Diego's sheets is recoloured to one darker, discreet gold taken from his
  colour reference (D-029); shapes, alpha and highlights are unchanged. The green equipped cell
  keeps its colour.

## 0.6.0-preview.3 — creature plate restored

### Fixed
- The creature plate above heads is back to the F3 plate (the generated boss plate drawn
  small); F4.0 had given it the sheet's slim bar, which belongs to the stamina readout only.
  The `enemy_plate` sprite is that generated plate, pixel-identical to 0.5.0.

## 0.6.0-preview.2 — R-042 adjustments; F4.0 approved

### Changed
- Eitr uses the stamina frame: only health is bigger.
- The horizontal stamina readout uses the right piece (sheet 3's slim bar with knot ends) and
  shows only the liquid.
- Hotbar: cells are measured from the art's own windows (items no longer sit to the left);
  the selected cell uses Diego's lit gold frame and an equipped item his green one.
- Defaults for 1920×1080 from Diego's layout: `[Boss] OffsetY 12`, `[Food] OffsetX 172`,
  `[Food] OffsetY 90`, `[Hotbar] KeyHintsLift 76`, `[Sprint] OffsetY 142` (existing config
  files keep their values).

### Removed
- The medallion under the health bar.

### Art pipeline
- `tools/art/sheets.py` exports every element at the sheet's resolution to
  `~/GenesisUI-Concept/GenesisUI-cuts/original/`; a retouched copy with the same name and size
  in `edited/` replaces the cut.

## 0.6.0-preview.1 — F4.0: Diego's textures on the whole HUD

### Changed
- Every HUD frame now comes from Diego's isolated texture sheets (D-027): the three vital
  bars (three sizes, as drawn), the stamina readout, the eight-cell hotbar, food slots,
  status tiles, the interaction card, notices, boss and creature plates, the minimap ring,
  its day/time and biome plates, and the vitals medallion. Nothing is stretched except plain
  straight rails; the card's side diamonds and the crest's top diamond are separate pieces.
- Health, stamina and eitr are filled with Diego's liquids, two layers drifting at different
  paces inside each frame's opening; the burn, embers, surface glint and low-health pulse stay.
  The stamina readout and the boss bar use the liquids too, and the readout shows the number.
- The selected hotbar cell glows from inside instead of drawing a second frame.

### Added
- The dark stone panel material behind every frame, clipped to the frame's silhouette, with
  its own opacity: `[Backgrounds] Default` and one override per panel (-1 = default). Only
  the material fades; frames, texts and icons stay.
- F8 panel: sprite and live background counts.

## 0.5.0-preview.4 — R-040 fixes; F3 approved

### Added
- Top-left notices stack up to three, newest on top; each fades while dropping after 4 s,
  and a repeat of the top one updates it (as vanilla merges pickups).

### Fixed
- Vital bars striped after a fault injection and retry: the host now caps the elapsed time a
  module receives at 0.25 s (a fresh build passed `float.MaxValue`, which turned animation
  clocks into NaN).
- The interaction card stayed over the open large map (Vegvísir): it hides while the map is
  open, and the HUD root is kept below the large map.
- Low health drew a red box around the health bar: the halo is gone; the frame pulses red.

### Changed
- The burn on a draining bar is a short bright edge instead of a trail that kept growing.
- Cracks in the liquid are a few small fragments in a darker shade of the liquid.

## 0.5.0-preview.3 — gold only, creature plates, F3 complete

### Removed
- The `carved` art style, the `[Theme] Style` option and the style selector (D-023). Gold is
  the only art direction; art ships flat in `plugins/art/`. An old `[Theme] Style` line in
  the config file is ignored.

### Added
- **Creature plates** (`hud.enemy`): over regular creatures, a small plate in the boss
  plate's language with name, 0–2 stars, health with a hot trail (green for tamed/friendly),
  and vanilla's aware "?" / alerted "!" marks. Each lives inside vanilla's own plate, so it
  follows vanilla's position and visibility exactly and disappears with it. `[Modules] Enemy`,
  `[Enemy] OffsetY`.

### Changed
- Gold vital bars keep the calmer liquid (soft clouds, a few long thin cracks) and the compact
  value plate from preview.2; bar sizes and the food position are back to the gold layout.
- The stamina readout scales its whole frame to its small height instead of squashing it.
- The F8 panel is taller and sums creature-plate veils in one line; restoring a module's
  veils logs one summary line.

## 0.5.0-preview.2 — carved bars reworked from the reference comparison

### Changed
- Carved vital bars are one slender piece: pointed rune cap and pommel as wide as the rails,
  a light V and diamond in recessed panels, a lighter weathered wood with worn edges, chips
  and a bevelled channel; the empty part is dark brown, not black. The frame now scales its
  borders with the bar's width (both styles), so the cap never stretches. Bars are slimmer
  (health 40, stamina and eitr 34 wide); food moves to `OffsetX` 176 by default.
- Liquid in every bar is calmer: large soft clouds and a few long, thin cracks instead of
  dense veins; bubbles fainter.
- The value plate is barely wider than the bar.

### Added
- `[Vitals] ShowValues`: hide the numbers for clean bars.

## 0.5.0-preview.1 — F3 complete, carved-wood style

### Added
- **Selectable art styles** (D-022): `[Theme] Style` = `carved` (carved wood with iron
  rivets and a V rune, the new default) or `gold` (the filigree style), switched live.
  Both ship; sprite sizes, 9-slice borders and content insets come from each style's
  `sprites.json`.
- **Boss plate** top centre: name, stars, health with a hot trail; replaces vanilla's boss
  bar (`hud.boss`, a dynamic region veiled as vanilla creates it).
- **Interaction card** beside the crosshair, mirroring vanilla's hover text and fade.
- **Notifications**: top-left card with icon and the large centre message, mirroring
  vanilla's queue, timing and fade.
- **Key hints** lifted above the hotbar (`[Hotbar] KeyHintsLift`) through `VanillaNudge`,
  restored exactly when the hotbar module stops or faults.
- Vital-bar effects in every bar's own colour: drifting veins and clots inside the liquid,
  a burn band with rising embers over the part just lost, a soft danger glow with embers
  at low health, and a value plate for the number.

### Changed
- The stamina bar above the hotbar is much smaller (220 x 22), appears whenever stamina is
  spent (not only running) and fades only after stamina is full.
- Package layout: `plugins/art/<style>/` per style.

## 0.4.1-preview.2 — R-031 follow-up, F3 sprint

### Changed
- The minimap terrain now uses a circular UI mesh with the live vanilla map material;
  only the pin images use a stencil mask. The old stencil material copy could leave
  the terrain grey even after following `minimap(Clone)`. The client test still needs
  to confirm this candidate fix. The minimap log now names its map textures.
- Vital-bar bubbles are larger and more translucent (14–15 % tint alpha). A small
  reflection moves along the liquid surface and brightens briefly when the value
  changes.

### Added
- A temporary horizontal sprint bar above the hotbar, using vanilla running and
  stamina values. It fades after running stops and has its own module toggle, offset
  and scale. It has no gameplay action or vanilla region to veil.

## 0.4.1-preview.1 — R-030 feedback

### Fixed
- **Minimap turned grey after a while**: vanilla's `Minimap.Start` replaces the small-map
  material and only then sets its textures; the module could be built before that and keep
  the old material. It now follows vanilla's live material every frame. Also stops copying
  `_zoom`, `_pixelSize`, `_mapCenter`, which the map shader does not declare (Unity errors).
- **Guardian-power cooldown stayed on a ready tile** after the cooldown was reset: the tile
  now really clears the clock text.
- Numbers inside the bars, slot corners and tile names shrink to fit instead of touching the
  frames.

### Changed
- **One ornament language** (D-020): every shape is generated from `tools/art/style.py`.
  Containers share a gold outer and bronze inner line; slots, status tiles and the wind disk
  share a bronze line with a gold hairline; the hotbar plate ends now use the same volutes
  and beads as the bars and crest.
- Bars: the glass is softer (no hard white stripe), and the living detail is now **subtle
  rising bubbles** with a slight sway (about 20 % opacity).
- Wind: the arrow sits in its own small disk on the crest, larger and always clearly visible;
  stronger wind makes it fuller.

## 0.4.0-preview.1 — Round minimap (includes 0.3.1)

### Added
- **Minimap module**: round map at the top-right in a gold ring; a crest on top of the ring
  holds the wind arrow (vanilla direction, opacity by strength) and "Dia N · HH:MM"; the
  biome name sits on a banner below. The map mirrors vanilla's small map (same fog of war,
  zoom, pins — including pins from other mods — player and ship markers; D-019). Hidden
  with the large map and in no-map worlds, like vanilla.
- Status effects now start below the minimap (`[Status] OffsetY` 340).

## 0.3.1-preview.1 — R-020 feedback (not delivered separately)

### Changed
- **Vital bars v2**: slimmer frame with small volutes on the arch shoulders, gold dots and a
  banner point with a bead at the bottom; the value is now a living liquid: glass shading,
  a bright surface line, and a seamless flow pattern rising slowly inside (calm for
  health, livelier for stamina, a counter-drifting shimmer for eitr). Health stays the
  widest and tallest bar.
- **Guardian power is one tile**: steady gold when ready; pulsing gold with the effect's
  time while active (no second tile); dimmed with a small red cross and the cooldown after.

### Fixed
- Vanilla leftovers under our bars: the three empty food slot frames, and the remaining
  decoration of the vanilla health panel (new region `hud.healthDecor`).

### Added
- Diagnostic report: tree of the vanilla health panel, marking what GenesisUI veils.
- Sprite manifest `wrap` (`clamp`/`repeat`) for scrolling textures; `tools/art/patterns.py`.


## 0.3.0-preview.1 — F3 part 1: food, hotbar, status effects; art fix

### Fixed
- **Sprites did not load in 0.2.0-preview.1** (`art/sprites.json rejected: no sprites`):
  Unity's JsonUtility left the sprite list empty without an error. Data files are now read
  by our own strict JSON reader in Core (D-018), and a build-time test runs the shipped
  manifest and PNGs through the same code the game uses.

### Added
- **Food module**: three framed slots next to the vital bars; food icon, time left written
  exactly like vanilla ("12m", blinking "45s"), a bar of what is left, the vanilla pulse
  when the food can be eaten again.
- **Hotbar module**: eight slots on a framed plate with knot caps at the bottom centre;
  key index, stack amount, durability bar (blinks red when broken), gold highlight for
  equipped items and for the gamepad selection. Keys and gamepad keep working through
  vanilla, which is veiled, not disabled.
- **Status module**: effect tiles with name and time, top-right below the vanilla
  minimap; the guardian power first, with its cooldown as m:ss and a gold frame when
  ready. Effects added by other mods appear too.
- Regions can hold several vanilla objects (the food strip is many loose pieces).
- New art: slot, active slot, status tile, hotbar plate (SVG sources).
- Config: `[Modules] Food/Hotbar/Status`, `[Food]`, `[Hotbar]`, `[Status]` offsets.


## 0.2.0-preview.1 — F2: first visual module

### Added
- **Vitals module**: health, stamina and eitr as framed vertical bars at the bottom-left
  (concepts 4–6). Numbers inside the bars, a trail showing the damage just taken, an
  ember pulse on the health frame below 25 %, the eitr bar only when the character has
  eitr. Vanilla bars are veiled, not destroyed. Position and size in `[Vitals]` config.
- **Module host**: regions with a single owner (blocked while SeneaL UI is installed),
  guarded build/refresh/teardown, live toggles (`[General] Enabled`, `[Modules] Vitals`),
  scene changes handled centrally, allocation-free refresh path.
- **Vanilla veil**: CanvasGroup veil restored exactly; re-applied every LateUpdate when a
  vanilla animator fights it, logged once.
- **Theme**: Cinzel and Cormorant Garamond (OFL) loaded at runtime with the game font as
  fallback; SVG-sourced sprites (bar frame, bar fill, medallion) from a validated manifest.
- **Diagnostics panel (F8)** in Preview/Debug: modules with state and cost, regions, veils,
  faults, input leases; buttons to inject a fault, retry a module, show vanilla underneath,
  write the report.
- `tools/inspect` (member lister over `ref/`), `tools/art/fonts.py`, `docs/regions.md`.
- 33 more Core tests (bar animation, colours, pulse, sprite manifest).


## 0.1.0-preview.2 — F1 fixes from R-000

### Fixed
- The session header logged the window size before Valheim applied the player's
  resolution (302x193 on a 1920x1080 screen). Screen and GUI scale are now logged as a
  `Display:` line once the game GUI exists, and again whenever they change.
- The watermark no longer triggers TextMeshPro's "LiberationSans SDF Font Asset was not
  found" warning: it is built inactive and enabled after the game font is assigned.
- Reference and contract labels said Valheim l-1.0.15; the copied assemblies were
  already l-1.0.16 (build 25527701). The label came from a stale server log.

## 0.1.0-preview.1 — F1 Foundation

### Added
- Plugin that loads on clients only (stops on dedicated servers) and declares
  `NetworkCompatibility(NotEnforced)`.
- `Foundation`: guard and fault registry, guarded per-class patcher with rollback,
  game-contract resolver, input leases over Jötunn, category logging with rate limit,
  own log file (non-Release), session header, redacted diagnostic report.
- Diagnostics key (F8): writes a report to `BepInEx/GenesisUI/reports/` and copies its path.
- Watermark with the exact build in Debug and Preview.
- Tests: 42 Core unit tests; contract, banned-API, Foundation-isolation and merge checks.
- `tools/`: `fill-ref.sh`, `package.sh`, art rendering; package icon.

## Unreleased

### Docs
- F0: vision, architecture, art direction, patch policy, extension API draft,
  adapters, diagnostics, testing, release, roadmap, decisions D-001–D-017,
  AGENTS.md.
