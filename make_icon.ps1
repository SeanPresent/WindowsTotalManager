# WinTotal app icon generator — black rounded square + blue→purple gradient bolt
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object Drawing.Bitmap $size, $size
$g = [Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([Drawing.Color]::Transparent)

# rounded rectangle (black background + subtle border)
function RoundRect([Drawing.Graphics]$gr, [int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, $r*2, $r*2, 180, 90)
    $p.AddArc($x+$w-$r*2, $y, $r*2, $r*2, 270, 90)
    $p.AddArc($x+$w-$r*2, $y+$h-$r*2, $r*2, $r*2, 0, 90)
    $p.AddArc($x, $y+$h-$r*2, $r*2, $r*2, 90, 90)
    $p.CloseFigure()
    return $p
}

$bgPath = RoundRect $g 8 8 240 240 56
$bgBrush = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 10, 10, 12))
$g.FillPath($bgBrush, $bgPath)
$pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 40, 40, 48)), 3
$g.DrawPath($pen, $bgPath)

# lightning bolt polygon (blue → purple gradient)
$bolt = @(
    (New-Object Drawing.PointF 148, 34),
    (New-Object Drawing.PointF 78, 142),
    (New-Object Drawing.PointF 122, 142),
    (New-Object Drawing.PointF 104, 224),
    (New-Object Drawing.PointF 182, 110),
    (New-Object Drawing.PointF 134, 110)
)
$gradBrush = New-Object Drawing.Drawing2D.LinearGradientBrush(
    (New-Object Drawing.PointF 78, 34),
    (New-Object Drawing.PointF 182, 224),
    [Drawing.Color]::FromArgb(255, 10, 132, 255),
    [Drawing.Color]::FromArgb(255, 191, 90, 242))
$g.FillPolygon($gradBrush, $bolt)

$g.Dispose()

# wrap the PNG in an ICO container (single 256px entry)
$ms = New-Object IO.MemoryStream
$bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()
$ms.Dispose()
$bmp.Dispose()

$ico = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $ico
$w.Write([uint16]0)      # reserved
$w.Write([uint16]1)      # type: icon
$w.Write([uint16]1)      # count
$w.Write([byte]0)        # width 256
$w.Write([byte]0)        # height 256
$w.Write([byte]0)        # palette
$w.Write([byte]0)        # reserved
$w.Write([uint16]1)      # planes
$w.Write([uint16]32)     # bpp
$w.Write([uint32]$png.Length)
$w.Write([uint32]22)     # offset
$w.Write($png)
$w.Flush()
[IO.File]::WriteAllBytes((Join-Path $PSScriptRoot "icon.ico"), $ico.ToArray())
$w.Dispose()
Write-Host "icon.ico created"
