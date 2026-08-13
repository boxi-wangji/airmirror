param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $sourceRoot
$buildRoot = Join-Path $workspaceRoot '构建'
$temporaryDirectory = Join-Path $buildRoot '临时'
$runtimeDirectory = Join-Path $buildRoot '运行环境'
$projectFile = Join-Path $sourceRoot 'src\AirMirror.csproj'
$publishDirectory = Join-Path $buildRoot '临时\程序文件'
$releaseDirectory = Join-Path $buildRoot '临时\Velopack'
$engineDirectory = Join-Path $buildRoot '运行环境\engine'
$brandAssetsScript = Join-Path $sourceRoot 'assets\Generate-BrandAssets.ps1'
$brandDirectory = Join-Path $buildRoot '临时\品牌素材'
$distributionDirectory = Join-Path $buildRoot '安装程序'
$installerOutput = Join-Path $distributionDirectory "AirMirror-Setup-$Version.exe"

& (Join-Path $PSScriptRoot 'Prepare-Engine.ps1')
& $brandAssetsScript

New-Item -ItemType Directory -Path $distributionDirectory -Force | Out-Null
Get-ChildItem -LiteralPath $distributionDirectory -Filter 'AirMirror-Setup-*.exe' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

Remove-Item -LiteralPath $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $releaseDirectory -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish $projectFile --configuration Release --runtime win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw 'AirMirror 发布失败。'
}

Copy-Item -LiteralPath $engineDirectory -Destination (Join-Path $publishDirectory 'engine') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot 'README.md') -Destination $publishDirectory -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot '第三方许可.md') -Destination $publishDirectory -Force

$vpkArguments = @(
    'vpk',
    '--version', '1.2.0',
    '--',
    'pack',
    '--packId', 'com.boxiwangji.AirMirror',
    '--packVersion', $Version,
    '--packDir', $publishDirectory,
    '--mainExe', 'AirMirror.exe',
    '--packAuthors', 'boxi-wangji',
    '--packTitle', 'AirMirror',
    '--icon', (Join-Path $brandDirectory 'AirMirror.ico'),
    '--splashImage', (Join-Path $brandDirectory 'AirMirror-Setup.png'),
    '--splashProgressColor', '#12D8F2',
    '--outputDir', $releaseDirectory,
    '--noPortable', 'true'
)

& dnx @vpkArguments

if ($LASTEXITCODE -ne 0) {
    throw 'Velopack 安装包构建失败。'
}

$setup = Get-ChildItem -LiteralPath $releaseDirectory -Filter '*-Setup.exe' -File -Recurse | Select-Object -First 1
if ($null -eq $setup) {
    throw 'Velopack 未生成 Setup.exe。'
}

Copy-Item -LiteralPath $setup.FullName -Destination $installerOutput -Force

if (-not (Test-Path -LiteralPath $installerOutput)) {
    throw '安装包复制失败。'
}

Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $runtimeDirectory -Recurse -Force -ErrorAction SilentlyContinue

Write-Output $installerOutput
