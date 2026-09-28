# GenesisUI — Regions

A region is a named piece of the vanilla UI. One owner at a time
([ARCHITECTURE.md §3](ARCHITECTURE.md#region-registry)). Resolvers live in
`src/GenesisUI/Host/RegionRegistry.cs`; every vanilla member they touch is a
`[GameContract]`.

| Region | Vanilla object | Owner module | Veil | Known interplay | Foreign owners |
|---|---|---|---|---|---|
| `hud.health` | `Hud.m_healthBarRoot` | `hud.vitals` | CanvasGroup alpha 0 | `m_healthAnimator` may animate it | SeneaL UI |
| `hud.stamina` | `Hud.m_staminaBar2Root` | `hud.vitals` | CanvasGroup alpha 0, re-applied each LateUpdate | `m_staminaAnimator` "Visible" hides it 1 s after full; vanilla moves it up while building or sailing | SeneaL UI |
| `hud.eitr` | `Hud.m_eitrBarRoot` | `hud.vitals` | CanvasGroup alpha 0 | `m_eitrAnimator` | SeneaL UI |

## How the vanilla HUD hides

Read from the decompiled `Hud` (game l-1.0.16): `Hud.SetVisible(false)` moves
`m_rootObject` to local position (10000, 0, 0); `IsVisible()` checks `x < 1000`.
GenesisUI's HUD root is a child of `m_rootObject`, so it hides with the vanilla HUD
(Ctrl+F3, death, cut-scenes) without any logic of its own.

## Open questions (answered by test reports)

- Is `m_rootObject` canvas-sized? The host logs `HUD root under …: parent …, canvas …`
  and adapts either way; the answer goes here.
- Which veils does vanilla fight? The overlay and report show `fought Nx` per veil.
