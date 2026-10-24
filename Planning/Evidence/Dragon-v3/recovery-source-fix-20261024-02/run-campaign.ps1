param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_.-]*$')][string]$CandidateId,
    [switch]$StoryOnly1280,
    [switch]$RecoveryOnly,
    [switch]$Run
)
$ErrorActionPreference='Stop'
if(-not $Run){throw 'Prepared only. Do not execute before the coordinator hands over the completed V3 candidate and execution ownership.'}
if($StoryOnly1280 -and $RecoveryOnly){throw 'Choose one targeted campaign scope'}
. (Join-Path $PSScriptRoot 'common.ps1')
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$allowedBuild=Join-Path $root 'Builds/Dragon-v3-QA'
$exe=Assert-V3Child $Executable $allowedBuild
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw 'Observed candidate executable is missing'}
$output=Assert-V3Child (Join-Path $root "Planning/Evidence/Dragon-v3/$CandidateId") (Join-Path $root 'Planning/Evidence/Dragon-v3')
if(Test-Path -LiteralPath $output){throw 'Existing candidate output must not be overwritten'}
$inputs=Assert-V3Inputs $root
New-Item -ItemType Directory -Path $output|Out-Null
# The coordinator forbids accessing the user's actual default profile. Only a
# synthetic legacy fixture is hashed. Do not report this as an actual-profile audit.
$protected=Join-Path $output 'protected-legacy-fixture'
New-Item -ItemType Directory -Path $protected|Out-Null
[IO.File]::WriteAllText((Join-Path $protected 'last-bell-save.json'),'QA synthetic legacy sentinel; not a user save')
[IO.File]::WriteAllText((Join-Path $protected 'last-bell-save.json.bak'),'QA synthetic legacy backup sentinel')
$saveBefore=Get-V3SaveManifest $protected
$beforeJson=ConvertTo-Json -InputObject $saveBefore -Depth 100 -Compress
Write-V3Json (Join-Path $output 'protected-saves-before.json') $saveBefore
function Scoped-Names($Key){@($Key.GetValueNames()|Where-Object {$_ -like 'LastBell.*' -or $_ -like 'Screenmanager*' -or $_ -like 'UnitySelectMonitor*'})}
$prefs=@()
foreach($path in @('Software\LastBellPrototype\Last Bell MVP','Software\Unity\UnityEditor\LastBellPrototype\Last Bell MVP')){
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path);$values=@()
    if($key){foreach($name in (Scoped-Names $key)){$values+=@{name=$name;kind=$key.GetValueKind($name).ToString();value=$key.GetValue($name)}}}
    $prefs+=@{path=$path;exists=($null -ne $key);values=$values};if($key){$key.Close()}
}
Write-V3Json (Join-Path $output 'playerprefs-before.json') $prefs
$files=@(Get-ChildItem -LiteralPath $allowedBuild -Recurse -File|ForEach-Object {
    Assert-V3NoLinks $_.FullName
    @{path=$_.FullName.Substring($allowedBuild.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length}
})
Write-V3Json (Join-Path $output 'candidate-manifest.json') @{exe=$exe;candidate=$CandidateId;version='0.9.2';dataVersion='mvp1.15.0';files=$files;createdAt=[DateTimeOffset]::Now.ToString('o');inputs=$inputs}
$runs=[Collections.Generic.List[object]]::new();$process=$null
function Assert-ProtectedSaves([string]$Label){
    $after=Get-V3SaveManifest $protected
    $matches=(ConvertTo-Json -InputObject $after -Depth 100 -Compress) -eq $beforeJson
    Write-V3Json (Join-Path $output "protected-saves-$Label.json") @{matches=$matches;manifest=$after}
    if(-not $matches){throw 'Protected synthetic legacy fixture changed. Stop without restoring over the evidence.'}
}
function Invoke-V3Case([string]$Label,[int]$Width,[int]$Height,[bool]$Resume){
    $caseRoot=Join-Path $output "$Width`x$Height"
    $fresh=Join-Path $caseRoot 'fresh';$dest=if($Resume){$caseRoot}else{$fresh}
    # Parent output for restart keeps the same save path inside the output boundary.
    $save=Join-Path $fresh 'profiles/boot'
    if(-not $Resume){
        New-Item -ItemType Directory -Path $fresh|Out-Null
        Copy-Item -LiteralPath $inputs.catalog -Destination (Join-Path $fresh 'expected-catalog.json')
        Copy-Item -LiteralPath $inputs.bindings -Destination (Join-Path $fresh 'data-bindings.json')
        Copy-Item -LiteralPath $inputs.art -Destination (Join-Path $fresh 'art-manifest.json')
    }else{
        Copy-Item -LiteralPath (Join-Path $fresh 'v3-resume-expected.json') -Destination (Join-Path $dest 'v3-resume-expected.json')
    }
    $mode=if($Resume){'v3-resume'}elseif($RecoveryOnly){'v3-recovery'}elseif($StoryOnly1280 -and $Width -eq 1280){'v3-story'}else{'v3'}
    $log=Join-Path $dest 'player.log'
    $argsList=@('-dragon-theme-smoke',('"'+$dest+'"'),'-dragon-theme-case',$mode,'-lastbell-save-dir',('"'+$save+'"'),
        '-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',('"'+$log+'"'))
    Assert-ProtectedSaves "before-$Label"
    $started=[DateTimeOffset]::Now;$timedOut=$false
    $script:process=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
    Write-Output "STARTED $Label pid=$($script:process.Id)"
    while(-not $script:process.WaitForExit(1000)){
        if(([DateTimeOffset]::Now-$started).TotalSeconds -gt 630){$timedOut=$true;$script:process.Kill();$script:process.WaitForExit();break}
    }
    $script:process.Refresh();$exitCode=$script:process.ExitCode;$script:process.Dispose();$script:process=$null
    $resultPath=Join-Path $dest 'result.json';$result=if(Test-Path -LiteralPath $resultPath){Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json}else{$null}
    $ok=$null -ne $result -and $result.completed -and $result.passed -and $exitCode -eq 0 -and -not $timedOut
    $runs.Add(@{name=$Label;mode=$mode;output=$dest;save=$save;width=$Width;height=$Height;exitCode=$exitCode;timedOut=$timedOut;passed=$ok;arguments=$argsList;startedAt=$started.ToString('o');endedAt=[DateTimeOffset]::Now.ToString('o')})
    Write-V3Json (Join-Path $output 'processes.json') $runs.ToArray()
    Assert-ProtectedSaves "after-$Label"
    Write-Output "FINISHED $Label exit=$exitCode passed=$ok"
    if(-not $ok){throw "V3 case failed: $Label. Remaining cases were not started."}
}
$campaignError=$null;$restored=@()
try{
    foreach($size in @(@(1280,720),@(640,360))){
        $label=if($RecoveryOnly){"v3-recovery-$($size[0])x$($size[1])"}elseif($StoryOnly1280 -and $size[0] -eq 1280){'v3-story-1280x720'}else{"v3-$($size[0])x$($size[1])"}
        Invoke-V3Case $label $size[0] $size[1] $false
        if(-not $RecoveryOnly){Invoke-V3Case "v3-resume-$($size[0])x$($size[1])" $size[0] $size[1] $true}
    }
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
            foreach($entry in $item.values){
                if($entry.name -notin $names -or $key.GetValueKind($entry.name).ToString() -ne $entry.kind -or (ConvertTo-Json -InputObject $key.GetValue($entry.name) -Compress) -ne (ConvertTo-Json -InputObject $entry.value -Compress)){$ok=$false}
            }
            $key.Close()
        }else{$ok=@($item.values).Count -eq 0}
        $restored+=@{path=$item.path;matches=$ok}
    }
    Write-V3Json (Join-Path $output 'playerprefs-restored.json') $restored
    try{Assert-ProtectedSaves 'final'}catch{$campaignError=$_.Exception.Message}
    if($restored.matches -contains $false){$campaignError='Scoped PlayerPrefs restore mismatch'}
    $expectedRuns=if($RecoveryOnly){2}else{4}
    $passed=-not $campaignError -and $runs.Count -eq $expectedRuns -and @($runs|Where-Object {-not $_.passed}).Count -eq 0
    Write-V3Json (Join-Path $output 'campaign.json') @{passed=$passed;error=$campaignError;runs=$runs.ToArray();storyOnly1280=[bool]$StoryOnly1280;recoveryOnly=[bool]$RecoveryOnly;expectedProcesses=$expectedRuns;full1280MotionRun=(-not $StoryOnly1280 -and -not $RecoveryOnly);osInput=$false;visualReview='NOT_RUN';defaultSaveRuntime='NOT_RUN';userDefaultProfile='NOT_ACCESSED';limits='Synthetic isolated profile campaign and legacy sentinel. RecoveryOnly runs two targeted processes without restart, separating automatic Arrival Idle from actual combo recovery. Attack asset inventory uses prior source/manifest evidence; actual combat frame coverage is recorded without forced sampling. With StoryOnly1280, prior motion evidence must be adopted separately after product identity review. Not a user-profile hash audit or complete visual acceptance.'}
}
if(-not $passed){throw "V3 campaign failed: $campaignError"}
