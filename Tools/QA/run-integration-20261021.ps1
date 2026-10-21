param([ValidateSet('Prepare','EditMode','PlayMode','Build','Smoke','Resume','PrefsBackup','PrefsRestore')][string]$Phase)
$ErrorActionPreference='Stop'
$root='C:\Github\Test2'
$evidence=Join-Path $root 'Planning\Evidence\UnityPresentation-20261021'
$copy=Join-Path $root 'Temp\CombatFeedbackValidation\Client'
$unity='C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$utf8=[System.Text.UTF8Encoding]::new($false)
function JsonFile($path,$data) { [IO.File]::WriteAllText($path,($data|ConvertTo-Json -Depth 15),$utf8) }
if($Phase -eq 'Prepare') {
    $entries=@()
    foreach($part in @('Assets','Packages','ProjectSettings')) {
        $source=Join-Path $root "Client\$part"
        $target=[IO.Path]::GetFullPath((Join-Path $copy $part))
        if($target -ne (Join-Path 'C:\Github\Test2\Temp\CombatFeedbackValidation\Client' $part)) { throw 'Invalid copy target' }
        if(Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
        Copy-Item -LiteralPath $source -Destination $target -Recurse
        foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
            $relative=$file.FullName.Substring((Join-Path $root 'Client').Length+1)
            $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            $copied=(Get-FileHash -LiteralPath (Join-Path $copy $relative) -Algorithm SHA256).Hash
            if($hash -ne $copied) { throw "Copy changed: $relative" }
            $entries += [pscustomobject]@{path=$relative;sha256=$hash;bytes=$file.Length}
        }
    }
    JsonFile (Join-Path $evidence 'source-manifest.json') @{createdAt=[DateTimeOffset]::Now.ToString('o');source=(Join-Path $root 'Client');copy=$copy;libraryReused=$true;files=$entries}
    git -C $root status --short | Set-Content -LiteralPath (Join-Path $evidence 'git-status-at-snapshot.txt') -Encoding utf8
    Write-Output "SNAPSHOT_VERIFIED files=$($entries.Count) copy=$copy"
    exit 0
}
$regPaths=@('Software\LastBellPrototype\Last Bell MVP','Software\Unity\UnityEditor\LastBellPrototype\Last Bell MVP')
if($Phase -eq 'PrefsBackup') {
    $items=@()
    foreach($regPath in $regPaths) {
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($regPath)
        $values=@()
        if($key) {
            foreach($name in $key.GetValueNames()) { if($name -like 'LastBell.*' -or $name -like 'Screenmanager*' -or $name -like 'UnitySelectMonitor*') { $values += @{name=$name;kind=$key.GetValueKind($name).ToString();value=$key.GetValue($name)} } }
            $key.Close()
        }
        $items += @{path=$regPath;exists=($null -ne $key);values=$values}
    }
    $backup=Join-Path $evidence 'playerprefs-before.json'
    if(Test-Path -LiteralPath $backup) { throw 'Existing prefs backup must not be overwritten' }
    JsonFile $backup $items
    Write-Output 'PLAYERPREFS_BACKED_UP'
    exit 0
}
if($Phase -eq 'PrefsRestore') {
    $items=Get-Content -Raw -LiteralPath (Join-Path $evidence 'playerprefs-before.json')|ConvertFrom-Json
    foreach($item in $items) {
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($item.path,$true)
        if(-not $key -and $item.exists) { $key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($item.path) }
        if($key) {
            foreach($name in $key.GetValueNames()) { if(($name -like 'LastBell.*' -or $name -like 'Screenmanager*' -or $name -like 'UnitySelectMonitor*') -and $name -notin @($item.values.name)) { $key.DeleteValue($name,$false) } }
            foreach($entry in $item.values) {
                $kind=[Microsoft.Win32.RegistryValueKind]::$($entry.kind)
                $value=$entry.value
                if($entry.kind -eq 'DWord') { $value=[int]$value }
                if($entry.kind -eq 'QWord') { $value=[long]$value }
                if($entry.kind -eq 'Binary') { $value=[byte[]]$value }
                if($entry.kind -eq 'MultiString') { $value=[string[]]$value }
                $key.SetValue($entry.name,$value,$kind)
            }
            $key.Close()
        }
    }
    Write-Output 'PLAYERPREFS_RESTORED'
    exit 0
}
$tag=$Phase.ToLowerInvariant()
$log=Join-Path $evidence "$tag.log"
if(Test-Path -LiteralPath $log) { throw "Existing run log: $log" }
if($Phase -in @('Smoke','Resume')) {
    $exe=Join-Path $root 'Temp\CombatFeedbackValidation\Builds\Windows\LastBellMvp.exe'
    $output=Join-Path $evidence $tag
    $profile=Join-Path $evidence 'profiles\smoke'
    New-Item -ItemType Directory -Path $output,$profile -Force | Out-Null
    $flag=if($Phase -eq 'Smoke'){'-lastbell-smoke'}else{'-lastbell-resume-smoke'}
    $argsList=@($flag,"`"$output`"",'-lastbell-save-dir',"`"$profile`"",'-screen-width','960','-screen-height','540','-screen-fullscreen','0','-logFile',"`"$log`"")
} else {
    $exe=$unity
    $argsList=@('-batchmode','-nographics','-projectPath',"`"$copy`"",'-buildTarget','Win64','-logFile',"`"$log`"",'-lastbell-save-dir',"`"$(Join-Path $evidence 'profiles\tests')`"")
    if($Phase -eq 'Build') { $argsList+=@('-quit','-executeMethod','LastBell.Editor.MvpBuild.Windows') }
    else {
        $argsList+=@('-runTests','-testPlatform',$Phase,'-testResults',"`"$(Join-Path $evidence "$tag.xml")`"")
        if($Phase -eq 'PlayMode') { $argsList+=@('-testFilter','LastBell.Tests.CombatPresentationTests') }
    }
}
$start=[DateTimeOffset]::Now
$process=Start-Process -FilePath $exe -ArgumentList $argsList -WindowStyle Hidden -PassThru
Write-Output "STARTED phase=$Phase pid=$($process.Id)"
$process.WaitForExit()
$process.Refresh()
JsonFile (Join-Path $evidence "$tag-process.json") @{phase=$Phase;exe=$exe;arguments=$argsList;pid=$process.Id;startedAt=$start.ToString('o');endedAt=[DateTimeOffset]::Now.ToString('o');exitCode=$process.ExitCode}
Write-Output "FINISHED phase=$Phase exit=$($process.ExitCode)"
exit $process.ExitCode
