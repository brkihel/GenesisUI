# 3D preview report — 1.1.0, 2026-10-02

Source: Diego's client report `report-20261002-132713.log` in the Gale `test` profile, and his confirmation that the character and hammer remained invisible. No in-game result for the next fix yet.

- No module fault or preview warning. The character copy built on layer 30. Its camera reported 6 enabled and 6 visible renderers, with `Custom/Creature` and `Custom/Player` forward passes.
- The hammer reported 1 enabled and 1 visible renderer, `Custom/Creature` with forward passes. The other sampled items also reported visible renderers. This rules out missing models and camera culling for those samples. `Renderer.isVisible` does not establish that opaque pixels reached the UI.
- The previous changes of render path and stage altitude did not resolve the character or hammer. The shared remaining path is the forward pass output into a transparent render texture, followed by `RawImage` alpha blending. The model shader may write colour with zero alpha; this is a hypothesis until the pixel probe and client view agree.

Next candidate: compose every 3D preview from a colour key, independent of the model's output alpha. Log one small texture readback per model (colour pixels and coloured pixels with near-zero alpha). Run R-062 on Diego's client and report visible results together with F8 output.

## Candidate verification on the development PC

`dotnet build` succeeded for Debug, Preview and Release with zero warnings or errors.
After the final source changes, `dotnet test GenesisUI.sln -c <channel> --no-restore -m:1 -nr:false`
built and passed all three channels: 145 Core tests and 8 contract tests per channel,
with no skips. The contract suite includes the banned-API and Foundation-isolation scans.
Shader source and bundle were not changed by this fix; it reuses the existing `GenesisUI/Keyed`.
No game was launched. No performance or visual approval is claimed.

Diego explicitly authorized packaging without the prior commit in this session, whose `.git`
access is read-only. `tools/package.ps1 Preview` produced the Hexium-format archive and
repeated the Preview checks successfully (145 Core + 8 contract tests, no skips).
The new Windows script mirrors `package.sh`; Git Bash could not start in this restricted session.

Package: `dist/GenesisMods-GenesisUI-1.1.1-preview.1.zip`, 2,871,548 bytes,
137 files, 118 manifest sprites, six fonts and the shader bundle. The icon is 256×256;
the dependency is checked against Jötunn 2.30.2 in `ref/`. The archive layout, entry lengths
and manifest were verified after creation. The package includes the existing uncommitted
1.1 development work; its watermark's HEAD stamp is not a commit of these changes.

SHA-256: `E317EA5ACE8E12E52361013B1F8C3987E676D37DB6756D30E948DF1C1FD78EEF`.
Client verification is still pending.
