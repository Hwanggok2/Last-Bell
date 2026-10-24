param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][ValidatePattern('^dragon-v4-[A-Za-z0-9_.-]+$')][string]$CandidateId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-fA-F0-9]{64}$')][string]$ExpectedRuntimeSha256,
    [Parameter(Mandatory=$true)][string]$CoreEvidence,
    [string]$Continue1280From,
    [switch]$Run
)
$ErrorActionPreference='Stop'
if(-not $Run){throw 'Prepared only. Await final V4 source freeze, observed build and execution ownership handoff.'}
. (Join-Path $PSScriptRoot 'common.ps1')
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$build=Join-Path $root 'Builds/Dragon-v4-QA'
$exe=Assert-V3Child $Executable $build
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw 'V4 observed executable missing'}
$runtime=Join-Path $build (([IO.Path]::GetFileNameWithoutExtension($exe))+'_Data/Managed/LastBell.Runtime.dll')
Assert-V3NoLinks $runtime
if(-not (Test-Path -LiteralPath $runtime -PathType Leaf) -or (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash -ne $ExpectedRuntimeSha256){throw 'V4 Runtime hash differs from coordinator handoff'}
$output=Assert-V3Child (Join-Path $root "Planning/Evidence/Dragon-v4/$CandidateId") (Join-Path $root 'Planning/Evidence/Dragon-v4')
if(Test-Path -LiteralPath $output){throw 'Existing candidate evidence must not be overwritten'}
$inputs4=Assert-V4Inputs $root $CoreEvidence
$continuation=$null
if($Continue1280From){
    $priorRoot=Assert-V3Child $Continue1280From (Join-Path $root 'Planning/Evidence/Dragon-v4')
    $priorFresh=Assert-V3Child (Join-Path $priorRoot '1280x720/fresh') $priorRoot
    $priorResult=Get-Content -LiteralPath (Join-Path $priorFresh 'result.json') -Raw|ConvertFrom-Json
    if($priorResult.passed -or $priorResult.error -notlike '*file restore preserves selected skill clocks: dam*'){throw 'Continuation requires the recorded Daren JSON-duration assertion failure'}
    foreach($pair in @(@('catalog','expected-catalog.json'),@('art','art-manifest.json'))){
        $old=Assert-V3Child (Join-Path $priorFresh $pair[1]) $priorRoot
        if((Get-FileHash -LiteralPath $old -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $inputs4[$pair[0]] -Algorithm SHA256).Hash){throw 'Continuation catalog/art changed; reassess adopted evidence before running'}
    }
    $priorSave=Assert-V3Child (Join-Path $priorFresh 'profiles/boot/last-bell-save.json') $priorRoot
    $envelope=Get-Content -LiteralPath $priorSave -Raw|ConvertFrom-Json
    $sha=[Security.Cryptography.SHA256]::Create()
    try{$payloadHash=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($envelope.payload))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
    if($payloadHash -ne $envelope.sha256){throw 'Prior QA continuation save checksum mismatch'}
    $continuation=@{sourceCandidate=$priorRoot;save=$priorSave;saveSha256=(Get-FileHash -LiteralPath $priorSave -Algorithm SHA256).Hash;mode='v4-tail';adopted='Passed Luka and on/dam selection from candidate01; rerun Daren restore boundary and unrun tail only'}
}
New-Item -ItemType Directory -Path $output|Out-Null
$protected=Join-Path $output 'protected-legacy-fixture'
New-Item -ItemType Directory -Path $protected|Out-Null
[IO.File]::WriteAllText((Join-Path $protected 'last-bell-save.json'),'Synthetic QA sentinel only; no actual user profile')
[IO.File]::WriteAllText((Join-Path $protected 'last-bell-save.json.bak'),'Synthetic QA backup sentinel')
$before=Get-V3SaveManifest $protected
$beforeJson=ConvertTo-Json -InputObject $before -Depth 100 -Compress
Write-V3Json (Join-Path $output 'protected-saves-before.json') $before
function Scoped-Names($Key){@($Key.GetValueNames()|Where-Object {$_ -like 'LastBell.*' -or $_ -like 'Screenmanager*' -or $_ -like 'UnitySelectMonitor*'})}
$prefs=@()
foreach($path in @('Software\LastBellPrototype\Last Bell MVP','Software\Unity\UnityEditor\LastBellPrototype\Last Bell MVP')){
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path);$values=@()
    if($key){foreach($name in (Scoped-Names $key)){$values+=@{name=$name;kind=$key.GetValueKind($name).ToString();value=$key.GetValue($name)}}}
    $prefs+=@{path=$path;exists=($null -ne $key);values=$values};if($key){$key.Close()}
}
Write-V3Json (Join-Path $output 'playerprefs-before.json') $prefs
$files=@(Get-ChildItem -LiteralPath $build -Recurse -File|ForEach-Object {
    Assert-V3NoLinks $_.FullName
    @{path=$_.FullName.Substring($build.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length}
})
Write-V3Json (Join-Path $output 'candidate-manifest.json') @{candidate=$CandidateId;exe=$exe;version='0.9.3';dataVersion='mvp1.16.0';runtimeSha256=$ExpectedRuntimeSha256;createdAt=[DateTimeOffset]::Now.ToString('o');inputs=$inputs4;files=$files;continuation=$continuation}
Copy-Item -LiteralPath $inputs4.core -Destination (Join-Path $output ('client-core-evidence'+[IO.Path]::GetExtension($inputs4.core)))
Copy-Item -LiteralPath $inputs4.operation -Destination (Join-Path $output 'integration-operation.json')
$runs=[Collections.Generic.List[object]]::new();$process=$null
function Assert-Protected([string]$Label){
    $after=Get-V3SaveManifest $protected
    $matches=(ConvertTo-Json -InputObject $after -Depth 100 -Compress) -eq $beforeJson
    Write-V3Json (Join-Path $output "protected-saves-$Label.json") @{matches=$matches;manifest=$after}
    if(-not $matches){throw 'Synthetic protected fixture changed; preserve evidence without overwriting it'}
}
function Invoke-V4Case([int]$Width,[int]$Height,[bool]$Resume){
    $caseRoot=Join-Path $output "$Width`x$Height";$fresh=Join-Path $caseRoot 'fresh'
    $dest=if($Resume){$caseRoot}else{$fresh};$save=Join-Path $fresh 'profiles/boot'
    $mode=if($Resume){'v4-resume'}elseif($Width -eq 1280 -and $continuation){'v4-tail'}else{'v4'};$label="$mode-$Width`x$Height"
    if($Resume){Copy-Item -LiteralPath (Join-Path $fresh 'v4-resume-expected.json') -Destination (Join-Path $dest 'v4-resume-expected.json')}
    else{
        New-Item -ItemType Directory -Path $fresh|Out-Null
        Copy-Item -LiteralPath $inputs4.catalog -Destination (Join-Path $fresh 'expected-catalog.json')
        Copy-Item -LiteralPath $inputs4.art -Destination (Join-Path $fresh 'art-manifest.json')
        Copy-Item -LiteralPath $inputs4.baseline -Destination (Join-Path $fresh 'timing-baseline-catalog.json')
        if($mode -eq 'v4-tail'){
            $source=Join-Path $fresh 'continuation-source';New-Item -ItemType Directory -Path $source|Out-Null
            Copy-Item -LiteralPath $continuation.save -Destination (Join-Path $source 'last-bell-save.json')
            Write-V3Json (Join-Path $fresh 'continuation.json') $continuation
        }
    }
    $argsList=@('-dragon-theme-smoke',('"'+$dest+'"'),'-dragon-theme-case',$mode,'-lastbell-save-dir',('"'+$save+'"'),
        '-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',('"'+(Join-Path $dest 'player.log')+'"'))
    Assert-Protected "before-$label"
    $started=[DateTimeOffset]::Now;$timedOut=$false
    $script:process=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
    Write-Output "STARTED $label pid=$($script:process.Id)"
    while(-not $script:process.WaitForExit(1000)){
        if(([DateTimeOffset]::Now-$started).TotalSeconds -gt 630){$timedOut=$true;$script:process.Kill();$script:process.WaitForExit();break}
    }
    $script:process.Refresh();$exitCode=$script:process.ExitCode;$script:process.Dispose();$script:process=$null
    $resultPath=Join-Path $dest 'result.json';$result=if(Test-Path -LiteralPath $resultPath){Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json}else{$null}
    $ok=$null -ne $result -and $result.completed -and $result.passed -and $exitCode -eq 0 -and -not $timedOut
    $runs.Add(@{name=$label;mode=$mode;output=$dest;save=$save;width=$Width;height=$Height;exitCode=$exitCode;timedOut=$timedOut;passed=$ok;arguments=$argsList;startedAt=$started.ToString('o');endedAt=[DateTimeOffset]::Now.ToString('o')})
    Write-V3Json (Join-Path $output 'processes.json') $runs.ToArray();Assert-Protected "after-$label"
    Write-Output "FINISHED $label exit=$exitCode passed=$ok"
    if(-not $ok){throw "V4 case failed: $label; remaining cases not started"}
}
$campaignError=$null;$restored=@()
try{
    foreach($size in @(@(1280,720),@(640,360))){Invoke-V4Case $size[0] $size[1] $false;Invoke-V4Case $size[0] $size[1] $true}
}catch{$campaignError=$_.Exception.Message}
finally{
    if($process){if(-not $process.HasExited){$process.Kill();$process.WaitForExit()};$process.Dispose()}
    foreach($item in $prefs){
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($item.path,$true)
        if(-not $key -and $item.exists){$key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($item.path)}
        if($key){
            foreach($name in (Scoped-Names $key)){if($name -notin @($item.values.name)){$key.DeleteValue($name,$false)}}
            foreach($entry in $item.values){
                $value=$entry.value;if($entry.kind -eq 'DWord'){$value=[int]$value};if($entry.kind -eq 'QWord'){$value=[long]$value};if($entry.kind -eq 'Binary'){$value=[byte[]]$value}
                $key.SetValue($entry.name,$value,[Enum]::Parse([Microsoft.Win32.RegistryValueKind],$entry.kind))
            }
            $names=Scoped-Names $key;$ok=$names.Count -eq @($item.values).Count
            foreach($entry in $item.values){if($entry.name -notin $names -or $key.GetValueKind($entry.name).ToString() -ne $entry.kind -or (ConvertTo-Json -InputObject $key.GetValue($entry.name) -Compress) -ne (ConvertTo-Json -InputObject $entry.value -Compress)){$ok=$false}}
            $key.Close()
        }else{$ok=@($item.values).Count -eq 0}
        $restored+=@{path=$item.path;matches=$ok}
    }
    Write-V3Json (Join-Path $output 'playerprefs-restored.json') $restored
    try{Assert-Protected 'final'}catch{$campaignError=$_.Exception.Message}
    if($restored.matches -contains $false){$campaignError='Scoped PlayerPrefs restore mismatch'}
    $passed=-not $campaignError -and $runs.Count -eq 4 -and @($runs|Where-Object {-not $_.passed}).Count -eq 0
    Write-V3Json (Join-Path $output 'campaign.json') @{passed=$passed;error=$campaignError;runs=$runs.ToArray();expectedProcesses=4;continuation=$continuation;osInput='DEFERRED';visualReview='NOT_RUN';audioListening='NOT_RUN';userDefaultProfile='NOT_ACCESSED';core='Client evidence reused, not executed';limits='Synthetic pointer/raycast paths, isolated save fixtures and native resume. With continuation, prior passed Luka/selection evidence is adopted after identity checks; the 1280 tail is not a full fresh run. Optional source-filter PCM is not device loopback or listening. Manual visual and audio review are separate.'}
}
if(-not $passed){throw "V4 campaign failed: $campaignError"}
