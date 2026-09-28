# GenesisUI — Extension API (draft v1)

Status: design. Ships in F4. Until 1.0 the API may change; every change is listed in
CHANGELOG.md under "API".

The API is how compatibility becomes a property of the design: a mod describes
**what** it wants to show, GenesisUI decides **where and how**. Supporting a mod never
requires changing a GenesisUI screen.

## 1. Consuming the API from another mod (soft dependency)

```csharp
[BepInDependency("Genesis.GenesisUI", BepInDependency.DependencyFlags.SoftDependency)]
public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        if (Chainloader.PluginInfos.ContainsKey("Genesis.GenesisUI"))
            GenesisUIBridge.Register();          // separate class: only JIT-compiled if present
    }
}

internal static class GenesisUIBridge
{
    public static void Register() =>
        GenesisUI.Api.UI.Register(new MyTooltipSection());
}
```

Keeping every GenesisUI type inside the bridge class means the other mod loads fine
when GenesisUI is absent.

## 2. Extension points

| Interface | Purpose | Example use |
|---|---|---|
| `IItemTooltipSection` | adds a section to item details and tooltips | item comparison, Jewelcrafting sockets |
| `IItemBadgeProvider` | small corner badge on item slots | socket count, "new" marker |
| `IEquipmentSlotProvider` | extra equipment slots shown in the equipment panel | rings, necklace, backpack |
| `IContainerPanelProvider` | shows a non-vanilla inventory as a container panel | backpacks |
| `IPlayerStatProvider` | rows for the character panel, optional level badge | a leveling mod's level and attributes |
| `IHudWidget` | a HUD widget that participates in layout and settings | season indicator |
| `IShellTab` | a tab in the main window navigation | a mod's own journal |
| `IMapPinCategoryProvider` | filter pills and legend entries for custom pins | resource or event pins |
| `IKeyHintSource` | footer and HUD key hints for non-Jötunn mods | — |

Jötunn `KeyHintManager` hints and `ModQuery` item origin are read automatically; mods
that already use them need nothing extra.

Sketch of one contract:

```csharp
public interface IItemTooltipSection
{
    string Id { get; }                 // "jewelcrafting.sockets" — unique, stable
    int Order { get; }                 // lower renders first
    bool AppliesTo(ItemDrop.ItemData item);
    void Write(ITooltipBuilder builder, ItemDrop.ItemData item);
}

public interface ITooltipBuilder       // data only; GenesisUI renders it with the theme
{
    void Heading(string text);
    void Stat(string label, string value, StatTone tone = StatTone.Neutral);
    void Line(string text, TextRole role = TextRole.Body);
    void Bar(string label, float value01);
}
```

Mods write data, not UI objects: they never receive a Unity `GameObject` owned by
GenesisUI. The only exceptions are `IHudWidget` and `IShellTab`, which receive a
root to build into. That root is destroyed on scene change; references must not be
kept.

`IEquipmentSlotProvider` only **describes** slots. Equipping and unequipping call the
provider's own methods, so the owning mod keeps its rules. GenesisUI never moves an
item itself.

## 3. Guarantees GenesisUI gives to extensions

- Every call into an extension goes through the guard. A throwing extension is
  disabled for the session, its mod is named in the log and in the diagnostics
  overlay, and the rest of the UI keeps working.
- Calls happen on the main thread, at a bounded rate. `Write`/`Refresh` never run
  per frame unless the widget asked for it.
- Registration can happen any time after GenesisUI's `Awake`. Registrations made
  before a screen exists are applied when it is built.

## 4. What extensions must do

- Return fast (budget: 0.05 ms per call). No I/O, no allocation-heavy LINQ in
  `Write`/`Refresh`.
- Use a stable `Id` prefixed with the mod name.
- Use localization tokens (`$...`) for text.

## 5. Versioning

- `GenesisUI.Api.UI.ApiVersion` is a semver string.
- Before 1.0: breaking changes are allowed in minor versions and announced.
- From 1.0: members are marked `[Obsolete]` for one minor version before removal;
  removal only in a major version.
