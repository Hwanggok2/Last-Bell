$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;using System.Drawing;
public static class PngInspection {
 public static int[] Inspect(string file){using(var b=new Bitmap(file)){
  int left=b.Width,top=b.Height,right=-1,bottom=-1,opaque=0,edges=0,transparent=0;
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
   int alpha=b.GetPixel(x,y).A;if(alpha==0)transparent++;
   if(alpha>32){opaque++;left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);if(x==0||y==0||x==b.Width-1||y==b.Height-1)edges++;}
  }
  return new int[]{b.Width,b.Height,left,top,right,bottom,opaque,edges,transparent};
 }}
}
'@
$delivery=Get-Content -LiteralPath (Join-Path $root 'delivery.json') -Raw | ConvertFrom-Json
$inputs=Get-Content -LiteralPath (Join-Path $root 'inputs.json') -Raw | ConvertFrom-Json
$report=@()
foreach($input in $inputs){$hash=(Get-FileHash -LiteralPath (Join-Path $root $input.file)).Hash.ToLower();if($hash -ne $input.sha256){throw "Changed input $($input.id)"}}
foreach($actor in $delivery.actors){
 if(($actor.clips.motion | Sort-Object) -join ',' -ne 'Attack,Hit,Stun'){throw "Invalid states $($actor.id)"}
 if(($actor.pivot -join ',') -ne '78,114' -or $actor.referenceHeightPixels -ne 92){throw 'Registration contract changed'}
 $baseHash=(Get-FileHash -LiteralPath (Join-Path $root $actor.base)).Hash
 foreach($clip in $actor.clips){
  if($clip.motion -eq 'Hit'){if($clip.effect -ne 'whiteFlash' -or $clip.holdMs -ne 45 -or $clip.fadeMs -ne 75){throw 'Hit contract'};continue}
  if($clip.frames.Count -ne $clip.frameDurationsMs.Count -or $clip.frames.Count -lt 2){throw 'Frame/timing mismatch'}
  if(@($clip.frameDurationsMs | Where-Object {$_ -le 0}).Count){throw 'Non-positive duration'}
  if($clip.motion -eq 'Attack'){
   if($clip.loop -or $clip.contactFrame -lt 0 -or $clip.contactFrame -ge $clip.frames.Count){throw 'Attack contract'}
   foreach($end in @($clip.frames[0],$clip.frames[-1])){if((Get-FileHash -LiteralPath (Join-Path $root $end)).Hash -ne $baseHash){throw 'Attack endpoint differs from base'}}
  }elseif(!$clip.loop -or $clip.contactFrame -ne -1){throw 'Stun contract'}
  for($i=0;$i -lt $clip.frames.Count;$i++){
   $file=$clip.frames[$i];$m=[PngInspection]::Inspect((Join-Path $root $file))
   if($m[0] -ne 128 -or $m[1] -ne 128 -or $m[6] -eq 0 -or $m[8] -eq 0){throw "Invalid PNG $file"}
   if($m[7] -gt 0){throw "Opaque artwork touches frame edge: $file"}
   $report += [ordered]@{actor=$actor.id;motion=$clip.motion;index=$i;file=$file;size=@($m[0],$m[1]);opaqueBounds=@($m[2],$m[3],$m[4],$m[5]);edgePixels=$m[7];durationMs=$clip.frameDurationsMs[$i]}
  }
 }
}
$result=[ordered]@{passed=$true;actorCount=$delivery.actors.Count;checked='Input hashes; state/pivot/timing/contact contracts; PNG dimensions, transparency and clipping; exact base endpoints. This does not establish visual quality.';frames=$report}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $root 'Review\technical-checks.json') -Encoding UTF8
Write-Output ('Technical checks passed for '+$delivery.actors.Count+' actors, '+$report.Count+' selected frames. Visual approval is recorded separately.')
