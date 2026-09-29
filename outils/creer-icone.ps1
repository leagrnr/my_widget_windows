Add-Type -AssemblyName System.Drawing

function Arrondi([float]$x, [float]$y, [float]$l, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $l - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $l - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

$images = foreach ($s in 16, 24, 32, 48, 256) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'

    $fond = Arrondi 0 0 $s $s ($s * 0.22)
    $degrade = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point $s, $s), ([System.Drawing.Color]::FromArgb(79, 140, 255)), ([System.Drawing.Color]::FromArgb(123, 92, 255))
    $g.FillPath($degrade, $fond)

    $marge = $s * 0.2; $ecart = $s * 0.08; $t = ($s - 2 * $marge - $ecart) / 2
    $couleurs = @([System.Drawing.Color]::FromArgb(240, 255, 255, 255), [System.Drawing.Color]::FromArgb(255, 255, 224, 102),
                  [System.Drawing.Color]::FromArgb(240, 255, 255, 255), [System.Drawing.Color]::FromArgb(240, 255, 255, 255))
    for ($i = 0; $i -lt 4; $i++) {
        $x = $marge + ($i % 2) * ($t + $ecart); $y = $marge + [math]::Floor($i / 2) * ($t + $ecart)
        $g.FillPath((New-Object System.Drawing.SolidBrush $couleurs[$i]), (Arrondi $x $y $t $t ([math]::Max(1, $t * 0.22))))
    }
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    [pscustomobject]@{ Taille = $s; Octets = $ms.ToArray() }
}

$sortie = Join-Path $PSScriptRoot "..\app.ico"
$f = New-Object System.IO.BinaryWriter ([System.IO.File]::Create($sortie))
$f.Write([uint16]0); $f.Write([uint16]1); $f.Write([uint16]$images.Count)
$decalage = 6 + 16 * $images.Count
foreach ($im in $images) {
    $cote = if ($im.Taille -ge 256) { 0 } else { $im.Taille }
    $f.Write([byte]$cote); $f.Write([byte]$cote); $f.Write([byte]0); $f.Write([byte]0)
    $f.Write([uint16]1); $f.Write([uint16]32)
    $f.Write([uint32]$im.Octets.Length); $f.Write([uint32]$decalage)
    $decalage += $im.Octets.Length
}
foreach ($im in $images) { $f.Write($im.Octets) }
$f.Close()
Write-Host "Icône créée : $sortie"
