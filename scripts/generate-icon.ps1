# Generates the Window Groups application icon (app_icon.ico) as a multi-size
# (16, 20, 24, 32, 40, 48, 64, 128, 256 px) PNG-compressed ICO file.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts\generate-icon.ps1

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$outputPath = Join-Path (Split-Path $PSScriptRoot -Parent) "src\WindowGroups\app_icon.ico"
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256

function New-RoundRectPath([System.Drawing.RectangleF]$rect, [single]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [single]($radius * 2)
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90) | Out-Null
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90) | Out-Null
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90) | Out-Null
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90) | Out-Null
    $path.CloseFigure()
    return $path
}

function Draw-IconBitmap([int]$side) {
    $scale = [single]($side / 256.0)

    $bmp = New-Object System.Drawing.Bitmap($side, $side, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Background rounded square.
    $bgX = [single](8  * $scale)
    $bgY = [single](8  * $scale)
    $bgW = [single](240 * $scale)
    $bgH = [single](240 * $scale)
    $bgRect = [System.Drawing.RectangleF]::new($bgX, $bgY, $bgW, $bgH)
    $bgPath = New-RoundRectPath $bgRect ([single](52 * $scale))
    $bgBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 30, 42, 71))
    $g.FillPath($bgBrush, $bgPath)
    $bgBrush.Dispose()
    $bgPath.Dispose()

    # Four grouped "window" tiles in a 2x2 grid.
    $tileBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 96, 165, 250))
    $gap = [single](14 * $scale)
    $pad = [single](34 * $scale)
    $tileRadius = [single](12 * $scale)
    $tileW = ($bgW - $pad * 2 - $gap) / 2
    $tileH = ($bgH - $pad * 2 - $gap) / 2

    foreach ($row in 0, 1) {
        foreach ($col in 0, 1) {
            $x = $bgX + $pad + ($col * ($tileW + $gap))
            $y = $bgY + $pad + ($row * ($tileH + $gap))
            $r = [System.Drawing.RectangleF]::new($x, $y, $tileW, $tileH)
            $p = New-RoundRectPath $r $tileRadius
            $g.FillPath($tileBrush, $p)
            $p.Dispose()
        }
    }

    $tileBrush.Dispose()
    $g.Dispose()
    return $bmp
}

function Get-PngBytes([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return $ms.ToArray()
}

# --- Assemble ICO -------------------------------------------------------
$entries = @()
foreach ($side in $sizes) {
    $bmp = Draw-IconBitmap $side
    $entries += ,@{
        Width  = if ($side -eq 256) { [byte]0 } else { [byte]$side }
        Height = if ($side -eq 256) { [byte]0 } else { [byte]$side }
        Data   = Get-PngBytes $bmp
    }
    $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($out)

$w.Write([uint16]0)
$w.Write([uint16]1)
$w.Write([uint16]$entries.Count)

$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $w.Write([byte]$e.Width)
    $w.Write([byte]$e.Height)
    $w.Write([byte]0)
    $w.Write([byte]0)
    $w.Write([uint16]1)
    $w.Write([uint16]32)
    $w.Write([uint32]$e.Data.Length)
    $w.Write([uint32]$offset)
    $offset += $e.Data.Length
}

foreach ($e in $entries) {
    $w.Write([byte[]]$e.Data)
}

$w.Flush()
[System.IO.File]::WriteAllBytes($outputPath, $out.ToArray())
$w.Dispose()
$out.Dispose()

Write-Host "Wrote $outputPath ($($entries.Count) sizes)"