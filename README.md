# GenesisUI

A modular, themed user interface for Valheim, built for **security and stability
first**.

> Status: **F3 (HUD) approved; F4.0 (Diego's textures on the HUD) approved; F4.1 next.** Latest test
> package: 0.6.0-preview.4 (R-043, R-044). See [docs/ROADMAP.md](docs/ROADMAP.md).

GenesisUI is a complete rework of the vanilla UI: HUD, inventory and crafting windows,
menus and map, in one gold ornament language. What exists today (0.5.0) is the whole HUD:
vital bars with a living liquid, food, hotbar, status effects and guardian power, round
minimap with wind, day and biome, stamina readout, boss and creature plates, interaction
card, stacked notifications, and the vanilla key hints lifted above the hotbar.

- **Client-only and visual.** It never moves your items or talks to the server;
  players with and without it can share a world.
- **Every piece can be turned off**, live, and the vanilla UI comes back for that
  piece.
- **A failure stays local.** If a game update or another mod breaks one module, that
  module steps aside and says why; the rest keeps working.
- **Compatible by design.** It mirrors what vanilla decides to show (so other mods'
  pins, messages and plates still appear) and hides vanilla instead of destroying it.
  A public API and small, isolated adapters for other mods come after the vanilla UI
  is complete (roadmap F7).

## Documentation

| Document | For |
|---|---|
| [VISION](docs/VISION.md) | what GenesisUI is and is not |
| [ARCHITECTURE](docs/ARCHITECTURE.md) | how it is built |
| [ART-DIRECTION](docs/ART-DIRECTION.md) | the look |
| [PATCH-POLICY](docs/PATCH-POLICY.md) | how we patch the game |
| [EXTENSION-API](docs/EXTENSION-API.md) | integrating your mod |
| [ADAPTERS](docs/ADAPTERS.md) | built-in support for other mods |
| [DIAGNOSTICS](docs/DIAGNOSTICS.md) | logs, debug overlay, reports |
| [TESTING](docs/TESTING.md) | how it is tested |
| [RELEASE](docs/RELEASE.md) | packaging and releases |
| [DECISIONS](docs/DECISIONS.md) | why things are the way they are |
| [AGENTS](AGENTS.md) | rules for contributors and coding agents |

## Requirements

- BepInExPack for Valheim
- [Jötunn](https://github.com/Valheim-Modding/Jotunn) (latest)

## License

Code: MIT (see [LICENSE](LICENSE)). Fonts: SIL Open Font License 1.1 (see
`art/fonts/`). Original art: see `art/LICENSES.md`.
