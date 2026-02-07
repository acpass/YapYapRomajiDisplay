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

$files = @(
    $TargetPath,
    (Join-Path $ProjectDir "manifest.json"),
    (Join-Path $ProjectDir "icon.png")
)
# Filter existing files
$validFiles = $files | Where-Object { Test-Path $_ }

Write-Host "Creating zip package: $zipPath"
Compress-Archive -Path $validFiles -DestinationPath $zipPath -Force
