#!/usr/bin/env bash
# Builds art/shaders/genesisui.shaders (D-033) on the Windows build machine (ssh host win-teste):
# copies unity/Assets there, runs Unity 6000.0.75f1 (Valheim's exact version) in batch mode, and
# copies the bundle back. Unity Personal must have been activated once in Unity Hub on that machine.
#   tools/shaders/build.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
HOST=${SHADER_HOST:-win-teste}
UNITY='C:\Unity\6000.0.75f1\Editor\Unity.exe'
PROJECT='C:\Unity\GenesisUIShaders'

ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"New-Item -ItemType Directory -Force '$PROJECT\\Assets' | Out-Null; if (Test-Path '$PROJECT\\Assets\\GenesisUI') { Remove-Item -Recurse -Force '$PROJECT\\Assets\\GenesisUI' }; exit 0\""
scp -q -r unity/Assets/GenesisUI "$HOST:C:/Unity/GenesisUIShaders/Assets/"
ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"\$p = Start-Process -FilePath '$UNITY' -ArgumentList '-batchmode','-nographics','-quit','-projectPath','$PROJECT','-executeMethod','GenesisUI.BuildShaders.Build','-logFile','$PROJECT\\build.log' -Wait -PassThru; exit \$p.ExitCode\"" || {
    echo "Unity build failed; log follows" >&2
    ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"Get-Content '$PROJECT\\build.log' -Tail 60\"" >&2
    exit 1
}
# Some errors only show when compiling for another API (Vulkan): the bundle still builds, the log says so.
if ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"if (Select-String -Path '$PROJECT\\build.log' -Pattern 'Shader error' -Quiet) { exit 1 } else { exit 0 }\""; then :; else
    echo "shader errors in the Unity log:" >&2
    ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"Select-String -Path '$PROJECT\\build.log' -Pattern 'Shader error' | % Line\"" >&2
    exit 1
fi
scp -q "$HOST:C:/Unity/GenesisUIShaders/Bundles/genesisui.shaders" art/shaders/genesisui.shaders
sha256sum art/shaders/genesisui.shaders
