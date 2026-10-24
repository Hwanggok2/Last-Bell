param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_.-]*$')][string]$CandidateId,
    [switch]$Run,
    [switch]$ReviewOnly,
    [switch]$CardsOnly,
    [switch]$V2Only,
    [switch]$V2HpOnly
)
$ErrorActionPreference='Stop'
if(-not $Run){throw 'Prepared tool only. Use -Run after the coordinator hands over the completed QA candidate.'}
if(([int][bool]$ReviewOnly+[int][bool]$CardsOnly+[int][bool]$V2Only+[int][bool]$V2HpOnly) -gt 1){throw 'Choose one targeted mode'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$exe=[IO.Path]::GetFullPath($Executable)
$theme=if($V2Only -or $V2HpOnly){'Dragon-v2'}else{'Dragon-v1'}
$allowedBuild=Join-Path $root "Builds\$theme-QA"
if(-not $exe.StartsWith($allowedBuild+'\',[StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $exe -PathType Leaf)){
    throw "Executable must be the handed-over observed build under Builds/$theme-QA."
}
$output=[IO.Path]::GetFullPath((Join-Path $root "Planning\Evidence\$theme\$CandidateId"))
$allowedOutput=Join-Path $root "Planning\Evidence\$theme"
if(-not $output.StartsWith($allowedOutput+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid evidence path'}
if(Test-Path -LiteralPath $output){throw 'Candidate output already exists; no automatic overwrites or reruns.'}
foreach($target in @($exe,$output)){
    $directory=[IO.DirectoryInfo]::new([IO.Path]::GetDirectoryName($target))
    while($directory){
        if($directory.Exists -and ($directory.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'QA paths must not traverse junctions or symbolic links.'}
        $directory=$directory.Parent
    }
}
New-Item -ItemType Directory -Path $output|Out-Null
$utf8=[Text.UTF8Encoding]::new($false)
function Write-Json($path,$value){[IO.File]::WriteAllText($path,(ConvertTo-Json -InputObject $value -Depth 15),$utf8)}
function Scoped-Names($key){@($key.GetValueNames()|Where-Object {$_ -like 'LastBell.*' -or $_ -like 'Screenmanager*' -or $_ -like 'UnitySelectMonitor*'})}
$registryPaths=@('Software\LastBellPrototype\Last Bell MVP','Software\Unity\UnityEditor\LastBellPrototype\Last Bell MVP')
$prefs=@()
foreach($path in $registryPaths){
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path)
    $values=@()
    if($key){foreach($name in (Scoped-Names $key)){$values+=@{name=$name;kind=$key.GetValueKind($name).ToString();value=$key.GetValue($name)}}}
    $prefs+=@{path=$path;exists=($null -ne $key);values=$values}
    if($key){$key.Close()}
}
Write-Json (Join-Path $output 'playerprefs-before.json') $prefs
$files=@(Get-ChildItem -LiteralPath $allowedBuild -Recurse -File|ForEach-Object {
    @{path=$_.FullName.Substring($allowedBuild.Length+1);sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length}
})
Write-Json (Join-Path $output 'candidate-manifest.json') @{exe=$exe;candidate=$CandidateId;files=$files;createdAt=[DateTimeOffset]::Now.ToString('o')}
$runs=[Collections.Generic.List[object]]::new()
$process=$null
function Invoke-Case([string]$name,[string]$mode,[int]$width,[int]$height,[string]$resumeSource=''){
    $caseOut=Join-Path $output $name
    $save=Join-Path $caseOut 'profiles\boot'
    New-Item -ItemType Directory -Path $caseOut,(Split-Path -Parent $save)|Out-Null
    if($resumeSource){
        Copy-Item -LiteralPath (Join-Path $resumeSource 'profiles\boot') -Destination $save -Recurse
        Copy-Item -LiteralPath (Join-Path $resumeSource 'expected-save.json') -Destination (Join-Path $caseOut 'expected-save.json')
    }
    $log=Join-Path $caseOut 'player.log'
    # Windows paths cannot contain quotes; quote only the path-valued arguments.
    $argsList=@('-dragon-theme-smoke',('"'+$caseOut+'"'),'-dragon-theme-case',$mode,
        '-lastbell-save-dir',('"'+$save+'"'),'-screen-width',"$width",'-screen-height',"$height",'-screen-fullscreen','0','-logFile',('"'+$log+'"'))
    $started=[DateTimeOffset]::Now
    $script:process=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
    Write-Output "STARTED $name pid=$($script:process.Id)"
    $timedOut=$false
    while(-not $script:process.WaitForExit(1000)){
        if(([DateTimeOffset]::Now-$started).TotalSeconds -gt 330){
            $timedOut=$true
            # This Process handle belongs only to the player launched immediately above.
            $script:process.Kill();$script:process.WaitForExit();break
        }
    }
    $script:process.Refresh();$exitCode=$script:process.ExitCode;$script:process.Dispose();$script:process=$null
    $resultPath=Join-Path $caseOut 'result.json'
    $result=if(Test-Path -LiteralPath $resultPath){Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json}else{$null}
    $record=@{name=$name;case=$mode;width=$width;height=$height;exe=$exe;arguments=$argsList;exitCode=$exitCode;timedOut=$timedOut;
        resultPresent=($null -ne $result);passed=($null -ne $result -and $result.passed -and $exitCode -eq 0 -and -not $timedOut);
        startedAt=$started.ToString('o');endedAt=[DateTimeOffset]::Now.ToString('o')}
    $runs.Add($record);Write-Json (Join-Path $output 'processes.json') $runs.ToArray()
    Write-Output "FINISHED $name exit=$exitCode passed=$($record.passed)"
}
try{
    if($V2HpOnly){
        Invoke-Case 'v2-hp-1280x720' 'v2-hp' 1280 720
        Invoke-Case 'v2-hp-640x360' 'v2-hp' 640 360
    }elseif($V2Only){
        Invoke-Case 'v2-1280x720' 'v2' 1280 720
        Invoke-Case 'v2-640x360' 'v2' 640 360
    }elseif($ReviewOnly -or $CardsOnly){
        $mode=if($CardsOnly){'cards'}else{'review'}
        Invoke-Case ($mode+'-1280x720') $mode 1280 720
        Invoke-Case ($mode+'-640x360') $mode 640 360
    }else{
        Invoke-Case 'full-1280x720' 'full' 1280 720
        Invoke-Case 'layout-960x540' 'layout' 960 540
        Invoke-Case 'layout-640x360' 'layout' 640 360
        $full=Join-Path $output 'full-1280x720'
        if((Test-Path -LiteralPath (Join-Path $full 'expected-save.json')) -and $runs[0].passed){
            Invoke-Case 'resume-1280x720' 'resume' 1280 720 $full
        }
    }
}
finally{
    if($process){if(-not $process.HasExited){$process.Kill();$process.WaitForExit()};$process.Dispose()}
    $restored=@()
    foreach($item in $prefs){
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($item.path,$true)
        if(-not $key -and $item.exists){$key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($item.path)}
        if($key){
            foreach($name in (Scoped-Names $key)){if($name -notin @($item.values.name)){$key.DeleteValue($name,$false)}}
            foreach($entry in $item.values){
                $value=$entry.value
                if($entry.kind -eq 'DWord'){$value=[int]$value}
                if($entry.kind -eq 'QWord'){$value=[long]$value}
                if($entry.kind -eq 'Binary'){$value=[byte[]]$value}
                $kind=[Enum]::Parse([Microsoft.Win32.RegistryValueKind],$entry.kind)
                $key.SetValue($entry.name,$value,$kind)
            }
            $names=Scoped-Names $key;$ok=$names.Count -eq @($item.values).Count
            foreach($entry in $item.values){
                if($entry.name -notin $names -or $key.GetValueKind($entry.name).ToString() -ne $entry.kind -or
                    (ConvertTo-Json -InputObject $key.GetValue($entry.name) -Compress) -ne (ConvertTo-Json -InputObject $entry.value -Compress)){$ok=$false}
            }
            $key.Close()
        }else{$ok=@($item.values).Count -eq 0}
        $restored+=@{path=$item.path;matches=$ok}
    }
    Write-Json (Join-Path $output 'playerprefs-restored.json') $restored
    if($restored.matches -contains $false){throw 'Scoped PlayerPrefs restoration mismatch; inspect evidence before continuing.'}
}
$expectedRuns=if($ReviewOnly -or $CardsOnly -or $V2Only -or $V2HpOnly){2}else{4}
$passed=$runs.Count -eq $expectedRuns -and @($runs|Where-Object {-not $_.passed}).Count -eq 0
Write-Json (Join-Path $output 'campaign.json') @{passed=$passed;targetedReview=([bool]$ReviewOnly -or [bool]$CardsOnly -or [bool]$V2Only -or [bool]$V2HpOnly);cardsOnly=[bool]$CardsOnly;playerV2=([bool]$V2Only -or [bool]$V2HpOnly);hpOnly=[bool]$V2HpOnly;osInput=$false;runs=$runs.ToArray();limits='Automated representative cases, not complete visual acceptance or OS input.'}
if(-not $passed){exit 1}
