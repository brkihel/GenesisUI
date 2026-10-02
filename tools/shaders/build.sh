#!/usr/bin/env bash
# Use the same keyed GPU build/provenance recipe locally or on the announced win-teste build host.
set -euo pipefail
cd "$(dirname "$0")/../.."
HOST=${SHADER_HOST:-win-teste}
if [ "$HOST" = local ]; then
    pwsh -NoProfile -File tools/shaders/build.ps1 -DirectEditor
else
    COMMIT=$(git rev-parse HEAD)
    REMOTE='C:/Unity/GenesisUIShaderSource'
    ssh -o BatchMode=yes "$HOST" "pwsh -NoProfile -Command \"New-Item -ItemType Directory -Force '$REMOTE/tools/shaders','$REMOTE/unity/Assets','$REMOTE/art/shaders' | Out-Null; \$target = [IO.Path]::GetFullPath('$REMOTE/unity/Assets/GenesisUI'); if (\$target -ne 'C:\Unity\GenesisUIShaderSource\unity\Assets\GenesisUI') { throw 'Unexpected staging path' }; if (Test-Path -LiteralPath \$target) { Remove-Item -LiteralPath \$target -Recurse -Force }\""
    scp -q tools/shaders/build.ps1 tools/shaders/provenance.ps1 "$HOST:$REMOTE/tools/shaders/"
    scp -q -r unity/Assets/GenesisUI "$HOST:$REMOTE/unity/Assets/"
    ssh -o BatchMode=yes "$HOST" "pwsh -NoProfile -File $REMOTE/tools/shaders/build.ps1 -DirectEditor -SourceCommit $COMMIT"
    scp -q "$HOST:$REMOTE/art/shaders/genesisui.shaders" art/shaders/genesisui.shaders
    scp -q "$HOST:$REMOTE/art/shaders/provenance.json" art/shaders/provenance.json
fi
python3 tools/shaders/provenance.py art/shaders/genesisui.shaders
sha256sum art/shaders/genesisui.shaders
