# Build in an isolated workspace project with Valheim's exact Unity version.
# Uses an activated Editor directly (-DirectEditor) or via the Unity CLI (default).
# A graphics device is required for the edge check; no game is launched.
param(
    [string]$EditorPath = 'C:\Unity\6000.0.75f1\Editor\Unity.exe',
    [switch]$PackagePreview,
    [switch]$DirectEditor,
    [int]$TimeoutSeconds = 300
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!(Test-Path -LiteralPath $EditorPath)) { throw "Missing Unity 6000.0.75f1 Editor: $EditorPath" }
$project = Join-Path $repoRoot 'dist/shader-build'
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $project $folder) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'unity/Assets/GenesisUI') -Destination (Join-Path $project 'Assets') -Recurse -Force
Set-Content -LiteralPath (Join-Path $project 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Value 'm_EditorVersion: 6000.0.75f1' -Encoding utf8NoBOM
$log = Join-Path $project 'build.log'
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
Copy-Item -LiteralPath (Join-Path $project 'Bundles/genesisui.shaders') -Destination (Join-Path $repoRoot 'art/shaders/genesisui.shaders') -Force
Get-FileHash -LiteralPath (Join-Path $repoRoot 'art/shaders/genesisui.shaders') -Algorithm SHA256
if ($PackagePreview) { & (Join-Path $repoRoot 'tools/package.ps1') Preview }
