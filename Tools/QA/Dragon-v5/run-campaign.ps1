param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][ValidatePattern('^dragon-v5-[A-Za-z0-9_.-]+$')][string]$CandidateId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-fA-F0-9]{64}$')][string]$ExpectedRuntimeSha256,
    [Parameter(Mandatory=$true)][string]$FreezeManifest,
    [Parameter(Mandatory=$true)][string]$CoreEvidence,
    [Parameter(Mandatory=$true)][string]$IntegrationEvidence,
    [Parameter(Mandatory=$true)][string]$LegacySave,
    [switch]$Run,
    [switch]$Story12Only
)
$ErrorActionPreference='Stop'
if(-not $Run){throw 'Prepared only. Source freeze, final observed build and execution ownership must be handed over before -Run.'}
. (Join-Path $PSScriptRoot 'common.ps1')
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$build=Join-Path $root 'Builds/Dragon-v5-QA'
$exe=Assert-V3Child $Executable $build
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw 'V5 observed executable missing'}
$runtime=Assert-V3Child (Join-Path $build (([IO.Path]::GetFileNameWithoutExtension($exe))+'_Data/Managed/LastBell.Runtime.dll')) $build
if(-not (Test-Path -LiteralPath $runtime -PathType Leaf) -or (Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash -ne $ExpectedRuntimeSha256){throw 'V5 Runtime hash differs from handoff'}
$inputs5=Assert-V5Inputs $root $FreezeManifest $CoreEvidence $IntegrationEvidence $LegacySave $ExpectedRuntimeSha256
$output=Assert-V3Child (Join-Path $root "Planning/Evidence/Dragon-v5/$CandidateId") (Join-Path $root 'Planning/Evidence/Dragon-v5')
if(Test-Path -LiteralPath $output){throw 'Refusing to overwrite existing candidate evidence'}
New-Item -ItemType Directory -Path $output|Out-Null
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
Write-V3Json (Join-Path $output 'candidate-manifest.json') @{candidate=$CandidateId;exe=$exe;version='0.9.4';dataVersion='mvp1.17.0';runtimeSha256=$ExpectedRuntimeSha256;createdAt=[DateTimeOffset]::Now.ToString('o');inputs=$inputs5;build=$manifest}
foreach($name in @('freeze','core','integration')){Copy-Item -LiteralPath $inputs5[$name] -Destination (Join-Path $output ($name+[IO.Path]::GetExtension($inputs5[$name])))}
$runs=[Collections.Generic.List[object]]::new();$process=$null;$campaignError=$null;$restored=@()
function Assert-Protected([string]$Label){
    $after=Get-V3SaveManifest $protected;$matches=(ConvertTo-Json -InputObject $after -Depth 100 -Compress) -eq $beforeJson
    $legacyMatches=(Get-FileHash -LiteralPath $inputs5.legacy -Algorithm SHA256).Hash -eq $inputs5.hashes.legacy
    Write-V3Json (Join-Path $output "protected-saves-$Label.json") @{matches=$matches;priorQaSourceUnchanged=$legacyMatches;manifest=$after;userDefaultProfile='NOT_ACCESSED'}
    if(-not $matches -or -not $legacyMatches){throw 'Protected QA evidence changed; preserve files and stop'}
}
function Invoke-V5Case([int]$Width,[int]$Height,[bool]$Resume){
    $caseRoot=Join-Path $output "$Width`x$Height";$fresh=Join-Path $caseRoot 'fresh'
    $dest=Join-Path $caseRoot $(if($Resume){'resume'}else{'fresh'});$save=Join-Path $dest 'profiles/boot'
    New-Item -ItemType Directory -Path $save -Force|Out-Null
    Copy-Item -LiteralPath $inputs5.catalog -Destination (Join-Path $dest 'expected-catalog.json')
    if($Resume){
        Copy-Item -LiteralPath (Join-Path $fresh 'v5-resume-expected.json') -Destination (Join-Path $dest 'v5-resume-expected.json')
        Copy-Item -LiteralPath (Join-Path $fresh 'profiles/boot/last-bell-save.json') -Destination (Join-Path $save 'last-bell-save.json')
        $mode='v5-resume'
    }elseif($Story12Only){$mode='v5-story12'}else{
        $source=Join-Path $dest 'legacy-source';New-Item -ItemType Directory -Path $source|Out-Null
        Copy-Item -LiteralPath $inputs5.legacy -Destination (Join-Path $source 'last-bell-save.json')
        if($Width -eq 1280){Copy-Item -LiteralPath $inputs5.legacy -Destination (Join-Path $save 'last-bell-save.json');$mode='v5-legacy'}else{$mode='v5'}
    }
    $label="$mode-$Width`x$Height";$argsList=@('-dragon-theme-smoke',('"'+$dest+'"'),'-dragon-theme-case',$mode,'-lastbell-save-dir',('"'+$save+'"'),'-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',('"'+(Join-Path $dest 'player.log')+'"'))
    Assert-Protected "before-$label"
    $started=[DateTimeOffset]::Now;$timedOut=$false
    $script:process=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
    Write-Output "STARTED $label pid=$($script:process.Id)"
    while(-not $script:process.WaitForExit(1000)){
        if(([DateTimeOffset]::Now-$started).TotalSeconds -gt 930){$timedOut=$true;$script:process.Kill();$script:process.WaitForExit();break}
    }
    $script:process.Refresh();$exitCode=$script:process.ExitCode;$script:process.Dispose();$script:process=$null
    $resultPath=Join-Path $dest 'result.json';$result=if(Test-Path -LiteralPath $resultPath){Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json}else{$null}
    $ok=$null -ne $result -and $result.completed -and $result.passed -and $exitCode -eq 0 -and -not $timedOut
    $runs.Add(@{name=$label;mode=$mode;output=$dest;save=$save;width=$Width;height=$Height;exitCode=$exitCode;timedOut=$timedOut;passed=$ok;arguments=$argsList;startedAt=$started.ToString('o');endedAt=[DateTimeOffset]::Now.ToString('o')})
    Write-V3Json (Join-Path $output 'processes.json') $runs.ToArray();Assert-Protected "after-$label"
    Write-Output "FINISHED $label exit=$exitCode observationPassed=$ok"
    if(-not $ok){throw "V5 observation failed: $label; remaining cases not started"}
}
try{
    Invoke-V5Case 1280 720 $false
    if(-not $Story12Only){Invoke-V5Case 1280 720 $true}
    Invoke-V5Case 640 360 $false
    if(-not $Story12Only){Invoke-V5Case 640 360 $true}
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
    $expectedProcesses=if($Story12Only){2}else{4}
    $passed=-not $campaignError -and $runs.Count -eq $expectedProcesses -and @($runs|Where-Object {-not $_.passed}).Count -eq 0
    $scope=if($Story12Only){'DQA5-002 Stage12 dialogue only: three player IDs at each resolution; candidate02 evidence remains separate.'}else{'Representative changed-scope probe only. Manual clock gaps and explicit high-HP/future-rule fixtures are recorded.'}
    Write-V3Json (Join-Path $output 'campaign.json') @{observationPassed=$passed;error=$campaignError;runs=$runs.ToArray();expectedProcesses=$expectedProcesses;acceptance='NOT_CERTIFIED';osInput='DEFERRED';visualReview='NOT_RUN';audioListening='NOT_RUN';userDefaultProfile='NOT_ACCESSED';core='Handed-over evidence reused';limits=($scope+' No full acceptance, normal-speed performance, physical input or listening claim.')}
}
if(-not $passed){throw "V5 observation failed: $campaignError"}
