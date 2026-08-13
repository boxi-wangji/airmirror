Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$sourceRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = Split-Path -Parent $sourceRoot
$buildRoot = Join-Path $workspaceRoot '构建'
$brandDirectory = Join-Path $buildRoot '临时\品牌素材'
$logoSourcePath = Join-Path $PSScriptRoot 'airmirror-logo.svg'
$iconPath = Join-Path $brandDirectory 'AirMirror.ico'
$splashImagePath = Join-Path $brandDirectory 'AirMirror-Setup.png'
$renderPath = Join-Path $brandDirectory 'airmirror-logo-render.png'
$profileDirectory = Join-Path $brandDirectory 'edge-profile'

New-Item -ItemType Directory -Path $brandDirectory -Force | Out-Null

function Get-EdgeExecutable {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft\Edge\Application\msedge.exe')
    )

    return $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

function New-LogoBitmap([int]$Size) {
    $edge = Get-EdgeExecutable
    if ($null -eq $edge) {
        throw '未找到 Microsoft Edge，无法从 SVG 生成 AirMirror 图标素材。'
    }

    Remove-Item -LiteralPath $renderPath -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $profileDirectory | Out-Null
    $sourceUrl = 'file:///' + $logoSourcePath.Replace('\', '/')
    $arguments = @(
        '--headless=new',
        '--disable-gpu',
        '--no-sandbox',
        '--hide-scrollbars',
        '--window-size=512,512',
        "--user-data-dir=$profileDirectory",
        "--screenshot=$renderPath",
        $sourceUrl
    )
    $edgeProcess = Start-Process -FilePath $edge -ArgumentList $arguments -PassThru
    $edgeProcess.WaitForExit()
    if (-not (Test-Path -LiteralPath $renderPath)) {
        throw 'AirMirror SVG Logo 渲染失败。'
    }

    $source = [System.Drawing.Bitmap]::new($renderPath)
    try {
        # Edge 截图会给 SVG 外侧加纯白底；转成透明，保留圆形 Logo。
        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = 0; $x -lt $source.Width; $x++) {
                $pixel = $source.GetPixel($x, $y)
                if ($pixel.R -gt 252 -and $pixel.G -gt 252 -and $pixel.B -gt 252) {
                    $source.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, $pixel.R, $pixel.G, $pixel.B))
                }
            }
        }

        $result = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($result)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.DrawImage($source, 0, 0, $Size, $Size)
        }
        finally {
            $graphics.Dispose()
        }

        # 只保留 SVG 中的圆形徽章，避免浏览器截图的画布底色进入图标。
        $center = ($Size - 1) / 2
        $radius = $Size * 0.456
        for ($y = 0; $y -lt $Size; $y++) {
            for ($x = 0; $x -lt $Size; $x++) {
                $distance = [Math]::Sqrt((($x - $center) * ($x - $center)) + (($y - $center) * ($y - $center)))
                if ($distance -gt $radius) {
                    $pixel = $result.GetPixel($x, $y)
                    $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, $pixel.R, $pixel.G, $pixel.B))
                }
            }
        }

        return $result
    }
    finally {
        $source.Dispose()
    }
}

function Save-Icon([System.Drawing.Bitmap]$Bitmap, [string]$Path) {
    $iconHandle = $Bitmap.GetHicon()
    try {
        $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create)
            try { $icon.Save($stream) }
            finally { $stream.Dispose() }
        }
        finally { $icon.Dispose() }
    }
    finally { $null = [AirMirrorNative]::DestroyIcon($iconHandle) }
}

if (-not ('AirMirrorNative' -as [type])) {
    Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class AirMirrorNative {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
'@
}

$iconBitmap = New-LogoBitmap 256
try { Save-Icon $iconBitmap $iconPath }
finally { $iconBitmap.Dispose() }

$banner = [System.Drawing.Bitmap]::new(960, 540)
$bannerGraphics = [System.Drawing.Graphics]::FromImage($banner)
try {
    $bannerGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $bannerGraphics.Clear([System.Drawing.Color]::FromArgb(9, 20, 45))
    $bannerLogo = New-LogoBitmap 240
    $titleFont = [System.Drawing.Font]::new('Microsoft YaHei UI', 42, [System.Drawing.FontStyle]::Bold)
    $subtitleFont = [System.Drawing.Font]::new('Microsoft YaHei UI', 20, [System.Drawing.FontStyle]::Regular)
    $subtitleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(154, 218, 245))
    $centeredText = [System.Drawing.StringFormat]::new()
    $centeredText.Alignment = [System.Drawing.StringAlignment]::Center
    try {
        $bannerGraphics.DrawImage($bannerLogo, 360, 72, 240, 240)
        $bannerGraphics.DrawString('AirMirror', $titleFont, [System.Drawing.Brushes]::White, [System.Drawing.RectangleF]::new(0, 320, 960, 90), $centeredText)
        $bannerGraphics.DrawString('iPhone 屏幕镜像', $subtitleFont, $subtitleBrush, [System.Drawing.RectangleF]::new(0, 415, 960, 60), $centeredText)
    }
    finally {
        $titleFont.Dispose(); $subtitleFont.Dispose(); $subtitleBrush.Dispose(); $centeredText.Dispose()
    }
    $bannerLogo.Dispose()
    $banner.Save($splashImagePath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $bannerGraphics.Dispose(); $banner.Dispose() }
