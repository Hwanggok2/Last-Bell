$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName PresentationCore
$outputDir = Split-Path $PSScriptRoot -Parent
$framesDir = Join-Path $outputDir 'frames'
New-Item -ItemType Directory -Path $framesDir -Force | Out-Null
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives -TypeDefinition @'
using System;
using System.Drawing;
public static class SpriteMetrics {
    public static int[] Bounds(Bitmap image, Rectangle region) {
        int left=region.Right, top=region.Bottom, right=region.Left, bottom=region.Top;
        for(int y=region.Top; y<region.Bottom; y++)
        for(int x=region.Left; x<region.Right; x++) {
            if(image.GetPixel(x,y).A<128) continue;
            left=Math.Min(left,x); top=Math.Min(top,y);
            right=Math.Max(right,x); bottom=Math.Max(bottom,y);
        }
        return new int[]{left,top,right+1,bottom+1};
    }
    public static double[] Eye(Bitmap image, int[] bounds) {
        double sumX=0, sumY=0; int count=0;
        for(int y=bounds[1]; y<bounds[1]+(bounds[3]-bounds[1])*0.44; y++)
        for(int x=bounds[0]+(int)((bounds[2]-bounds[0])*0.6); x<bounds[2]; x++) {
            Color c=image.GetPixel(x,y);
            if(c.A>200 && c.R>190 && c.G>100 && c.B<85) { sumX+=x; sumY+=y; count++; }
        }
        if(count==0) throw new Exception("No golden eye detected");
        return new double[]{sumX/count,sumY/count};
    }
}
'@
function New-Canvas([int]$width,[int]$height) {
    return [System.Drawing.Bitmap]::new($width,$height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}
function New-PixelGraphics($bitmap) {
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    return $graphics
}
$sheet = [System.Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'generated-sheet.png'))
$nativeSheet = New-Canvas 1024 128
$sheetGraphics = New-PixelGraphics $nativeSheet
$contact = New-Canvas 1024 592
$contactGraphics = New-PixelGraphics $contact
$background = [System.Drawing.Color]::FromArgb(255,53,62,74)
$contactGraphics.Clear($background)
$font = [System.Drawing.Font]::new('Segoe UI',11)
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255,221,227,235))
$gif = [System.Windows.Media.Imaging.GifBitmapEncoder]::new()
$durations = @(40,40,60,60,40,40,60,60)
$phases = @('CONTACT','DRIVE','FLIGHT','RECOVER','CONTACT','DRIVE','FLIGHT','RECOVER')
$frameData = @()
for ($i=0; $i -lt 8; $i++) {
    $col = $i % 4; $row = [math]::Floor($i/4)
    $x0 = [int][math]::Floor($col*$sheet.Width/4)
    $x1 = [int][math]::Floor(($col+1)*$sheet.Width/4)
    $y0 = [int][math]::Floor($row*$sheet.Height/2)
    $y1 = [int][math]::Floor(($row+1)*$sheet.Height/2)
    $region = [System.Drawing.Rectangle]::new($x0,$y0,$x1-$x0,$y1-$y0)
    $bounds = [SpriteMetrics]::Bounds($sheet,$region)
    $eye = [SpriteMetrics]::Eye($sheet,$bounds)
    $x0 = [math]::Max($x0,$bounds[0]-2)
    $y0 = [math]::Max($y0,$bounds[1]-2)
    $x1 = [math]::Min($x1,$bounds[2]+2)
    $y1 = [math]::Min($y1,$bounds[3]+2)
    $region = [System.Drawing.Rectangle]::new($x0,$y0,$x1-$x0,$y1-$y0)
    $scale = 0.25
    $eyeHeight = @(64,63,62,63)[$i % 4]
    $destX = [int][math]::Round(88-($eye[0]-$x0)*$scale)
    $destY = [int][math]::Round($eyeHeight-($eye[1]-$y0)*$scale)
    $crop = $sheet.Clone($region,$sheet.PixelFormat)
    $frame = New-Canvas 128 128
    $graphics = New-PixelGraphics $frame
    $dest = [System.Drawing.Rectangle]::new($destX,$destY,[int][math]::Round($region.Width*$scale),[int][math]::Round($region.Height*$scale))
    $graphics.DrawImage($crop,$dest,0,0,$crop.Width,$crop.Height,[System.Drawing.GraphicsUnit]::Pixel)
    $graphics.Dispose(); $crop.Dispose()
    $name = 'run-{0:d2}.png' -f ($i+1)
    $framePath = Join-Path $framesDir $name
    $frame.Save($framePath,[System.Drawing.Imaging.ImageFormat]::Png)
    $sheetGraphics.DrawImageUnscaled($frame,$i*128,0)
    $contactGraphics.DrawImage($frame,([int]$col*256),([int]$row*296),256,256)
    $contactGraphics.DrawString(('{0:d2}  {1}  /  {2} ms' -f ($i+1),$phases[$i],$durations[$i]),$font,$brush,([single]($col*256+22)),([single]($row*296+261)))
    $preview = New-Canvas 512 512
    $previewGraphics = New-PixelGraphics $preview
    $previewGraphics.Clear($background)
    $previewGraphics.DrawImage($frame,0,0,512,512)
    $previewGraphics.Dispose()
    $pngStream = [System.IO.MemoryStream]::new()
    $preview.Save($pngStream,[System.Drawing.Imaging.ImageFormat]::Png)
    $pngStream.Position=0
    $bitmapFrame = [System.Windows.Media.Imaging.BitmapFrame]::Create($pngStream,[System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,[System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    $metadata = [System.Windows.Media.Imaging.BitmapMetadata]::new('gif')
    $metadata.SetQuery('/grctlext/Delay',[uint16]($durations[$i]/10))
    $metadata.SetQuery('/grctlext/Disposal',[byte]2)
    if($i -eq 0) {
        $metadata.SetQuery('/appext/Application',[System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'))
        $metadata.SetQuery('/appext/Data',[byte[]]@(3,1,0,0,0))
    }
    $gif.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmapFrame,$null,$metadata,$null))
    $frameData += [ordered]@{file="frames/$name"; durationMs=$durations[$i]; phase=$phases[$i]; data='data:image/png;base64,'+[Convert]::ToBase64String([System.IO.File]::ReadAllBytes($framePath))}
    $pngStream.Dispose(); $preview.Dispose(); $frame.Dispose()
}
$gifStream = [System.IO.File]::Create((Join-Path $outputDir 'run-preview.gif'))
$gif.Save($gifStream); $gifStream.Dispose()
node (Join-Path $PSScriptRoot 'set-gif-timing.cjs')
$nativeSheet.Save((Join-Path $outputDir 'run-sheet.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$contact.Save((Join-Path $outputDir 'contact-sheet.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$sheetGraphics.Dispose(); $contactGraphics.Dispose(); $nativeSheet.Dispose(); $contact.Dispose(); $sheet.Dispose(); $font.Dispose(); $brush.Dispose()
$frameJson = ConvertTo-Json -InputObject $frameData -Depth 5 -Compress
$html = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'preview-template.html') -Raw
$html.Replace('__FRAMES__',$frameJson) | Set-Content -LiteralPath (Join-Path $outputDir 'preview.html') -Encoding UTF8
$manifest = [ordered]@{name='Low explosive forward sprint'; reference='../character-base.png'; frameSize=@(128,128); frameCount=8; cycleMs=400; loop=$true; generator='Built-in image_gen'; frames=@($frameData | ForEach-Object { [ordered]@{file=$_.file; durationMs=$_.durationMs; phase=$_.phase} })}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputDir 'animation.json') -Encoding UTF8
Write-Output ('Saved 8 PNG frames, GIF, HTML player, sprite sheet, and contact sheet in '+$outputDir)
