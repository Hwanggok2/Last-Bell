$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'common.ps1')
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$dest=Join-Path $PSScriptRoot ('Validation/self-test-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $dest|Out-Null
Add-Type -TypeDefinition (Get-Content -LiteralPath (Join-Path $root 'Client/Assets/Game/Runtime/Bootstrap/SaveDirectorySelector.cs') -Raw)
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$Ok,[string]$Label){if(-not $Ok){throw $Label};$checks.Add($Label)}
$persistent=Join-Path $dest 'persistent'
New-Item -ItemType Directory -Path $persistent|Out-Null
$legacy=Join-Path $persistent 'last-bell-save.json';$backup=$legacy+'.bak'
[IO.File]::WriteAllText($legacy,'legacy progress marker');[IO.File]::WriteAllText($backup,'legacy backup marker')
$hashes=@((Get-FileHash -LiteralPath $legacy).Hash,(Get-FileHash -LiteralPath $backup).Hash)
$selected=[LastBell.SaveDirectorySelector]::Select($persistent,[string[]]@(),$true)
Check ($selected -eq (Join-Path $persistent 'Playtests/dragon-0.9.2')) 'playtest selects separate namespace'
Check (-not (Test-Path -LiteralPath $selected)) 'selector creates no directory or save'
Check ([LastBell.SaveDirectorySelector]::Select($persistent,[string[]]@(),$false) -eq $persistent) 'legacy/Editor path remains unchanged'
$override=Join-Path $dest 'override'
Check ([LastBell.SaveDirectorySelector]::Select($persistent,[string[]]@('-lastbell-save-dir',$override),$true) -eq $override) 'explicit override wins for playtest'
Check ([LastBell.SaveDirectorySelector]::Select($persistent,[string[]]@('-lastbell-save-dir',$override),$false) -eq $override) 'explicit override wins for legacy'
New-Item -ItemType Directory -Path $selected|Out-Null
$newSave=Join-Path $selected 'last-bell-save.json';[IO.File]::WriteAllText($newSave,'new progress marker')
$selectedAgain=[LastBell.SaveDirectorySelector]::Select($persistent,[string[]]@(),$true)
Check ($selectedAgain -eq $selected -and (Get-Content -LiteralPath (Join-Path $selectedAgain 'last-bell-save.json') -Raw) -eq 'new progress marker') 'reselecting namespace neither resets nor changes new file'
Check ((Get-FileHash -LiteralPath $legacy).Hash -eq $hashes[0] -and (Get-FileHash -LiteralPath $backup).Hash -eq $hashes[1]) 'synthetic legacy primary and backup preserved'
$child=Assert-V3Child (Join-Path $dest 'safe/inside') (Join-Path $dest 'safe')
Check ($child -eq (Join-Path $dest 'safe/inside')) 'normal descendant accepted'
foreach($bad in @((Join-Path $dest 'safe/../escape'),(Join-Path $dest 'safe-sibling/file'),(Join-Path $dest 'safe'))){
    $rejected=$false;try{$null=Assert-V3Child $bad (Join-Path $dest 'safe')}catch{$rejected=$true}
    Check $rejected "non-child path rejected: $bad"
}
foreach($file in (Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1')){
    $tokens=$null;$errors=$null;$null=[Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
    Check ($errors.Count -eq 0) "PowerShell parse: $($file.Name)"
}
Write-V3Json (Join-Path $dest 'result.json') @{passed=$true;checks=$checks.ToArray();unityRun=$false;playerRun=$false;userDefaultProfile='NOT_ACCESSED';limits='Source-level selector and path guards; native progress restoration is NOT_RUN'}
Write-Output "PASS $($checks.Count) source-only checks. Result: $dest/result.json"
