# GenesisUI

A modular, themed user interface for Valheim, built for **security and stability
first**.

> Status: **design (F0)**. No playable build yet. See [docs/ROADMAP.md](docs/ROADMAP.md).

- **Client-only and visual.** It never moves your items or talks to the server;
  players with and without it can share a world.
- **Every piece can be turned off**, live, and the vanilla UI comes back for that
  piece.
- **A failure stays local.** If a game update or another mod breaks one module, that
  module steps aside and says why; the rest keeps working.
- **Compatible by design.** Other mods integrate through a public API; mods that do
  not know GenesisUI are covered by small, isolated adapters.

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
