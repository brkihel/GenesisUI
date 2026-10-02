# Build in an isolated workspace project with Valheim's exact Unity version.
# Uses an activated Editor directly (-DirectEditor) or via the Unity CLI (default).
# A graphics device is required for the edge check; no game is launched.
param(
    [string]$EditorPath = 'C:\Unity\6000.0.75f1\Editor\Unity.exe',
    [switch]$PackagePreview,
    [switch]$DirectEditor,
    [int]$TimeoutSeconds = 300,
    [string]$SourceCommit
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!(Test-Path -LiteralPath $EditorPath)) { throw "Missing Unity 6000.0.75f1 Editor: $EditorPath" }
$project = Join-Path $repoRoot 'dist/shader-build'
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $project $folder) -Force | Out-Null
}
$oldSource = [IO.Path]::GetFullPath((Join-Path $project 'Assets/GenesisUI'))
if (!$oldSource.StartsWith([IO.Path]::GetFullPath($project) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Shader staging path outside dist/shader-build' }
if (Test-Path -LiteralPath $oldSource) { Remove-Item -LiteralPath $oldSource -Recurse -Force }
Copy-Item -LiteralPath (Join-Path $repoRoot 'unity/Assets/GenesisUI') -Destination (Join-Path $project 'Assets') -Recurse -Force
Set-Content -LiteralPath (Join-Path $project 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Value 'm_EditorVersion: 6000.0.75f1' -Encoding utf8NoBOM
$log = Join-Path $project 'build.log'
$stagedBundle = Join-Path $project 'Bundles/genesisui.shaders'
foreach ($staleFile in @($log, $stagedBundle)) {
    $stalePath = [IO.Path]::GetFullPath($staleFile)
    if (!$stalePath.StartsWith([IO.Path]::GetFullPath($project) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Stale build artifact outside shader workspace' }
    if (Test-Path -LiteralPath $stalePath) { Remove-Item -LiteralPath $stalePath -Force }
}
if ($DirectEditor) {
    # Match the existing build.sh launch, without the CLI's Hub session arguments.
    $arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
        '-logFile', ('"' + $log + '"'), '-noUpm', '-force-d3d11', '-job-worker-count', '2',
        '-executeMethod', 'GenesisUI.PreviewKeyVerification.BuildAndVerify')
    $process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -InputObject $process -Force
        throw 'Unity shader build timed out; shipped bundle was not replaced'
    }
    $buildExit = $process.ExitCode
} else {
    unity run $project --editor-path $EditorPath --timeout $TimeoutSeconds --log-file $log --no-tail --format json --non-interactive --no-banner -- -noUpm -force-d3d11 -executeMethod GenesisUI.PreviewKeyVerification.BuildAndVerify
    $buildExit = $LASTEXITCODE
}
if ($buildExit -ne 0) {
    if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 60 }
    throw 'Unity shader build or GPU verification failed; shipped bundle was not replaced'
}
if (Select-String -LiteralPath $log -Pattern 'Shader error' -Quiet) { throw 'Shader errors in the Unity build log' }
if (!(Select-String -LiteralPath $log -Pattern 'GenesisUI preview key verification passed' -Quiet)) { throw 'GPU edge verification did not pass' }
$stagedRecord = Join-Path $project 'provenance.json'
& (Join-Path $PSScriptRoot 'provenance.ps1') Record $stagedBundle $log -SourceCommit $SourceCommit -RecordPath $stagedRecord
$bundleTarget = Join-Path $repoRoot 'art/shaders/genesisui.shaders'
$recordTarget = Join-Path $repoRoot 'art/shaders/provenance.json'
$previousBundle = if (Test-Path -LiteralPath $bundleTarget) { [IO.File]::ReadAllBytes($bundleTarget) } else { $null }
$previousRecord = if (Test-Path -LiteralPath $recordTarget) { [IO.File]::ReadAllBytes($recordTarget) } else { $null }
try {
    Copy-Item -LiteralPath $stagedBundle -Destination $bundleTarget -Force
    Copy-Item -LiteralPath $stagedRecord -Destination $recordTarget -Force
} catch {
    if ($null -ne $previousBundle) { [IO.File]::WriteAllBytes($bundleTarget, $previousBundle) }
    if ($null -ne $previousRecord) { [IO.File]::WriteAllBytes($recordTarget, $previousRecord) }
    throw
}
Get-FileHash -LiteralPath (Join-Path $repoRoot 'art/shaders/genesisui.shaders') -Algorithm SHA256
if ($PackagePreview) { & (Join-Path $repoRoot 'tools/package.ps1') Preview }
