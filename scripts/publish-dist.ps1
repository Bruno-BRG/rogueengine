# Publishes a portable RogueEngine distribution and an example game export.
# Output: dist/engine/ (editor + build tool + runtime template) and dist/games/RpgDemo/

$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$DistRoot = Join-Path $RepoRoot "dist"
$EngineRoot = Join-Path $DistRoot "engine"
$TemplateDir = Join-Path $EngineRoot "sdk\runtime-template"
$EditorDir = Join-Path $EngineRoot "Editor"
$ToolsDir = Join-Path $EngineRoot "tools"
$GamesRoot = Join-Path $DistRoot "games"
$RpgDemoOut = Join-Path $GamesRoot "RpgDemo"
$RpgDemoReproj = Join-Path $RepoRoot "templates\RpgDemoProject\game.reproj"

$PublishArgs = @(
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=false"
)

Write-Host "==> Cleaning dist/"
if (Test-Path $DistRoot) {
    Remove-Item $DistRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $TemplateDir -Force | Out-Null
New-Item -ItemType Directory -Path $EditorDir -Force | Out-Null
New-Item -ItemType Directory -Path $ToolsDir -Force | Out-Null
New-Item -ItemType Directory -Path $RpgDemoOut -Force | Out-Null

Write-Host "==> Publishing runtime template"
dotnet publish (Join-Path $RepoRoot "src\RogueEngine.Runtime\RogueEngine.Runtime.csproj") `
    @PublishArgs -o $TemplateDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Publishing RogueEngine Editor"
dotnet publish (Join-Path $RepoRoot "src\RogueEngine.Editor\RogueEngine.Editor.csproj") `
    @PublishArgs -o $EditorDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Publishing rogueengine build tool"
dotnet publish (Join-Path $RepoRoot "src\RogueEngine.BuildTool\RogueEngine.BuildTool.csproj") `
    @PublishArgs -o $ToolsDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:ROGUEENGINE_RUNTIME_TEMPLATE = $TemplateDir
Write-Host "==> Exporting example game (RpgDemo)"
dotnet run --project (Join-Path $RepoRoot "src\RogueEngine.BuildTool\RogueEngine.BuildTool.csproj") -c Release -- `
    build $RpgDemoReproj --output $RpgDemoOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Creating ZIP archives"
$EngineZip = Join-Path $DistRoot "RogueEngine-Portable-win-x64.zip"
$GameZip = Join-Path $GamesRoot "RpgDemo-win-x64.zip"
if (Test-Path $EngineZip) { Remove-Item $EngineZip -Force }
if (Test-Path $GameZip) { Remove-Item $GameZip -Force }
Compress-Archive -Path (Join-Path $EngineRoot "*") -DestinationPath $EngineZip
Compress-Archive -Path (Join-Path $RpgDemoOut "*") -DestinationPath $GameZip

@(
    '@echo off',
    'start "" "%~dp0Editor\RogueEngine.Editor.exe" %*'
) | Set-Content -Path (Join-Path $EngineRoot "Launch-Editor.bat") -Encoding ASCII

@(
    "# RogueEngine distribution (generated)",
    "",
    "## Engine (ferramentas)",
    "",
    "- Editor: ``engine/Editor/RogueEngine.Editor.exe`` (ou ``Launch-Editor.bat``)",
    "- Build CLI: ``engine/tools/rogueengine.exe build <game.reproj>``",
    "- ZIP portátil: ``RogueEngine-Portable-win-x64.zip``",
    "",
    "Instalar em ``%LOCALAPPDATA%\\RogueEngine``:",
    "",
    "````powershell",
    "powershell -ExecutionPolicy Bypass -File installer\\install-engine.ps1",
    "````",
    "",
    "## Jogo exemplo exportado",
    "",
    "- Pasta: ``games/RpgDemo/``",
    "- Executar: ``games/RpgDemo/RpgDemo.exe`` (usa ``game.reproj`` na mesma pasta)",
    "- ZIP: ``games/RpgDemo-win-x64.zip``",
    "",
    "Controles: mover, G pickup, U+1-9 usar, E+1-9 equipar, Space interagir."
) | Set-Content -Path (Join-Path $DistRoot "README.md") -Encoding UTF8

Remove-Item Env:ROGUEENGINE_RUNTIME_TEMPLATE -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Done."
Write-Host "  Engine: $EngineRoot"
Write-Host "  Game:   $RpgDemoOut"
Write-Host "  Run game: $RpgDemoOut\RpgDemo.exe"
