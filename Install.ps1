param(
    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley'
)

$ErrorActionPreference = 'Stop'
if (Get-Process StardewModdingAPI, StardewValley -ErrorAction SilentlyContinue) {
    throw 'Close Stardew Valley and SMAPI before installing the release.'
}
if (-not (Test-Path -LiteralPath (Join-Path $GamePath 'StardewModdingAPI.exe'))) {
    throw "SMAPI was not found in $GamePath"
}

$modsPath = Join-Path $GamePath 'Mods'
$projectPath = Join-Path $PSScriptRoot 'LIWI.sln'
& dotnet build $projectPath -c Release --nologo "-p:GamePath=$GamePath"
if ($LASTEXITCODE -ne 0) { throw 'LinkWindow build failed.' }

# Install the optimized local TrueWindow when missing or older; preserve config.json.
$trueWindowRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'TWIN'
$trueWindowManifest = Get-Content -LiteralPath (Join-Path $trueWindowRoot 'src/TrueWindow/manifest.json') -Raw | ConvertFrom-Json
$installedTrueWindowManifestPath = Join-Path $modsPath 'TrueWindow/manifest.json'
$updateTrueWindow = -not (Test-Path -LiteralPath $installedTrueWindowManifestPath)
if (-not $updateTrueWindow) {
    $installedTrueWindowManifest = Get-Content -LiteralPath $installedTrueWindowManifestPath -Raw | ConvertFrom-Json
    $updateTrueWindow = [version]$installedTrueWindowManifest.Version -lt [version]$trueWindowManifest.Version
}
if ($updateTrueWindow) {
    $trueWindowRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'TWIN'
    $trueWindowProject = Join-Path $trueWindowRoot 'src/TrueWindow/TrueWindow.csproj'
    if (-not (Test-Path -LiteralPath $trueWindowProject)) { throw 'Build/install TrueWindow first.' }
    & dotnet build $trueWindowProject -c Release --nologo '-p:EnableModDeploy=false' "-p:GamePath=$GamePath"
    if ($LASTEXITCODE -ne 0) { throw 'TrueWindow build failed.' }
    $trueWindowManifest = Get-Content -LiteralPath (Join-Path $trueWindowRoot 'src/TrueWindow/manifest.json') -Raw | ConvertFrom-Json
    $trueWindowZip = Join-Path $trueWindowRoot "releases/TrueWindow $($trueWindowManifest.Version).zip"
    Expand-Archive -LiteralPath $trueWindowZip -DestinationPath $modsPath -Force
}

$linkManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src/LinkWindow/manifest.json') -Raw | ConvertFrom-Json
Expand-Archive -LiteralPath (Join-Path $PSScriptRoot "releases/LinkWindow $($linkManifest.Version).zip") -DestinationPath $modsPath -Force
Write-Output "Installed LinkWindow to $modsPath\LinkWindow"
Write-Output 'Launch SMAPI, get LinkWindow from the Furniture Catalogue, and configure placed windows with the key set in GMCM (default F6).'

