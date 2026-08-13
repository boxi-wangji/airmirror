Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$iconPath = Join-Path $PSScriptRoot 'AirMirror.ico'
$wizardImagePath = Join-Path $projectRoot 'installer\airmirror-wizard.bmp'
$wizardSmallImagePath = Join-Path $projectRoot 'installer\airmirror-wizard-small.bmp'

function New-LogoBitmap([int]$Size) {
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $padding = [int]($Size * 0.055)
        $bounds = [System.Drawing.Rectangle]::new($padding, $padding, $Size - ($padding * 2), $Size - ($padding * 2))
        $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            $bounds,
            [System.Drawing.Color]::FromArgb(32, 213, 245),
            [System.Drawing.Color]::FromArgb(16, 45, 132),
            45)
        try {
            $graphics.FillEllipse($background, $bounds)
        }
        finally {
            $background.Dispose()
        }

        $white = [System.Drawing.Pens]::White
        $screenWidth = [int]($Size * 0.58)
        $screenHeight = [int]($Size * 0.39)
        $screenX = [int](($Size - $screenWidth) / 2)
        $screenY = [int]($Size * 0.25)
        $stroke = [int]($Size * 0.055)
        $screenPen = [System.Drawing.Pen]::new($white.Color, $stroke)
        $screenPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $graphics.DrawRectangle($screenPen, $screenX, $screenY, $screenWidth, $screenHeight)
            $standY = $screenY + $screenHeight
            $graphics.DrawLine($screenPen, [int]($Size * 0.38), [int]($Size * 0.74), [int]($Size * 0.62), [int]($Size * 0.74))
            $graphics.DrawLine($screenPen, [int]($Size * 0.5), $standY, [int]($Size * 0.5), [int]($Size * 0.74))
        }
        finally {
            $screenPen.Dispose()
        }

        $castPen = [System.Drawing.Pen]::new($white.Color, [int]($Size * 0.047))
        $castPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $castPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        try {
            $originX = [int]($Size * 0.32)
            $originY = [int]($Size * 0.55)
            $dotSize = [int]($Size * 0.065)
            $graphics.FillEllipse($white.Brush, $originX - [int]($dotSize / 2), $originY - [int]($dotSize / 2), $dotSize, $dotSize)
            $graphics.DrawArc($castPen, $originX - [int]($Size * 0.02), $originY - [int]($Size * 0.15), [int]($Size * 0.28), [int]($Size * 0.30), 270, 90)
            $graphics.DrawArc($castPen, $originX - [int]($Size * 0.02), $originY - [int]($Size * 0.25), [int]($Size * 0.46), [int]($Size * 0.50), 270, 90)
        }
        finally {
            $castPen.Dispose()
        }

        return $bitmap
    }
    finally {
        $graphics.Dispose()
    }
}

function Save-Icon([System.Drawing.Bitmap]$Bitmap, [string]$Path) {
    $iconHandle = $Bitmap.GetHicon()
    try {
        $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create)
            try {
                $icon.Save($stream)
            }
            finally {
                $stream.Dispose()
            }
        }
        finally {
            $icon.Dispose()
        }
    }
    finally {
        $null = [AirMirrorNative]::DestroyIcon($iconHandle)
    }
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
try {
    Save-Icon $iconBitmap $iconPath
}
finally {
    $iconBitmap.Dispose()
}

$banner = [System.Drawing.Bitmap]::new(164, 314)
$bannerGraphics = [System.Drawing.Graphics]::FromImage($banner)
try {
    $bannerGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $bannerGraphics.Clear([System.Drawing.Color]::FromArgb(9, 20, 45))
    $bannerLogo = New-LogoBitmap 124
    try {
        $bannerGraphics.DrawImage($bannerLogo, 20, 34, 124, 124)
    }
    finally {
        $bannerLogo.Dispose()
    }
    $titleFont = [System.Drawing.Font]::new('Microsoft YaHei UI', 15, [System.Drawing.FontStyle]::Bold)
    $subtitleFont = [System.Drawing.Font]::new('Microsoft YaHei UI', 9, [System.Drawing.FontStyle]::Regular)
    $subtitleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(154, 218, 245))
    $centeredText = [System.Drawing.StringFormat]::new()
    $centeredText.Alignment = [System.Drawing.StringAlignment]::Center
    try {
        $bannerGraphics.DrawString('AirMirror', $titleFont, [System.Drawing.Brushes]::White, [System.Drawing.RectangleF]::new(2, 177, 160, 28), $centeredText)
        $bannerGraphics.DrawString('iPhone 屏幕镜像', $subtitleFont, $subtitleBrush, [System.Drawing.RectangleF]::new(2, 212, 160, 24), $centeredText)
    }
    finally {
        $titleFont.Dispose()
        $subtitleFont.Dispose()
        $subtitleBrush.Dispose()
        $centeredText.Dispose()
    }
    $banner.Save($wizardImagePath, [System.Drawing.Imaging.ImageFormat]::Bmp)
}
finally {
    $bannerGraphics.Dispose()
    $banner.Dispose()
}

$small = [System.Drawing.Bitmap]::new(55, 55)
$smallGraphics = [System.Drawing.Graphics]::FromImage($small)
try {
    $smallGraphics.Clear([System.Drawing.Color]::FromArgb(9, 20, 45))
    $smallLogo = New-LogoBitmap 49
    try {
        $smallGraphics.DrawImage($smallLogo, 3, 3, 49, 49)
    }
    finally {
        $smallLogo.Dispose()
    }
    $small.Save($wizardSmallImagePath, [System.Drawing.Imaging.ImageFormat]::Bmp)
}
finally {
    $smallGraphics.Dispose()
    $small.Dispose()
}
