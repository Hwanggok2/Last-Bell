$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$delivery=Get-Content -LiteralPath (Join-Path $root 'delivery.json') -Raw | ConvertFrom-Json
$actors=$delivery.actors
$cellWidth=416; $cellHeight=190
$bmp=[Drawing.Bitmap]::new(832,([Math]::Ceiling($actors.Count/2)*$cellHeight))
$g=[Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::FromArgb(32,43,57))
$font=[Drawing.Font]::new('Consolas',12)
$small=[Drawing.Font]::new('Consolas',9)
$brush=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(235,225,198))
for($i=0;$i -lt $actors.Count;$i++){
 $a=$actors[$i]; $x=($i%2)*$cellWidth; $y=[Math]::Floor($i/2)*$cellHeight
 $attack=$a.clips | Where-Object motion -eq 'Attack'
 $stun=$a.clips | Where-Object motion -eq 'Stun'
 $paths=@($a.base,$attack.frames[$attack.contactFrame],$stun.frames[0])
 $labels=@('Base',"Attack [$($attack.contactFrame)]",'Stun')
 $g.DrawString($a.id,$font,$brush,($x+16),($y+8))
 for($k=0;$k -lt 3;$k++){
  $src=[Drawing.Bitmap]::new((Join-Path $root $paths[$k]))
  $g.DrawImageUnscaled($src,($x+16+$k*128),($y+30))
  $src.Dispose()
  $g.DrawString($labels[$k],$small,$brush,($x+24+$k*128),($y+164))
 }
}
$bmp.Save((Join-Path $root 'overview.png'),[Drawing.Imaging.ImageFormat]::Png)
$brush.Dispose();$font.Dispose();$small.Dispose();$g.Dispose();$bmp.Dispose()
Write-Output "Overview: $($actors.Count) reviewed actors"
