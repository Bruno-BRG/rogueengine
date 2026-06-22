# Installs the portable RogueEngine build from dist/engine into %LOCALAPPDATA%\RogueEngine.
# Run from repo root after scripts/publish-dist.ps1

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Source = Join-Path $RepoRoot "dist\engine"
$Target = Join-Path $env:LOCALAPPDATA "RogueEngine"
$ToolsTarget = Join-Path $Target "tools"

if (-not (Test-Path (Join-Path $Source "Editor\RogueEngine.Editor.exe"))) {
    Write-Error "Missing dist/engine. Run: powershell -File scripts/publish-dist.ps1"
}

Write-Host "Installing RogueEngine to $Target"
if (Test-Path $Target) {
    Remove-Item $Target -Recurse -Force
}
New-Item -ItemType Directory -Path $Target -Force | Out-Null
Copy-Item -Path (Join-Path $Source "*") -Destination $Target -Recurse -Force

$UserPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($UserPath -notlike "*$ToolsTarget*") {
    $NewPath = if ([string]::IsNullOrWhiteSpace($UserPath)) { $ToolsTarget } else { "$UserPath;$ToolsTarget" }
    [Environment]::SetEnvironmentVariable("Path", $NewPath, "User")
    Write-Host "Added $ToolsTarget to user PATH (restart terminal to use rogueengine.exe globally)."
}

$Desktop = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = Join-Path $Desktop "RogueEngine Editor.lnk"
$Wsh = New-Object -ComObject WScript.Shell
$Shortcut = $Wsh.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Join-Path $Target "Editor\RogueEngine.Editor.exe"
$Shortcut.WorkingDirectory = Join-Path $Target "Editor"
$Shortcut.Description = "RogueEngine Editor"
$Shortcut.Save()

Write-Host "Shortcut created: $ShortcutPath"
Write-Host "Launch editor: $Target\Editor\RogueEngine.Editor.exe"
Write-Host "Build games: rogueengine build path\to\game.reproj"
