# GenesisUI — Vision

GenesisUI is a complete, themed user interface for Valheim built on one premise:
**a UI mod must never be the reason a player loses an item, a session, or another
mod's feature.** Looks come third. Safety and stability come first and second.

It is part of the GenesisMods line. The pillars of every GenesisMods product apply,
in this order:

1. **Security above everything** — player data, items, the server, other mods.
2. **Simplicity and didactics** — every option explained; every failure explained.
3. **A clean, Valheim-themed look** — see [ART-DIRECTION.md](ART-DIRECTION.md).

## What GenesisUI is

- A **client-side** replacement for the vanilla HUD and windows: vitals,
  hotbar, status effects, minimap frame, compass, hover/interaction cards, boss and
  enemy bars, notifications, key hints, inventory, crafting, texts, GenesisUI settings
  and map windows. Main-menu/native-settings replacement remains planned.
- **Modular**: every piece is a module that can be turned off on its own, live,
  handing its screen region back to vanilla.
- **Dynamic**: layout, theme and texts are data, not code. Grids follow whatever
  size the game (or another mod) gives them; nothing assumes 8×4.
- **Compatible by design**: other mods plug in through a public API; mods that do
  not know GenesisUI are covered by small, isolated adapters; elements that other
  mods attach to the vanilla HUD stay visible. Supporting a new mod must never mean
  rewriting a screen. See [EXTENSION-API.md](EXTENSION-API.md) and
  [ADAPTERS.md](ADAPTERS.md).

## What GenesisUI is not (for now)

- **Not a gameplay mod.** No extra inventory rows, quick slots, action slots,
  crafting from chests, quick stack, area pickup or tombstone logic. Those change
  game state and need server authority; they belong to a separate, later package
  (roadmap F8) and are never a requirement for the UI. **Exception (D-028):** the inventory
  gameplay (admin slot count, quick/utility/equipment slots, sort) is built during F4 as an
  isolated module of this plugin, with its own toggle and server-synced settings.
- **Visual modules register no RPC.** The optional inventory module uses ServerSync
  for admin settings (D-031). The plugin declares `NetworkCompatibility(NotEnforced)`:
  players with or without it can share a server.
- **Not a copy of anything.** No code, art, icon or text from SeneaL UI or any other
  closed mod. No redistributed game assets. See [DECISIONS.md](DECISIONS.md) D-005.

## Success criteria for replacing SeneaL UI on the GenesisHeim server

- Entering, leaving and re-entering a world never duplicates or loses HUD elements.
- Every module can be switched back to vanilla, individually, in-session.
- Nothing is lost or duplicated when moving items between inventory, container,
  backpack and tombstone with the real modpack. The inventory module changes slot
  positions under D-030; its full item-safety matrix must pass on the client.
- A game update breaks at most the modules whose contracts changed; the contract
  tests name them before a player does.
- The diagnostic report of any failure names the module, the region, the game
  build and the mods involved.
- Measured client cost stays inside the budget in
  [ARCHITECTURE.md](ARCHITECTURE.md#performance-budget).

## Future sister project

The infrastructure GenesisUI needs (guarded patching, contract checks, failure
isolation, input leases, diagnostics plumbing) is built inside GenesisUI in a
self-contained `Foundation` layer. When a second GenesisMods mod needs it, it moves
into **GenesisModLIB**, a separate plugin. See DECISIONS.md D-015.
