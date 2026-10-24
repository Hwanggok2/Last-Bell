param([Parameter(Mandatory=$true)][string]$Actor)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
foreach($motion in @('Attack','Stun')){
 $dir=Join-Path $root "Actors\$Actor\$motion"
 $files=Get-ChildItem -LiteralPath $dir -Filter 'frame-*.png' | Sort-Object Name
 $sheet=[Drawing.Bitmap]::new(($files.Count*128),128)
 $g=[Drawing.Graphics]::FromImage($sheet)
 for($i=0;$i -lt $files.Count;$i++){$b=[Drawing.Bitmap]::new($files[$i].FullName);$g.DrawImageUnscaled($b,($i*128),0);$b.Dispose()}
 $sheet.Save((Join-Path $root "Actors\$Actor\$motion-sheet.png"),[Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$sheet.Dispose()
 & (Join-Path $PSScriptRoot 'contact-sheet.ps1') -InputDir $dir -Output (Join-Path $root "Actors\$Actor\$motion-contact.png")
}
