<div align="center">

<img src=".github/assets/banner.webp" alt="GenesisUI" width="100%">

### Valheim's whole interface, redrawn in fine gold metal

Living bars, clear windows and a framed map — with the game's own interface still running underneath,<br>
so your other mods keep working and any part can be switched back.

[**Download on Hexium**](https://valheim.hexium.gg/mods/GenesisMods/GenesisUI) · [**Discord**](https://discord.gg/TZ785sYtgx) · [**Leia em português**](README.pt-BR.md)

[![Version](https://img.shields.io/badge/version-1.0.1-c8a45c?style=flat-square&labelColor=0d151d)](CHANGELOG.md)
[![Valheim](https://img.shields.io/badge/Valheim-1.0.16-c8a45c?style=flat-square&labelColor=0d151d)](https://valheim.com)
[![Jötunn](https://img.shields.io/badge/J%C3%B6tunn-2.30.2-c8a45c?style=flat-square&labelColor=0d151d)](https://github.com/Valheim-Modding/Jotunn)
[![License](https://img.shields.io/badge/license-MIT-c8a45c?style=flat-square&labelColor=0d151d)](LICENSE)
[![Discord](https://img.shields.io/badge/Discord-GenesisMods-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/TZ785sYtgx)

</div>

---

## A HUD that feels alive

Health, stamina and eitr as liquid bars that move and burn away when you take a hit. A round minimap with the day, the time and the biome. Boss and creature plates, food and active effects you read at a glance.

<img src=".github/assets/hud.webp" alt="GenesisUI HUD" width="100%">

## An inventory that makes sense

Your bag, your equipment and the item you are looking at, side by side, with extra quick-use and utility slots. **R** sorts; **Q** and **E** switch between inventory, skills, map, crafting, achievements and settings.

<img src=".github/assets/inventory.webp" alt="GenesisUI inventory" width="100%">

## Craft without guessing

At any station, what you can make comes first and what is missing is written plainly. Crafting and upgrading side by side, with the upgrade's gain shown before you spend.

<img src=".github/assets/crafting.webp" alt="GenesisUI crafting" width="100%">

## A map worth opening

The map in a proper frame, with pin filters and eight new markers — dungeon, ore, monster den, base, portal, treasure, trader, danger — saved as ordinary pins, so a world never depends on the mod.

<img src=".github/assets/map.webp" alt="GenesisUI map" width="100%">

## Features

<table>
<tr>
<td width="50%" valign="top">

**HUD**
- Liquid vital bars with a burn trail and low-health pulse
- Round minimap with wind, day, time and biome
- Hotbar, food, status effects and guardian power
- Boss and creature plates, interaction card, notifications
- Key hints in the same style, at half size

</td>
<td width="50%" valign="top">

**Windows**
- Inventory with quick-use, utility and equipment slots, sorting
- Crafting and upgrading at every station
- Skills and character, achievements, GenesisUI settings
- Trader, split and variant dialogs, text input
- Esc menu over a blurred game

</td>
</tr>
<tr>
<td width="50%" valign="top">

**Map and building**
- Framed map with filters, zoom, "to the player"
- Eight custom marker types on top of vanilla's
- Hammer, hoe and cultivator menus with search and favourites
- Placement card while building

</td>
<td width="50%" valign="top">

**Safe by design**
- Every part can be turned off live; vanilla's comes back
- A failing part closes, rebuilds itself and explains what happened
- Vanilla stays the engine: other mods' items, pins and messages still show
- No gameplay network messages; only the optional server lock of the slot settings (ServerSync)

</td>
</tr>
</table>

<img src=".github/assets/skills.webp" alt="GenesisUI skills and character" width="100%">

## Install

With a mod manager (Hexium, Gale, r2modman): install **GenesisUI**; Jötunn comes with it.

Manually: BepInExPack for Valheim and [Jötunn](https://github.com/Valheim-Modding/Jotunn), then extract the package's `plugins` folder into `BepInEx/plugins/GenesisUI`.

GenesisUI is a client mod. On a dedicated server it is optional and only keeps the inventory slot settings the same for everyone; players without it can still join.

## Settings

In game, open the inventory and go to the **Settings** tab (or `BepInEx/config/Genesis.GenesisUI.cfg`). Each part of the interface has its own switch under *Modules*.

## Support

Ask on the [**GenesisMods Discord**](https://discord.gg/TZ785sYtgx). For a bug, press **F8** in game to save a report (personal data removed) and post it in **#valheim-bugs** with `BepInEx/LogOutput.log`.

See the [release notes](CHANGELOG.md).

## For developers

GenesisUI is built for security and stability first: it hides vanilla's UI instead of destroying it, every game member it touches is a contract checked by tests, and every failure stays inside its module.

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

```bash
dotnet build GenesisUI.sln -c Release
dotnet test GenesisUI.sln -c Release
tools/package.sh Release
```

## License

Code: MIT ([LICENSE](LICENSE)). Fonts: SIL Open Font License 1.1 (`art/fonts/`). Original art: [art/LICENSES.md](art/LICENSES.md).

Valheim is a trademark of Iron Gate AB. GenesisUI is not affiliated with Iron Gate AB or Coffee Stain.

<div align="center">
<br>
<sub>Made by <a href="https://github.com/brkihel">BRKiHeL</a> · <a href="https://discord.gg/TZ785sYtgx">GenesisMods</a></sub>
</div>
