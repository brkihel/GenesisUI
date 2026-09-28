# R-000 — 0.1.0-preview.1 (bbbef9a)

| Field | Value |
|---|---|
| Date | 2026-09-28 |
| Run by | Diego, own client (Windows 11, 1920x1080, Gale profile `test`) |
| Game | Valheim 1.0.16, BepInEx 5.4.23.5, Jotunn 2.30.2 |
| Result | **Passed** — all 12 steps as expected, no `[Error]` from GenesisUI |

## Observations and follow-ups

1. **Session header showed `Screen: 302x193 @60Hz, fullscreen=False`** at load, while the
   F8 report taken in-world showed `1920x1080 @60Hz, fullscreen=True`. Cause: the header
   is written in `Awake`, before Valheim applies the resolution. Fixed in preview.2
   (display logged when the GUI exists and on change).
2. **Unity warning, twice** (menu and world): `The LiberationSans SDF Font Asset was not
   found. There is no Font Asset assigned to GenesisUI_Watermark.` Cause: TMP's Awake runs
   on `AddComponent`, before the game font is assigned. Fixed in preview.2 (built
   inactive). Harmless in preview.1: the watermark rendered with the game font.
3. Report redaction worked: the Windows user became `<os-user>` in paths; no character
   or world name leaked.
4. Reference label said l-1.0.15; the game and the copied assemblies are l-1.0.16. Label
   fixed at the source (devplugins `atualizar-referencias.sh`).
