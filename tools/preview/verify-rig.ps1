param([string]$EditorPath = 'C:\Unity\6000.0.75f1\Editor\Unity.exe', [int]$TimeoutSeconds = 300)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = Join-Path $repo 'dist/character-verification'
foreach ($folder in @('Assets/Editor', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $project $folder) -Force | Out-Null
}
$staleSource = Join-Path $project 'Assets/VisualRigSnapshot.cs'
if (Test-Path -LiteralPath $staleSource) { Remove-Item -LiteralPath $staleSource -Force }
Copy-Item -LiteralPath (Join-Path $repo 'src/GenesisUI/Widgets/VisualRigSnapshot.cs') -Destination (Join-Path $project 'Assets/Editor/VisualRigSnapshot.cs') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VisualRigVerification.cs') -Destination (Join-Path $project 'Assets/Editor/VisualRigVerification.cs') -Force
Set-Content -LiteralPath (Join-Path $project 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding utf8NoBOM
Set-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Value 'm_EditorVersion: 6000.0.75f1' -Encoding utf8NoBOM
$log = Join-Path $project 'verification.log'
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $log + '"'),
    '-noUpm', '-force-d3d11', '-job-worker-count', '2', '-executeMethod', 'GenesisUI.Verification.VisualRigVerification.Verify')
$process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) { Stop-Process -InputObject $process -Force; throw 'Visual rig verification timed out' }
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'GenesisUI visual rig verification passed' -Quiet)) {
    Get-Content -LiteralPath $log -Tail 60
    throw 'Visual rig verification failed'
}
Select-String -LiteralPath $log -Pattern 'GenesisUI visual rig'
