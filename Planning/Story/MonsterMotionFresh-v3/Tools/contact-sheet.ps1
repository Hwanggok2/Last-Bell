param([Parameter(Mandatory=$true)][string]$InputDir,[Parameter(Mandatory=$true)][string]$Output,[int]$Columns=5)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$files=Get-ChildItem -LiteralPath $InputDir -Filter '*.png' | Sort-Object Name
if($files.Count -eq 0){throw 'No frames'}
$rows=[int][math]::Ceiling($files.Count/$Columns)
$sheet=[Drawing.Bitmap]::new($Columns*256,$rows*286)
$g=[Drawing.Graphics]::FromImage($sheet)
$g.Clear([Drawing.Color]::FromArgb(51,61,73))
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
$font=[Drawing.Font]::new('Segoe UI',11)
$line=[Drawing.Pen]::new([Drawing.Color]::FromArgb(70,140,160,180))
for($i=0;$i -lt $files.Count;$i++){
 $b=[Drawing.Bitmap]::new($files[$i].FullName)
 $x=($i%$Columns)*256;$y=[int][math]::Floor($i/$Columns)*286
 $g.DrawLine($line,$x,($y+228),($x+255),($y+228))
 $g.DrawImage($b,$x,$y,256,256)
 $g.DrawString(($files[$i].BaseName),$font,[Drawing.Brushes]::White,($x+12),($y+258))
 $b.Dispose()
}
$sheet.Save($Output,[Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$sheet.Dispose();$font.Dispose();$line.Dispose()
Write-Output $Output
