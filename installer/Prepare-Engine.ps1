$ErrorActionPreference = 'Stop'

$sourceRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $sourceRoot
$buildRoot = Join-Path $workspaceRoot '构建'
$engineDirectory = Join-Path $buildRoot '运行环境\engine'
$engineExecutable = Join-Path $engineDirectory 'uxplay-windows.exe'
$downloadDirectory = Join-Path $buildRoot '临时\下载'
$archivePath = Join-Path $downloadDirectory 'uxplay-windows-2.0.0.1736.zip'
$downloadUrl = 'https://github.com/leapbtw/uxplay-windows/releases/download/2.0.0.1736/uxplay-windows.zip'
$expectedSha256 = '9D3A51C15FC9DB857351195E7EB7BBB21700D9AE25D936A54BCF8536B62CCA18'

if (Test-Path -LiteralPath $engineExecutable) {
    Write-Output 'UxPlay Windows runtime is already present.'
    return
}

New-Item -ItemType Directory -Path $downloadDirectory -Force | Out-Null
Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

$actualSha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
if ($actualSha256 -ne $expectedSha256) {
    throw "UxPlay archive hash verification failed: $actualSha256"
}

New-Item -ItemType Directory -Path $engineDirectory -Force | Out-Null
Expand-Archive -LiteralPath $archivePath -DestinationPath $engineDirectory -Force

if (-not (Test-Path -LiteralPath $engineExecutable)) {
    throw 'UxPlay archive did not contain engine\\uxplay-windows.exe as expected.'
}

Write-Output 'UxPlay Windows runtime downloaded and verified.'
