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
| `hud.minimap` | `Minimap.m_smallRoot` | `hud.minimap` | CanvasGroup alpha 0 | vanilla deactivates it for the large map and in no-map worlds; our minimap follows `activeInHierarchy`. Terrain uses a circular mesh with the live material; pins stay masked (D-019, D-021) | SeneaL UI |
| `hud.hotbar` | every `HotkeyBar` under `Hud` | `hud.hotbar` | CanvasGroup alpha 0 | its `Update` keeps running: gamepad selection and use still work; our hotbar reads `m_selected` | SeneaL UI |
| `hud.hover` | `Hud.m_hoverName` | `hud.hover` | CanvasGroup alpha 0 | vanilla writes the text and fades it through the CanvasRenderer alpha every frame; the card mirrors both | SeneaL UI |
| `hud.messages` | `MessageHud.m_messageText`, `m_messageIcon`, `m_messageCenterText` | `hud.notice` | CanvasGroup alpha 0 on each | vanilla queues, times and fades the messages; the cards mirror text, icon and alpha | SeneaL UI |
| `hud.boss` | children of `EnemyHud.m_hudRoot` cloned from `m_baseHudBoss` | `hud.boss` | CanvasGroup alpha 0 on each clone | **dynamic**: vanilla creates one clone per boss in range and destroys it later; the host re-applies the veil to new clones every 0.5 s and does not warn when there is none | SeneaL UI |

The sprint bar is additive and has no vanilla region or veil. It appears when stamina
drops below its maximum (any action that spends it) and fades only after it is full.

**Nudge, not veil — `hud.keyHints`.** Vanilla's `KeyHints` (the button hints bottom
centre) sits where our hotbar plate is. `hud.hotbar` moves it up by `[Hotbar] KeyHintsLift`
through `VanillaNudge`: the original `anchoredPosition` is recorded, re-applied if vanilla
moves it, and restored exactly when the hotbar module is torn down or faults. Jötunn's
custom key hints live under the same object and move with it.

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
