# Reuse path/hash helpers only. Dot-sourcing this file does not launch a campaign.
. (Join-Path $PSScriptRoot '../Dragon-v3/common.ps1')
function Assert-V6Inputs([string]$Root,[string]$FreezeManifest,[string]$CoreEvidence,[string]$IntegrationEvidence,[string]$LegacySave,[string]$RuntimeHash){
    $result=@{}
    foreach($pair in @(@('freeze',$FreezeManifest),@('core',$CoreEvidence),@('integration',$IntegrationEvidence))){
        $path=Assert-V3Child $pair[1] $Root
        if(-not (Test-Path -LiteralPath $path -PathType Leaf)){throw "Missing handed-over input: $path"}
        $result[$pair[0]]=$path
    }
    $freeze=Get-Content -LiteralPath $result.freeze -Raw|ConvertFrom-Json
    if($freeze.sourceFrozen -ne $true -or $freeze.version -ne '0.9.5' -or $freeze.dataVersion -ne 'mvp1.18.0' -or $freeze.runtimeSha256 -ne $RuntimeHash){throw 'Final v6 source/build freeze does not match this invocation'}
    $required=@('Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.cs','Client/Assets/Game/Runtime/Bootstrap/DragonThemeSmokeProbe.V6.cs','Client/Assets/Game/Data/Generated/mvp-catalog.json','Tools/QA/Dragon-v6/common.ps1','Tools/QA/Dragon-v3/common.ps1','Tools/QA/Dragon-v6/run-campaign.ps1','Tools/QA/Dragon-v6/analyze.py')
    $seen=@{}
    foreach($entry in $freeze.files){
        if(-not $entry.path -or $entry.sha256 -notmatch '^[a-fA-F0-9]{64}$' -or $seen.ContainsKey($entry.path)){throw 'Invalid/duplicate freeze file entry'}
        $path=Assert-V3Child (Join-Path $Root $entry.path) $Root
        if(-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256){throw "Source changed after freeze: $($entry.path)"}
        $seen[$entry.path]=$true
    }
    foreach($path in $required){if(-not $seen.ContainsKey($path)){throw "Freeze must include $path"}}
    $result.catalog=Assert-V3Child (Join-Path $Root 'Client/Assets/Game/Data/Generated/mvp-catalog.json') $Root
    $catalog=Get-Content -LiteralPath $result.catalog -Raw|ConvertFrom-Json
    if($catalog.dataVersion -ne 'mvp1.18.0' -or $catalog.mvpLastStage -ne 20 -or @($catalog.companions).Count -ne 5){throw 'V6 catalog contract mismatch'}
    # Only old QA evidence is allowed here. Never accept an arbitrary/default profile path.
    $result.legacy=Assert-V3Child $LegacySave (Join-Path $Root 'Planning/Evidence/Dragon-v5')
    $envelope=Get-Content -LiteralPath $result.legacy -Raw|ConvertFrom-Json
    $sha=[Security.Cryptography.SHA256]::Create()
    try{$digest=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($envelope.payload))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
    $payload=$envelope.payload|ConvertFrom-Json
    if($envelope.envelopeVersion -ne 1 -or $digest -ne $envelope.sha256 -or $payload.schemaVersion -ne 9){throw 'Prior QA seed must have a valid schema9 envelope'}
    $result.hashes=@{}
    foreach($key in @('freeze','core','integration','catalog','legacy')){$result.hashes[$key]=(Get-FileHash -LiteralPath $result[$key] -Algorithm SHA256).Hash}
    return $result
}
