# Generates the Window Groups application icon (app_icon.ico) as a multi-size
# (16, 24, 32, 48, 64, 128, 256 px) ICO using classic uncompressed 32-bit BGRA
# (BMP) entries, which GDI+ and the Windows tray render reliably.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts\generate-icon.ps1

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$outputPath = Join-Path (Split-Path $PSScriptRoot -Parent) "src\WindowGroups\app_icon.ico"
$sizes = 16, 24, 32, 48, 64, 128, 256

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

    # Background rounded square (dark navy).
    $bgRect = [System.Drawing.RectangleF]::new(
        [single](8 * $scale), [single](8 * $scale),
        [single](240 * $scale), [single](240 * $scale))
    $bgPath = New-RoundRectPath $bgRect ([single](52 * $scale))
    $bgBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 30, 42, 71))
    $g.FillPath($bgBrush, $bgPath)
    $bgBrush.Dispose()
    $bgPath.Dispose()

    # Four grouped "window" tiles in a 2x2 grid (light blue).
    $tileBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 96, 165, 250))
    $gap = [single](14 * $scale)
    $pad = [single](34 * $scale)
    $tileRadius = [single](12 * $scale)
    $tileW = ($bgRect.Width - $pad * 2 - $gap) / 2
    $tileH = ($bgRect.Height - $pad * 2 - $gap) / 2

    foreach ($row in 0, 1) {
        foreach ($col in 0, 1) {
            $x = $bgRect.X + $pad + ($col * ($tileW + $gap))
            $y = $bgRect.Y + $pad + ($row * ($tileH + $gap))
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

# Builds a single classic BMP-format (DIB) icon image: BITMAPINFOHEADER +
# bottom-up 32-bit BGRA pixels + an all-zero AND mask.
function Get-IcoImageBmp([System.Drawing.Bitmap]$bmp) {
    $s = $bmp.Width
    $maskStride = [int](([math]::Floor(($s + 31) / 32)) * 4)
    $xorBytes = $s * 4 * $s
    $andBytes = $maskStride * $s

    $ms = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter($ms)

    $w.Write([uint32]40)
    $w.Write([int32]$s)
    $w.Write([int32]($s * 2))          # height covers XOR + AND planes
    $w.Write([uint16]1)                # planes
    $w.Write([uint16]32)               # bit count
    $w.Write([uint32]0)                # BI_RGB
    $w.Write([uint32]($xorBytes + $andBytes))
    $w.Write([int32]0)
    $w.Write([int32]0)
    $w.Write([uint32]0)
    $w.Write([uint32]0)

    # XOR pixel data, bottom-up, byte order B,G,R,A.
    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($x = 0; $x -lt $s; $x++) {
            $p = $bmp.GetPixel($x, $y)
            $w.Write([byte]$p.B)
            $w.Write([byte]$p.G)
            $w.Write([byte]$p.R)
            $w.Write([byte]$p.A)
        }
    }

    # AND mask (all opaque), bottom-up.
    for ($y = $s - 1; $y -ge 0; $y--) {
        for ($i = 0; $i -lt $maskStride; $i++) {
            $w.Write([byte]0)
        }
    }

    $w.Flush()
    $data = $ms.ToArray()
    $w.Dispose()
    $ms.Dispose()
    return $data
}

# --- Assemble ICO -------------------------------------------------------
$entries = @()
foreach ($side in $sizes) {
    $bmp = Draw-IconBitmap $side
    $entries += ,@{
        Width  = if ($side -eq 256) { [byte]0 } else { [byte]$side }
        Height = if ($side -eq 256) { [byte]0 } else { [byte]$side }
        Data   = Get-IcoImageBmp $bmp
    }
    $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($out)

$w.Write([uint16]0)                  # reserved
$w.Write([uint16]1)                  # type = icon
$w.Write([uint16]$entries.Count)

$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $w.Write([byte]$e.Width)
    $w.Write([byte]$e.Height)
    $w.Write([byte]0)                # color count
    $w.Write([byte]0)                # reserved
    $w.Write([uint16]1)              # planes
    $w.Write([uint16]32)             # bit count
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

# Also export a PNG preview for easy viewing.
$preview = Draw-IconBitmap 256
$preview.Save((Join-Path (Split-Path $outputPath -Parent) "app_icon_preview.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

Write-Host "Wrote $outputPath ($($entries.Count) sizes, BMP-encoded)"