param([Parameter(Mandatory=$true)][ValidateSet('Backup','Restore')][string]$Phase,
    [Parameter(Mandatory=$true)][string]$EvidenceDirectory,
    [ValidateSet('Dragon-v1','Dragon-v2')][string]$Theme='Dragon-v1')
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($EvidenceDirectory)
$allowed=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\Planning\Evidence\$Theme"))
if(-not $output.StartsWith($allowed+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Evidence must stay under $Theme"}
function Names($key){@($key.GetValueNames()|Where-Object {$_ -like 'LastBell.*' -or $_ -like 'Screenmanager*' -or $_ -like 'UnitySelectMonitor*'})}
$file=Join-Path $output 'editor-playerprefs-before.json'
if($Phase -eq 'Backup'){
    if(Test-Path -LiteralPath $file){throw 'Do not overwrite the settings backup'}
    $items=@()
    foreach($path in @('Software\LastBellPrototype\Last Bell MVP','Software\Unity\UnityEditor\LastBellPrototype\Last Bell MVP')){
        $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path);$values=@()
        if($key){foreach($name in (Names $key)){$values+=@{name=$name;kind=$key.GetValueKind($name).ToString();value=$key.GetValue($name)}}}
        $items+=@{path=$path;exists=($null -ne $key);values=$values};if($key){$key.Close()}
    }
    ConvertTo-Json -InputObject $items -Depth 10|Set-Content -LiteralPath $file -Encoding utf8
    'EDITOR_PLAYERPREFS_BACKED_UP';exit
}
$items=Get-Content -LiteralPath $file -Raw|ConvertFrom-Json;$results=@()
foreach($item in $items){
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($item.path,$true)
    if(-not $key -and $item.exists){$key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($item.path)}
    $ok=$true
    if($key){
        foreach($name in (Names $key)){if($name -notin @($item.values.name)){$key.DeleteValue($name,$false)}}
        foreach($entry in $item.values){
            $value=$entry.value
            if($entry.kind -eq 'DWord'){$value=[int]$value}
            if($entry.kind -eq 'QWord'){$value=[long]$value}
            if($entry.kind -eq 'Binary'){$value=[byte[]]$value}
            if($entry.kind -eq 'MultiString'){$value=[string[]]$value}
            $key.SetValue($entry.name,$value,[Enum]::Parse([Microsoft.Win32.RegistryValueKind],$entry.kind))
        }
        $names=@(Names $key);$ok=$names.Count -eq @($item.values).Count
        foreach($entry in $item.values){
            if($entry.name -notin $names -or $key.GetValueKind($entry.name).ToString() -ne $entry.kind -or
                (ConvertTo-Json -InputObject $key.GetValue($entry.name) -Compress) -ne (ConvertTo-Json -InputObject $entry.value -Compress)){$ok=$false}
        }
        $key.Close()
    }else{$ok=@($item.values).Count -eq 0}
    $results+=@{path=$item.path;matches=$ok}
}
ConvertTo-Json -InputObject $results -Depth 5|Set-Content -LiteralPath (Join-Path $output 'editor-playerprefs-restored.json') -Encoding utf8
if($results.matches -contains $false){throw 'Settings restoration mismatch'}
'EDITOR_PLAYERPREFS_RESTORED'
