Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\src\ViewportCap.App\Assets\app.ico'
New-Item -ItemType Directory -Path (Split-Path $out) -Force | Out-Null

$bitmap = New-Object System.Drawing.Bitmap 32, 32
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$background = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 17, 24, 39))
$border = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 96, 165, 250)), 2

$graphics.FillRectangle($background, 0, 0, 32, 32)
$graphics.DrawRectangle($border, 7, 8, 18, 16)
$graphics.FillEllipse([System.Drawing.Brushes]::White, 14, 14, 4, 4)

$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = [System.IO.File]::Create($out)
$icon.Save($stream)
$stream.Close()

$graphics.Dispose()
$bitmap.Dispose()
$icon.Dispose()
