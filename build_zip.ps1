param(
    [string]$TargetDir, 
    [string]$TargetPath, 
    [string]$ProjectDir
)

$ver = "1.0.0"
$csPath = Join-Path $ProjectDir "RomajiDisplay.cs"

if (Test-Path $csPath) {
    $content = Get-Content $csPath -Raw
    if ($content -match '\[BepInPlugin\("[^"]+", "[^"]+", "([^"]+)"\)\]') {
        $ver = $matches[1]
    }
}

$zipName = "acpass-YapYapRomajiDisplay-$ver.zip"
$zipPath = Join-Path $TargetDir $zipName

# Create a staging directory to organize files exactly how we want them in the zip
$stagingDir = Join-Path $TargetDir "staging_zip"
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir | Out-Null

# 1. DLL
if (Test-Path $TargetPath) {
    Copy-Item -Path $TargetPath -Destination $stagingDir
}

# 2. manifest.json
$manifestPath = Join-Path $ProjectDir "manifest.json"
if (Test-Path $manifestPath) {
    Copy-Item $manifestPath $stagingDir
}

# 3. romaji_mapping.txt
$mappingPath = Join-Path $ProjectDir "romaji_mapping.txt"
if (Test-Path $mappingPath) {
    Copy-Item $mappingPath $stagingDir
}

# 4. ModREADME.md -> README.md
$readmeSrc = Join-Path $ProjectDir "ModREADME.md"
if (Test-Path $readmeSrc) {
    Copy-Item -Path $readmeSrc -Destination (Join-Path $stagingDir "README.md")
}

# 5. assets folder (entire folder, for README images)
$assetsSrc = Join-Path $ProjectDir "assets"
if (Test-Path $assetsSrc) {
    Copy-Item -Path $assetsSrc -Destination $stagingDir -Recurse
}

# 6. icon.png (copy from assets/icon.png to root for Thunderstore/Mod managers)
$iconSrc = Join-Path $assetsSrc "icon.png"
if (Test-Path $iconSrc) {
    Copy-Item -Path $iconSrc -Destination (Join-Path $stagingDir "icon.png")
}

Write-Host "Creating zip package: $zipPath"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -Force

# Cleanup
Remove-Item $stagingDir -Recurse -Force
