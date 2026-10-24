# Read-only path/hash helpers reused from V3; no V3 campaign is invoked.
. (Join-Path $PSScriptRoot '../Dragon-v3/common.ps1')
function Assert-V4Inputs([string]$Root,[string]$CoreEvidence){
    $paths=@{
        catalog='Client/Assets/Game/Data/Generated/mvp-catalog.json'
        art='Client/Assets/Game/Art/Presentation/manifest.json'
        baseline='Planning/Evidence/Dragon-v3/dragon-v3-candidate-05/1280x720/fresh/expected-catalog.json'
        operation='Client/Logs/dragon-v4-operation.json'
    }
    $result=@{}
    foreach($key in $paths.Keys){
        $path=Assert-V3Child (Join-Path $Root $paths[$key]) $Root
        if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing final V4 input: $path"}
        $result[$key]=$path
    }
    $core=Assert-V3Child $CoreEvidence $Root
    if(-not (Test-Path -LiteralPath $core -PathType Leaf)){throw 'Handed-over Client Core result is required; this runner never reruns Core tests'}
    $result.core=$core
    $catalog=Get-Content -LiteralPath $result.catalog -Raw|ConvertFrom-Json
    if($catalog.dataVersion -ne 'mvp1.16.0' -or $catalog.mvpLastStage -ne 20){throw 'V4 generated catalog version/scope mismatch'}
    foreach($id in @('on','dam','rin')){
        $character=@($catalog.characters|Where-Object id -eq $id)
        $skill=@($catalog.skills|Where-Object characterId -eq $id)
        if($character.Count -ne 1 -or -not $character[0].availableInMvp -or $skill.Count -ne 1 -or $skill[0].cooldownMs -ne 20000){throw "V4 character/skill contract: $id"}
    }
    if((@($catalog.stories.stage|Sort-Object)-join ',') -ne '2,7,10,15,20'){throw 'V4 encounter stages mismatch'}
    foreach($pair in @(@(10,3),@(20,6))){
        $story=@($catalog.stories|Where-Object stage -eq $pair[0])
        if($story.Count -ne 1 -or @($story[0].lines).Count -ne $pair[1]){throw "V4 story page count: $($pair[0])"}
    }
    $baseline=Get-Content -LiteralPath $result.baseline -Raw|ConvertFrom-Json
    if((ConvertTo-Json -InputObject $catalog.combat -Depth 50 -Compress) -ne (ConvertTo-Json -InputObject $baseline.combat -Depth 50 -Compress)){throw 'Existing combat rules/timing changed from V3 baseline'}
    $result.hashes=@{}
    foreach($key in @('catalog','art','baseline','operation','core')){$result.hashes[$key]=(Get-FileHash -LiteralPath $result[$key] -Algorithm SHA256).Hash}
    return $result
}
