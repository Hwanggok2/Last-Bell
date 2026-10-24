$ErrorActionPreference='Stop'
function Write-V3Json([string]$Path,$Value){
    [IO.File]::WriteAllText($Path,(ConvertTo-Json -InputObject $Value -Depth 100),[Text.UTF8Encoding]::new($false))
}
function Assert-V3NoLinks([string]$Path){
    $item=[IO.FileInfo]::new([IO.Path]::GetFullPath($Path))
    if($item.Exists -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Linked file: $Path"}
    $dir=[IO.DirectoryInfo]::new([IO.Path]::GetFullPath($Path))
    while($dir){
        if($dir.Exists -and ($dir.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Linked directory: $($dir.FullName)"}
        $dir=$dir.Parent
    }
}
function Assert-V3Child([string]$Path,[string]$Parent){
    $full=[IO.Path]::GetFullPath($Path)
    $prefix=[IO.Path]::GetFullPath($Parent).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    if(-not $full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw "Path is outside intended root: $full"}
    Assert-V3NoLinks $full
    return $full
}
function Get-V3SaveManifest([string]$Directory){
    Assert-V3NoLinks $Directory
    $files=@()
    if(Test-Path -LiteralPath $Directory){
        # Reject linked descendants before recursing into them.
        $pending=[Collections.Generic.Queue[string]]::new();$pending.Enqueue($Directory)
        while($pending.Count){
            foreach($entry in (Get-ChildItem -LiteralPath $pending.Dequeue() -Force)){
                if($entry.Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Linked entry in protected save directory: $($entry.FullName)"}
                if($entry.PSIsContainer){$pending.Enqueue($entry.FullName)}else{
                    $files+=@{path=$entry.FullName.Substring($Directory.TrimEnd('\','/').Length+1);bytes=$entry.Length;
                        sha256=(Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash;lastWriteUtc=$entry.LastWriteTimeUtc.ToString('o')}
                }
            }
        }
    }
    return @{directory=$Directory;exists=(Test-Path -LiteralPath $Directory);files=@($files|Sort-Object { $_.path })}
}
function Assert-V3Inputs([string]$Root){
    $catalogPath=Join-Path $Root 'Client/Assets/Game/Data/Generated/mvp-catalog.json'
    $bindingPath=Join-Path $Root 'Planning/Production/dragon-v3-data-bindings.json'
    $artPath=Join-Path $Root 'Client/Assets/Game/Art/Presentation/manifest.json'
    $baselinePath=Join-Path $Root 'Planning/Production/dragon-v3-baseline.json'
    $catalog=Get-Content -LiteralPath $catalogPath -Raw|ConvertFrom-Json
    $bindings=Get-Content -LiteralPath $bindingPath -Raw|ConvertFrom-Json
    $art=Get-Content -LiteralPath $artPath -Raw|ConvertFrom-Json
    $baseline=Get-Content -LiteralPath $baselinePath -Raw|ConvertFrom-Json
    if($catalog.dataVersion -ne 'mvp1.15.0' -or $bindings.skillsImplemented -ne $false -or $bindings.playableStages -ne 20){throw 'V3 version/scope handoff mismatch'}
    if(@($bindings.mapping).Count -ne 10 -or (@($bindings.mapping.stage|Sort-Object)-join ',') -ne '11,12,13,14,15,16,17,18,19,20'){throw 'Need one binding for every stage 11 through 20'}
    foreach($domain in $bindings.protectedDomains){
        if((ConvertTo-Json -InputObject $catalog.$domain -Depth 100 -Compress) -ne (ConvertTo-Json -InputObject $baseline.catalog.$domain -Depth 100 -Compress)){throw "Protected catalog domain changed: $domain"}
    }
    foreach($mapping in $bindings.mapping){
        $stage=@($catalog.stages|Where-Object number -eq $mapping.stage)
        $enemy=@($catalog.enemies|Where-Object id -eq $mapping.stableEnemyId)
        if($stage.Count -ne 1 -or $enemy.Count -ne 1 -or $stage[0].enemyId -ne $mapping.stableEnemyId -or $enemy[0].artId -ne $mapping.artId -or $enemy[0].displayName -ne $mapping.displayName){throw "Stage binding mismatch: $($mapping.stage)"}
    }
    $ids=@($bindings.mapping.artId)+@(($catalog.characters|Where-Object id -eq 'on').artId)
    $checked=@{}
    foreach($id in $ids){
        $actors=@($art.actors|Where-Object id -eq $id)
        if($actors.Count -ne 1){throw "Final imported art missing or duplicated: $id"}
        $actor=$actors[0]
        foreach($required in @('Idle','Attack')){if($required -notin $actor.clips.motion){throw "Missing $id/$required"}}
        if($id -in $bindings.mapping.artId -and 'Stun' -notin $actor.clips.motion){throw "Missing $id/Stun"}
        foreach($clip in $actor.clips){
            if(@($clip.frames).Count -eq 0){throw "Empty final clip: $id/$($clip.motion)"}
            foreach($frame in $clip.frames){
                if($checked.ContainsKey($frame)){continue}
                $textures=@($art.textures|Where-Object path -eq $frame)
                if($textures.Count -ne 1){throw "Manifest texture missing or duplicated: $frame"}
                $path=Assert-V3Child (Join-Path (Join-Path $Root 'Client') $frame) (Join-Path $Root 'Client/Assets')
                if(-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $textures[0].sha256){throw "Manifest texture hash mismatch: $frame"}
                $checked[$frame]=$true
            }
        }
    }
    return @{catalog=$catalogPath;bindings=$bindingPath;art=$artPath;baseline=$baselinePath;actors=$ids;textures=$checked.Count}
}
