#!/usr/bin/env bash
# Builds art/shaders/genesisui.shaders (D-033) on the Windows build machine (ssh host win-teste):
# copies unity/Assets there, runs Unity 6000.0.75f1 (Valheim's exact version) in batch mode, and
# copies the bundle back. Unity Personal must have been activated once in Unity Hub on that machine.
#   tools/shaders/build.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
HOST=${SHADER_HOST:-win-teste}
UNITY='D:\Unity\6000.0.75f1\Editor\Unity.exe'
PROJECT='D:\Unity\GenesisUIShaders'

ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"New-Item -ItemType Directory -Force '$PROJECT\\Assets' | Out-Null; Remove-Item -Recurse -Force '$PROJECT\\Assets\\GenesisUI' -ErrorAction SilentlyContinue\""
scp -q -r unity/Assets/GenesisUI "$HOST:D:/Unity/GenesisUIShaders/Assets/"
ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"\$p = Start-Process -FilePath '$UNITY' -ArgumentList '-batchmode','-nographics','-quit','-projectPath','$PROJECT','-executeMethod','GenesisUI.BuildShaders.Build','-logFile','$PROJECT\\build.log' -Wait -PassThru; exit \$p.ExitCode\"" || {
    echo "Unity build failed; log follows" >&2
    ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"Get-Content '$PROJECT\\build.log' -Tail 60\"" >&2
    exit 1
}
scp -q "$HOST:D:/Unity/GenesisUIShaders/Bundles/genesisui.shaders" art/shaders/genesisui.shaders
sha256sum art/shaders/genesisui.shaders
