# Called by the shader builder and both package paths. Hashes bind the GPU check to exact sources/bundle.
param([ValidateSet('Record', 'Verify')][string]$Mode, [string]$Bundle, [string]$BuildLog, [string]$SourceCommit, [string]$RecordPath)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$RecordPath) { $RecordPath = Join-Path $repoRoot 'art/shaders/provenance.json' }
$sourceRoot = Join-Path $repoRoot 'unity/Assets/GenesisUI'
$sources = [ordered]@{}
foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -File -Recurse | Sort-Object FullName) {
    $relative = [IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')
    $sources[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
}
if (!(Test-Path -LiteralPath $Bundle -PathType Leaf)) { throw 'Required shader bundle missing' }
$bundleHash = (Get-FileHash -LiteralPath $Bundle -Algorithm SHA256).Hash
if ($Mode -eq 'Record') {
    $logText = Get-Content -LiteralPath $BuildLog -Raw
    if ($logText -match 'Shader error' -or $logText -notmatch 'Initialize engine version: 6000\.0\.75f1' -or $logText -notmatch 'GenesisUI preview key verification passed: solid 256, edge 512, clear 256 \(Direct3D11\)') { throw 'Required Unity version/keyed GPU verification missing/failed' }
    if (!$SourceCommit) { $SourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim() }
    if ($SourceCommit -notmatch '^[0-9a-f]{40}$') { throw 'Invalid shader source commit' }
    $stagedSources = Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($BuildLog))) 'Assets/GenesisUI'
    foreach ($relative in $sources.Keys) {
        $stagedFile = Join-Path $stagedSources $relative.Substring('unity/Assets/GenesisUI/'.Length)
        if (!(Test-Path -LiteralPath $stagedFile) -or (Get-FileHash -LiteralPath $stagedFile).Hash -ne $sources[$relative]) { throw "Shader source changed during build: $relative" }
    }
    $record = [ordered]@{
        schema = 1; unityVersion = '6000.0.75f1'; target = 'StandaloneWindows64'
        requiredShader = 'GenesisUI/Keyed'; bundleSha256 = $bundleHash
        sourceCommit = $SourceCommit; sources = $sources
        gpu = [ordered]@{ api = 'Direct3D11'; solid = 256; edge = 512; clear = 256; verifiedAtUtc = (Get-Item -LiteralPath $BuildLog).LastWriteTimeUtc.ToString('o'); logSha256 = (Get-FileHash -LiteralPath $BuildLog).Hash }
    }
    $record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $recordPath -Encoding utf8NoBOM
} else {
    $record = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    if ($record.schema -ne 1 -or $record.requiredShader -ne 'GenesisUI/Keyed' -or $record.unityVersion -ne '6000.0.75f1' -or $record.target -ne 'StandaloneWindows64' -or $record.bundleSha256 -ne $bundleHash) { throw 'Shader provenance/bundle mismatch' }
    if ($record.gpu.api -ne 'Direct3D11' -or $record.gpu.solid -ne 256 -or $record.gpu.edge -ne 512 -or $record.gpu.clear -ne 256) { throw 'Required keyed GPU provenance missing' }
    if (@($record.sources.PSObject.Properties).Count -ne $sources.Count) { throw 'Shader source inventory mismatch' }
    foreach ($relative in $sources.Keys) { if ($record.sources.$relative -ne $sources[$relative]) { throw "Shader source changed since verified build: $relative" } }
}
