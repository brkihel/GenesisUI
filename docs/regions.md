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
| `hud.healthDecor` | direct children of `Hud.m_healthPanel` that hold no piece of another region | `hud.vitals` | CanvasGroup alpha 0 | R-020 showed a red emblem and a gold tick left over; the report lists the panel tree | SeneaL UI |
| `hud.food` | `Hud.m_foodBarRoot`, `m_foodBaseBar`, `m_foodIcon`, `m_foodText`, each of `m_foodIcons[]` and its slot frame (parent), `m_foodTime[]`, `m_foodBars[]` | `hud.food` | CanvasGroup alpha 0 on each | vanilla toggles icons/times with SetActive every frame; the CanvasGroup stays on the object | SeneaL UI |
| `hud.statusEffects` | `Hud.m_statusEffectListRoot` | `hud.status` | CanvasGroup alpha 0 | vanilla clones tiles under it; they inherit the veil | SeneaL UI |
| `hud.guardianPower` | `Hud.m_gpRoot` | `hud.status` | CanvasGroup alpha 0 | — | SeneaL UI |
| `hud.hotbar` | every `HotkeyBar` under `Hud` | `hud.hotbar` | CanvasGroup alpha 0 | its `Update` keeps running: gamepad selection and use still work; our hotbar reads `m_selected` | SeneaL UI |

## How the vanilla HUD hides

Read from the decompiled `Hud` (game l-1.0.16): `Hud.SetVisible(false)` moves
`m_rootObject` to local position (10000, 0, 0); `IsVisible()` checks `x < 1000`.
GenesisUI's HUD root is a child of `m_rootObject`, so it hides with the vanilla HUD
(Ctrl+F3, death, cut-scenes) without any logic of its own.

## Answered by test reports

- `m_rootObject` ("hudroot") is canvas-sized: 1920x1080 at GUI scale 1 (R-010, 0.2.0-preview.1).
- No veil was fought by vanilla in R-010, including the animator-driven stamina bar.

## Open questions

- Where exactly `HotkeyBar` hangs under `Hud` (we search the whole `Hud` object).
