param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'src\AirMirror.csproj'
$publishDirectory = Join-Path $projectRoot 'artifacts\publish'
$installerScript = Join-Path $PSScriptRoot 'AirMirror.iss'
$brandAssetsScript = Join-Path $projectRoot 'assets\Generate-BrandAssets.ps1'
$engineExecutable = Join-Path $projectRoot 'engine\uxplay-windows.exe'
$distributionDirectory = Join-Path $projectRoot 'dist'
$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if ($null -eq $iscc) {
    throw '未找到 Inno Setup 6。请安装后再运行此脚本。'
}

if (-not (Test-Path -LiteralPath $engineExecutable)) {
    & (Join-Path $PSScriptRoot 'Prepare-Engine.ps1')
}

& $brandAssetsScript

# 本地只保留最新安装包；历史版本由发布平台管理，不在源码目录堆积。
Get-ChildItem -LiteralPath $distributionDirectory -Filter 'AirMirror-Setup-*.exe' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

Remove-Item -LiteralPath $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish $projectFile `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw 'AirMirror 发布失败。'
}

$env:AIRMIRROR_VERSION = $Version
& $iscc $installerScript

if ($LASTEXITCODE -ne 0) {
    throw '安装包构建失败。'
}

Write-Output (Join-Path $projectRoot "dist\AirMirror-Setup-$Version.exe")
