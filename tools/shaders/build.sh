#!/usr/bin/env bash
# Builds art/shaders/genesisui.shaders (D-033) on the Windows build machine (ssh host win-teste):
# copies unity/Assets there, runs Unity 6000.0.75f1 (Valheim's exact version) in batch mode, and
# copies the bundle back. Unity Personal must have been activated once in Unity Hub on that machine.
# On that Windows machine itself (Git Bash), SHADER_HOST=local runs the same steps without ssh.
#   tools/shaders/build.sh
#   SHADER_HOST=local tools/shaders/build.sh
set -euo pipefail
cd "$(dirname "$0")/../.."
HOST=${SHADER_HOST:-win-teste}
UNITY='C:\Unity\6000.0.75f1\Editor\Unity.exe'
PROJECT='C:\Unity\GenesisUIShaders'

# win <powershell command>: runs on the build machine.
win() {
    if [ "$HOST" = local ]; then powershell.exe -NoProfile -Command "$1"
    else ssh -o BatchMode=yes "$HOST" "powershell -NoProfile -Command \"$1\""; fi
}
# to_win <local path> <windows path> / from_win <windows path> <local path>
to_win() { if [ "$HOST" = local ]; then cp -r "$1" "$2"; else scp -q -r "$1" "$HOST:$2"; fi; }
from_win() { if [ "$HOST" = local ]; then cp "$1" "$2"; else scp -q "$HOST:$1" "$2"; fi; }


win "New-Item -ItemType Directory -Force '$PROJECT\\Assets' | Out-Null; if (Test-Path '$PROJECT\\Assets\\GenesisUI') { Remove-Item -Recurse -Force '$PROJECT\\Assets\\GenesisUI' }; exit 0"
to_win unity/Assets/GenesisUI "C:/Unity/GenesisUIShaders/Assets/"
win "\$p = Start-Process -FilePath '$UNITY' -ArgumentList '-batchmode','-nographics','-quit','-projectPath','$PROJECT','-executeMethod','GenesisUI.BuildShaders.Build','-logFile','$PROJECT\\build.log' -Wait -PassThru; exit \$p.ExitCode" || {
    echo "Unity build failed; log follows" >&2
    win "Get-Content '$PROJECT\\build.log' -Tail 60" >&2
    exit 1
}
# Some errors only show when compiling for another API (Vulkan): the bundle still builds, the log says so.
if win "if (Select-String -Path '$PROJECT\\build.log' -Pattern 'Shader error' -Quiet) { exit 1 } else { exit 0 }"; then :; else
    echo "shader errors in the Unity log:" >&2
    win "Select-String -Path '$PROJECT\\build.log' -Pattern 'Shader error' | % Line" >&2
    exit 1
fi
from_win "C:/Unity/GenesisUIShaders/Bundles/genesisui.shaders" art/shaders/genesisui.shaders
sha256sum art/shaders/genesisui.shaders
