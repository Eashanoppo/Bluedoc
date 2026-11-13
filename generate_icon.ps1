Add-Type -AssemblyName System.Drawing

$outputPath = "e:\BlueDoc\src\BlueDoc.UI\BlueDoc.ico"
$sizes = @(16, 32, 48, 64, 128, 256)
$pngList = New-Object System.Collections.ArrayList

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded rectangle background
    $pad = [Math]::Max(1.0, $sz * 0.04)
    $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($sz - 2 * $pad), ($sz - 2 * $pad))
    $radius = $sz * 0.22

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    # Blue gradient
    $c1 = [System.Drawing.Color]::FromArgb(255, 14, 116, 222)
    $c2 = [System.Drawing.Color]::FromArgb(255, 3, 37, 76)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 60.0)
    $g.FillPath($bgBrush, $path)

    # Cyan border
    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 56, 189, 248), [Math]::Max(1.0, $sz * 0.03))
    $g.DrawPath($borderPen, $path)

    # Medical cross accent
    $cx = $sz / 2.0
    $cy = $sz / 2.0
    $crossHalf = $sz * 0.32
    $crossThick = $sz * 0.12
    $crossBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(60, 255, 255, 255))
    $g.FillRectangle($crossBrush, ($cx - $crossThick/2.0), ($cy - $crossHalf), $crossThick, ($crossHalf * 2.0))
    $g.FillRectangle($crossBrush, ($cx - $crossHalf), ($cy - $crossThick/2.0), ($crossHalf * 2.0), $crossThick)

    # Bluetooth Rune symbol
    $btPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [Math]::Max(1.5, $sz * 0.08))
    $btPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $btPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $btPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $topY = $sz * 0.22
    $botY = $sz * 0.78
    $midY = $sz * 0.50
    $spineX = $sz * 0.46
    $wingX = $sz * 0.70
    $leftWingX = $sz * 0.28

    # Vertical spine
    $g.DrawLine($btPen, $spineX, $topY, $spineX, $botY)

    # Upper diagonal loop
    $g.DrawLine($btPen, $spineX, $topY, $wingX, (($topY + $midY) / 2.0))
    $g.DrawLine($btPen, $wingX, (($topY + $midY) / 2.0), $spineX, $midY)

    # Lower diagonal loop
    $g.DrawLine($btPen, $spineX, $midY, $wingX, (($midY + $botY) / 2.0))
    $g.DrawLine($btPen, $wingX, (($midY + $botY) / 2.0), $spineX, $botY)

    # Cross diagonals
    $g.DrawLine($btPen, $leftWingX, ($topY + $sz * 0.13), $wingX, (($midY + $botY) / 2.0))
    $g.DrawLine($btPen, $leftWingX, ($botY - $sz * 0.13), $wingX, (($topY + $midY) / 2.0))

    $g.Flush()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $entry = @{ Size = $sz; Bytes = $ms.ToArray() }
    [void]$pngList.Add($entry)

    $g.Dispose()
    $bmp.Dispose()
    $bgBrush.Dispose()
    $borderPen.Dispose()
    $crossBrush.Dispose()
    $btPen.Dispose()
    $path.Dispose()
}

$fs = [System.IO.File]::Create($outputPath)
$bw = New-Object System.IO.BinaryWriter($fs)

# ICONDIR header
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$pngList.Count)

$offset = 6 + (16 * $pngList.Count)
foreach ($item in $pngList) {
    $sz = $item.Size
    $bytes = $item.Bytes

    $w = if ($sz -eq 256) { [byte]0 } else { [byte]$sz }
    $h = if ($sz -eq 256) { [byte]0 } else { [byte]$sz }

    $bw.Write($w)
    $bw.Write($h)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $bytes.Length
}

foreach ($item in $pngList) {
    $bw.Write($item.Bytes)
}

$bw.Flush()
$bw.Close()
$fs.Close()

Write-Host "Created BlueDoc.ico successfully: $outputPath"
