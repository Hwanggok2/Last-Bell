param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][ValidatePattern('^dragon-v6-[A-Za-z0-9_.-]+$')][string]$CandidateId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-fA-F0-9]{64}$')][string]$ExpectedRuntimeSha256,
    [Parameter(Mandatory=$true)][string]$FreezeManifest,
    [Parameter(Mandatory=$true)][string]$CoreEvidence,
    [Parameter(Mandatory=$true)][string]$IntegrationEvidence,
    [Parameter(Mandatory=$true)][string]$LegacySave,
    [switch]$ContinueAfterCandidate01,
    [switch]$ContinueAfterCandidate02,
    [switch]$ContinueAfterCandidate03,
    [switch]$HealthBarOnly,
    [switch]$Run
)
$ErrorActionPreference='Stop'
if(-not $Run){throw 'Prepared only. Source freeze, final observed build and execution ownership must be handed over before -Run.'}
. (Join-Path $PSScriptRoot 'common.ps1')
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$build=Join-Path $root 'Builds/Dragon-v6-QA'
$exe=Assert-V3Child $Executable $build
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw 'V6 observed executable missing'}
$runtime=Assert-V3Child (Join-Path $build (([IO.Path]::GetFileNameWithoutExtension($exe))+'_Data/Managed/LastBell.Runtime.dll')) $build
if(-not (Test-Path -LiteralPath $runtime -PathType Leaf) -or (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash -ne $ExpectedRuntimeSha256){throw 'V6 Runtime hash differs from handoff'}
$version6=if($HealthBarOnly){'0.9.6'}else{'0.9.5'}
$inputs6=Assert-V6Inputs $root $FreezeManifest $CoreEvidence $IntegrationEvidence $LegacySave $ExpectedRuntimeSha256 $version6
$continuation6=$null;$priorRoot6=$null;$priorRaw6=@()
if(([int]$ContinueAfterCandidate01.IsPresent+[int]$ContinueAfterCandidate02.IsPresent+[int]$ContinueAfterCandidate03.IsPresent+[int]$HealthBarOnly.IsPresent) -gt 1){throw 'Choose one explicit continuation or health-only point'}
if($ContinueAfterCandidate01 -or $ContinueAfterCandidate02 -or $ContinueAfterCandidate03 -or $HealthBarOnly){
    $priorCandidate6=if($HealthBarOnly){'dragon-v6-candidate-04'}elseif($ContinueAfterCandidate03){'dragon-v6-candidate-03'}elseif($ContinueAfterCandidate02){'dragon-v6-candidate-02'}else{'dragon-v6-candidate-01'}
    $priorRoot6=Assert-V3Child (Join-Path $root "Planning/Evidence/Dragon-v6/$priorCandidate6") (Join-Path $root 'Planning/Evidence/Dragon-v6')
    $priorResult6=Assert-V3Child (Join-Path $priorRoot6 '1280x720/fresh/result.json') $priorRoot6
    if($HealthBarOnly){
        $priorResult6=Assert-V3Child (Join-Path $priorRoot6 'campaign.json') $priorRoot6
        $priorResultData6=Get-Content -LiteralPath $priorResult6 -Raw|ConvertFrom-Json
        if(-not $priorResultData6.observationPassed -or @($priorResultData6.runs).Count -ne 4){throw 'HP-only addendum requires preserved completed candidate04'}
    }elseif($ContinueAfterCandidate03){
        $priorResult6=Assert-V3Child (Join-Path $priorRoot6 'campaign.json') $priorRoot6
        $priorResultData6=Get-Content -LiteralPath $priorResult6 -Raw|ConvertFrom-Json
        $operatorStop6=Get-Content -LiteralPath (Assert-V3Child (Join-Path $priorRoot6 'review/operator-stop.json') $priorRoot6) -Raw|ConvertFrom-Json
        if($priorResultData6.observationPassed -or @($priorResultData6.runs).Count -ne 1 -or $priorResultData6.runs[0].mode -ne 'v6-tail' -or $operatorStop6.reason -notlike 'QA6-PROBE-003:*'){throw 'Route continuation requires preserved operator-stopped candidate03'}
    }else{
        $priorResultData6=Get-Content -LiteralPath $priorResult6 -Raw|ConvertFrom-Json
        $stopPoint6=if($ContinueAfterCandidate02){'*actual21particle stars and no old duplicate*'}else{'*V6 repository payload roundtrip player-on-after*'}
        if($priorResultData6.completed -or $priorResultData6.passed -or $priorResultData6.error -notlike $stopPoint6){throw 'Continuation requires the preserved explicit stop point'}
    }
    $priorRaw6=Get-Content -LiteralPath (Assert-V3Child (Join-Path $priorRoot6 'review/raw-manifest.json') $priorRoot6) -Raw|ConvertFrom-Json
    foreach($entry in $priorRaw6){$path=Assert-V3Child (Join-Path $priorRoot6 $entry.path) $priorRoot6;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256){throw "Prior candidate evidence changed: $($entry.path)"}}
    $continuationScope6=if($HealthBarOnly){'0.9.6 HP-only addendum: actual player/normal/boss contact and trail at1280/640. Completed0.9.5 candidate04 reused; no campaign replay.'}elseif($ContinueAfterCandidate03){'1280 route-only retry with correct reward confirmation, then pending/native. Prior visual/stun/concurrent/kill observations reused. All640 cases remain unrun.'}elseif($ContinueAfterCandidate02){'1280 only: normal/boss lightning recheck, active/end stun, unrun concurrent/kill/route/native. Completed player/companion cases reused. All640 cases remain unrun.'}else{'1280 resumes at Luka skill/save regression; completed legacy/inn/HP observations reused. Remaining1280 and all640 cases run once.'}
    $continuation6=@{candidate=$priorCandidate6;resultSha256=(Get-FileHash -LiteralPath $priorResult6 -Algorithm SHA256).Hash;rawFiles=@($priorRaw6).Count;scope=$continuationScope6}
}
$output=Assert-V3Child (Join-Path $root "Planning/Evidence/Dragon-v6/$CandidateId") (Join-Path $root 'Planning/Evidence/Dragon-v6')
if(Test-Path -LiteralPath $output){throw 'Refusing to overwrite existing candidate evidence'}
New-Item -ItemType Directory -Path $output|Out-Null
if($continuation6){Write-V3Json (Join-Path $output 'continuation.json') $continuation6}
$protected=Join-Path $output 'protected-synthetic-fixture';New-Item -ItemType Directory -Path $protected|Out-Null
[IO.File]::WriteAllText((Join-Path $protected 'last-bell-save.json'),'Synthetic QA sentinel. No actual user profile.')
$before=Get-V3SaveManifest $protected;$beforeJson=ConvertTo-Json -InputObject $before -Depth 100 -Compress
Write-V3Json (Join-Path $output 'protected-saves-before.json') $before
function Scoped-Names($Key){@($Key.GetValueNames()|Where-Object {$_ -like 'LastBell.*' -or $_ -like 'Screenmanager*' -or $_ -like 'UnitySelectMonitor*'})}
$prefs=@()
foreach($path in @('Software\LastBellPrototype\Last Bell MVP','Software\Unity\UnityEditor\LastBellPrototype\Last Bell MVP')){
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path);$values=@()
    if($key){foreach($name in (Scoped-Names $key)){$values+=@{name=$name;kind=$key.GetValueKind($name).ToString();value=$key.GetValue($name)}}}
    $prefs+=@{path=$path;exists=($null -ne $key);values=$values};if($key){$key.Close()}
}
Write-V3Json (Join-Path $output 'playerprefs-before.json') $prefs
$manifest=Get-V3SaveManifest $build
Write-V3Json (Join-Path $output 'candidate-manifest.json') @{candidate=$CandidateId;exe=$exe;version=$version6;dataVersion='mvp1.18.0';runtimeSha256=$ExpectedRuntimeSha256;createdAt=[DateTimeOffset]::Now.ToString('o');inputs=$inputs6;build=$manifest}
foreach($name in @('freeze','core','integration')){Copy-Item -LiteralPath $inputs6[$name] -Destination (Join-Path $output ($name+[IO.Path]::GetExtension($inputs6[$name])))}
$runs=[Collections.Generic.List[object]]::new();$process=$null;$campaignError=$null;$restored=@()
function Assert-Protected([string]$Label){
    $after=Get-V3SaveManifest $protected;$matches=(ConvertTo-Json -InputObject $after -Depth 100 -Compress) -eq $beforeJson
    $legacyMatches=(Get-FileHash -LiteralPath $inputs6.legacy -Algorithm SHA256).Hash -eq $inputs6.hashes.legacy
    $priorMatches=$true
    foreach($entry in $priorRaw6){$path=Assert-V3Child (Join-Path $priorRoot6 $entry.path) $priorRoot6;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256){$priorMatches=$false}}
    Write-V3Json (Join-Path $output "protected-saves-$Label.json") @{matches=$matches;priorQaSourceUnchanged=$legacyMatches;priorCandidateRawUnchanged=$priorMatches;manifest=$after;userDefaultProfile='NOT_ACCESSED'}
    if(-not $matches -or -not $legacyMatches -or -not $priorMatches){throw 'Protected QA evidence changed; preserve files and stop'}
}
function Invoke-V6Case([int]$Width,[int]$Height,[bool]$Resume){
    $caseRoot=Join-Path $output "$Width`x$Height";$fresh=Join-Path $caseRoot 'fresh'
    $dest=Join-Path $caseRoot $(if($Resume){'resume'}else{'fresh'});$save=Join-Path $dest 'profiles/boot'
    New-Item -ItemType Directory -Path $save -Force|Out-Null
    Copy-Item -LiteralPath $inputs6.catalog -Destination (Join-Path $dest 'expected-catalog.json')
    if($Resume){
        Copy-Item -LiteralPath (Join-Path $fresh 'v6-resume-expected.json') -Destination (Join-Path $dest 'v6-resume-expected.json')
        Copy-Item -LiteralPath (Join-Path $fresh 'profiles/boot/last-bell-save.json') -Destination (Join-Path $save 'last-bell-save.json')
        $mode='v6-resume'
    }else{
        $source=Join-Path $dest 'legacy-source';New-Item -ItemType Directory -Path $source|Out-Null
        Copy-Item -LiteralPath $inputs6.legacy -Destination (Join-Path $source 'last-bell-save.json')
        if($HealthBarOnly){$mode='v6-hp-only'}
        elseif($Width -eq 1280 -and ($ContinueAfterCandidate01 -or $ContinueAfterCandidate02 -or $ContinueAfterCandidate03)){Write-V3Json (Join-Path $dest 'continuation-source.json') $continuation6;$mode=if($ContinueAfterCandidate03){'v6-route-tail'}elseif($ContinueAfterCandidate02){'v6-tail'}else{'v6-continue'}}
        elseif($Width -eq 1280){Copy-Item -LiteralPath $inputs6.legacy -Destination (Join-Path $save 'last-bell-save.json');$mode='v6-legacy'}else{$mode='v6'}
    }
    $label="$mode-$Width`x$Height";$argsList=@('-dragon-theme-smoke',('"'+$dest+'"'),'-dragon-theme-case',$mode,'-lastbell-save-dir',('"'+$save+'"'),'-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',('"'+(Join-Path $dest 'player.log')+'"'))
    Assert-Protected "before-$label"
    $started=[DateTimeOffset]::Now;$timedOut=$false
    $script:process=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
    Write-Output "STARTED $label pid=$($script:process.Id)"
    while(-not $script:process.WaitForExit(1000)){
        if(([DateTimeOffset]::Now-$started).TotalSeconds -gt 1830){$timedOut=$true;$script:process.Kill();$script:process.WaitForExit();break}
    }
    $script:process.Refresh();$exitCode=$script:process.ExitCode;$script:process.Dispose();$script:process=$null
    $resultPath=Join-Path $dest 'result.json';$result=if(Test-Path -LiteralPath $resultPath){Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json}else{$null}
    $ok=$null -ne $result -and $result.completed -and $result.passed -and $exitCode -eq 0 -and -not $timedOut
    $runs.Add(@{name=$label;mode=$mode;output=$dest;save=$save;width=$Width;height=$Height;exitCode=$exitCode;timedOut=$timedOut;passed=$ok;arguments=$argsList;startedAt=$started.ToString('o');endedAt=[DateTimeOffset]::Now.ToString('o')})
    Write-V3Json (Join-Path $output 'processes.json') $runs.ToArray();Assert-Protected "after-$label"
    Write-Output "FINISHED $label exit=$exitCode observationPassed=$ok"
    if(-not $ok){throw "V6 observation failed: $label; remaining cases not started"}
}
try{
    Invoke-V6Case 1280 720 $false
    if(-not $HealthBarOnly){Invoke-V6Case 1280 720 $true}
    Invoke-V6Case 640 360 $false
    if(-not $HealthBarOnly){Invoke-V6Case 640 360 $true}
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
    $expectedProcesses=if($HealthBarOnly){2}else{4}
    $passed=-not $campaignError -and $runs.Count -eq $expectedProcesses -and @($runs|Where-Object {-not $_.passed}).Count -eq 0
    $scope=if($HealthBarOnly){'0.9.6 healthbar-only integration: actual player/normal/boss damage, explicit HP fixtures and frame-clock trail; Core held after first contact. Prior0.9.5 campaign reused. Boundary/heal/reset/multi-hit/pause use Root unit evidence.'}else{'Representative v6 changed-scope observations. Seed/high-HP/future-three-slot fixtures and manual clock gaps are labelled. Only1280 includes continuous1-to20; native restore covers two selected checkpoints.'}
    Write-V3Json (Join-Path $output 'campaign.json') @{observationPassed=$passed;error=$campaignError;runs=$runs.ToArray();expectedProcesses=$expectedProcesses;continuation=$continuation6;acceptance='NOT_CERTIFIED';osInput='DEFERRED';visualReview='NOT_RUN';audioListening='NOT_RUN';userDefaultProfile='NOT_ACCESSED';core='Handed-over evidence reused';limits=($scope+' No full acceptance, normal-speed performance, physical input or listening claim.')}
}
if(-not $passed){throw "V6 observation failed: $campaignError"}
