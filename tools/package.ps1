# Windows equivalent of package.sh. Dependencies must already be restored.
# Preview packages are for client testing only (docs/RELEASE.md).
param([ValidateSet('Preview', 'Release')][string]$Channel = 'Preview')

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$jotunnVersion = '2.30.2'
Push-Location $repoRoot
try {
    if (!(Test-Path -LiteralPath 'ref/assembly_valheim.dll')) { throw 'ref/ is empty: refresh the reference assemblies first' }
    $dirty = git status --porcelain --untracked-files=no
    if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Commit tracked changes before packaging: the watermark must identify the packaged code' }
    if (git ls-files --others --exclude-standard -- src tests docs tools art unity) { throw 'Commit new project files before packaging' }
    $env:DOTNET_GCHeapHardLimit = '0x40000000'
    dotnet test GenesisUI.sln -c $Channel --no-restore --nologo -v q -m:1 -nr:false
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed; no package produced' }

    $info = Get-Content -LiteralPath 'src/GenesisUI/PluginInfo.cs' -Raw
    $version = [regex]::Match($info, 'const string Version = "(\d+\.\d+\.\d+)"').Groups[1].Value
    $preview = [regex]::Match($info, 'const int PreviewNumber = (\d+)').Groups[1].Value
    if (!$version -or !$preview) { throw 'Invalid package version in PluginInfo.cs' }
    $outDir = Join-Path $repoRoot "src/GenesisUI/bin/$Channel/net48"
    $dll = Join-Path $outDir 'GenesisUI.dll'
    $stampJson = dotnet run --project tools/review --no-restore -- stamp $repoRoot $dll
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect compiled package stamp' }
    $stamp = $stampJson | ConvertFrom-Json
    $expectedSha = (git rev-parse --short=7 HEAD).Trim()
    if ($stamp.sha -ne $expectedSha -or $stamp.version -ne $version -or [string]$stamp.preview -ne $preview -or $stamp.channel -ne $Channel) { throw 'Compiled package source/version/channel mismatch' }
    $dllData = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($dll))
    if (!$dllData.Contains('FaultRegistry') -or !$dllData.Contains('SendConfigsAfterLogin')) { throw 'Core or ServerSync was not merged into GenesisUI.dll' }
    $jotunnData = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes((Join-Path $repoRoot 'ref/Jotunn.dll')))
    if (!$jotunnData.Contains($jotunnVersion)) { throw "ref/Jotunn.dll is not Jotunn $jotunnVersion" }

    $icon = [IO.File]::ReadAllBytes((Join-Path $repoRoot 'icon.png'))
    if ($icon.Length -lt 24) { throw 'Invalid icon.png' }
    $width = [int]$icon[16] * 16777216 + [int]$icon[17] * 65536 + [int]$icon[18] * 256 + [int]$icon[19]
    $height = [int]$icon[20] * 16777216 + [int]$icon[21] * 65536 + [int]$icon[22] * 256 + [int]$icon[23]
    if ($width -ne 256 -or $height -ne 256) { throw "icon.png is ${width}x${height}; Hexium requires 256x256" }
    $fontsDir = Join-Path $outDir 'fonts'
    if (@(Get-ChildItem -LiteralPath $fontsDir -Filter '*.ttf' -File).Count -ne 6) { throw 'Expected 6 fonts in the build output' }
    $artDir = Join-Path $outDir 'art'
    $art = Get-Content -LiteralPath (Join-Path $artDir 'sprites.json') -Raw | ConvertFrom-Json
    $entries = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    $entries.Add('plugins/GenesisUI.dll', $dll)
    $entries.Add('icon.png', (Join-Path $repoRoot 'icon.png'))
    $entries.Add('CHANGELOG.md', (Join-Path $repoRoot 'CHANGELOG.md'))
    $entries.Add('LICENSE', (Join-Path $repoRoot 'LICENSE'))
    $entries.Add('README.md', (Join-Path $repoRoot 'store/README.md'))
    $entries.Add('plugins/art/sprites.json', (Join-Path $artDir 'sprites.json'))
    foreach ($sprite in $art.sprites) {
        $file = [string]$sprite.file
        if (!$file -or [IO.Path]::GetFileName($file) -ne $file -or $file.Contains('/') -or $file.Contains('\')) { throw "Sprite file must be a filename: $file" }
        $entries["plugins/art/$file"] = Join-Path $artDir $file
    }
    $bundle = Join-Path $artDir 'genesisui.shaders'
    & (Join-Path $PSScriptRoot 'shaders/provenance.ps1') Verify $bundle
    $entries.Add('plugins/art/genesisui.shaders', $bundle)
    $entries.Add('plugins/art/shader-provenance.json', (Join-Path $repoRoot 'art/shaders/provenance.json'))
    foreach ($folder in @('Translations', 'fonts')) {
        $source = if ($folder -eq 'Translations') { Join-Path $repoRoot 'src/GenesisUI/Translations' } else { $fontsDir }
        foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse) {
            $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\', '/')
            $entries.Add("plugins/$folder/$relative", $file.FullName)
        }
    }
    foreach ($source in $entries.Values) {
        if (!(Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing package file: $source" }
    }

    $manifest = [ordered]@{
        name = 'GenesisUI'
        version_number = $version
        website_url = 'https://github.com/brkihel/GenesisUI'
        description = "Valheim's whole interface redrawn in fine gold metal: living health bars, a clear inventory and crafting, a framed map with new markers. Turn any part off; it repairs itself if something breaks."
        dependencies = @("ValheimModding-Jotunn-$jotunnVersion")
    } | ConvertTo-Json
    $references = [ordered]@{}
    foreach ($reference in @('assembly_valheim', 'assembly_utils', 'assembly_guiutils', 'gui_framework', 'Jotunn')) {
        $references["$reference.dll"] = (Get-FileHash -LiteralPath (Join-Path $repoRoot "ref/$reference.dll")).Hash
    }
    $fileHashes = [ordered]@{}
    foreach ($entry in $entries.GetEnumerator() | Sort-Object Key) { $fileHashes[$entry.Key] = (Get-FileHash -LiteralPath $entry.Value).Hash }
    $evidence = [ordered]@{ schema = 1; commit = (git rev-parse HEAD).Trim(); channel = $Channel; version = $version; preview = $preview; references = $references; filesSha256 = $fileHashes } | ConvertTo-Json -Depth 6
    $generated = [ordered]@{ 'manifest.json' = $manifest; 'build-evidence.json' = $evidence }
    $name = "GenesisMods-GenesisUI-$version"
    if ($Channel -eq 'Preview') { $name += "-preview.$preview" }
    $dist = Join-Path $repoRoot 'dist'
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    $zipPath = [IO.Path]::GetFullPath((Join-Path $dist "$name.zip"))
    if (!$zipPath.StartsWith($dist + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package path is outside dist/' }
    $stream = [IO.File]::Open($zipPath, [IO.FileMode]::Create)
    $zip = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $entries.GetEnumerator() | Sort-Object Key) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entry.Value, $entry.Key, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        foreach ($name in $generated.Keys) {
            $writer = [IO.StreamWriter]::new($zip.CreateEntry($name).Open(), [Text.UTF8Encoding]::new($false))
            try { $writer.Write($generated[$name]) } finally { $writer.Dispose() }
        }
    } finally { $zip.Dispose(); $stream.Dispose() }

    $check = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        if ($check.Entries.Count -ne $entries.Count + $generated.Count) { throw 'Package file count mismatch' }
        foreach ($name in $entries.Keys) {
            $entry = $check.GetEntry($name)
            if ($null -eq $entry -or $entry.Length -ne (Get-Item -LiteralPath $entries[$name]).Length) { throw "Package entry mismatch: $name" }
            $entryStream = $entry.Open()
            $hasher = [Security.Cryptography.SHA256]::Create()
            try { $actualHash = [Convert]::ToHexString($hasher.ComputeHash($entryStream)) } finally { $entryStream.Dispose(); $hasher.Dispose() }
            if ($actualHash -ne (Get-FileHash -LiteralPath $entries[$name] -Algorithm SHA256).Hash) { throw "Package content hash mismatch: $name" }
        }
        $reader = [IO.StreamReader]::new($check.GetEntry('manifest.json').Open())
        try { $actualText = $reader.ReadToEnd(); $actual = $actualText | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($actualText -cne $manifest) { throw 'Package manifest content mismatch' }
        if ($actual.version_number -ne $version -or $actual.dependencies[0] -ne "ValheimModding-Jotunn-$jotunnVersion") { throw 'Package manifest mismatch' }
        foreach ($name in $generated.Keys) {
            $entryStream = $check.GetEntry($name).Open(); $hasher = [Security.Cryptography.SHA256]::Create()
            try {
                $actualHash = [Convert]::ToHexString($hasher.ComputeHash($entryStream))
                $expectedHash = [Convert]::ToHexString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($generated[$name])))
                if ($actualHash -ne $expectedHash) { throw "Generated entry hash mismatch: $name" }
            } finally { $entryStream.Dispose(); $hasher.Dispose() }
        }
    } finally { $check.Dispose() }
    [pscustomobject]@{
        Package = $zipPath
        Bytes = (Get-Item -LiteralPath $zipPath).Length
        Files = $entries.Count + $generated.Count
        Sprites = $art.sprites.Count
        SHA256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    } | Format-List
} finally { Pop-Location }
