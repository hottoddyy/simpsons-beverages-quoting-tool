param(
    [string]$InstallRoot = "$env:LOCALAPPDATA\Simpsons Beverages\Quoting Tool"
)

$ErrorActionPreference = 'Stop'

$publishRoot = Join-Path $PSScriptRoot 'publish'
$source = Get-ChildItem -LiteralPath $publishRoot -Directory -Filter 'QuotingToolAppSelfContained*' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if ([string]::IsNullOrWhiteSpace($source)) {
    $source = Join-Path $PSScriptRoot 'publish\current'
}
$exeName = 'SimpsonsBeverages.QuotingTool.App.exe'
$sourceExe = Join-Path $source $exeName

if (-not (Test-Path -LiteralPath $sourceExe)) {
    throw "Published app not found: $sourceExe. Run the self-contained publish command first."
}

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null

Copy-Item -Path (Join-Path $source '*') -Destination $InstallRoot -Recurse -Force

$targetExe = Join-Path $InstallRoot $exeName
$desktop = [Environment]::GetFolderPath('DesktopDirectory')
$shortcutPath = Join-Path $desktop 'Simpsons Beverages Quoting Tool.lnk'

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $InstallRoot
$shortcut.Description = 'Simpsons Beverages Quoting Tool'
$iconPath = Join-Path $InstallRoot 'Assets\simpsons-logo.ico'
if (Test-Path -LiteralPath $iconPath) {
    $shortcut.IconLocation = $iconPath
}
$shortcut.Save()

Write-Host "Installed Simpsons Beverages Quoting Tool to:"
Write-Host $InstallRoot
Write-Host "Desktop shortcut:"
Write-Host $shortcutPath
