param([switch]$PlayerDefines)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$unity='C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data'
$dest=Join-Path $PSScriptRoot 'Validation'
New-Item -ItemType Directory -Path $dest -Force|Out-Null
$defines=if($PlayerDefines){'DEVELOPMENT_BUILD,UNITY_STANDALONE_WIN,LASTBELL_PLAYTEST_092,UNITY_6000_0_OR_NEWER,ENABLE_LEGACY_INPUT_MANAGER'}else{'UNITY_EDITOR,UNITY_STANDALONE_WIN,UNITY_6000_0_OR_NEWER,ENABLE_LEGACY_INPUT_MANAGER'}
$label=if($PlayerDefines){'player'}else{'editor'}
$refs=@(Get-ChildItem -LiteralPath "$unity/NetStandard/ref/2.1.0" -Filter '*.dll')+@(Get-ChildItem -LiteralPath "$unity/Managed/UnityEngine" -Filter '*.dll')
if(-not $PlayerDefines){$refs+=@(Get-ChildItem -LiteralPath "$unity/Managed" -Filter 'UnityEditor*.dll'|Where-Object Name -ne 'UnityEditor.dll')}
$refs+=Get-Item -LiteralPath "$root/Client/Library/ScriptAssemblies/UnityEngine.UI.dll"
$argsList=@('-nostdlib+','-target:library','-langversion:9',('-define:'+$defines),('-out:"'+$dest+'/LastBell.Runtime.'+$label+'.dll"'))
$argsList+=@($refs|ForEach-Object {'-r:"'+$_.FullName+'"'})
$argsList+=@(Get-ChildItem -LiteralPath "$root/Client/Assets/Game/Runtime" -Filter '*.cs' -Recurse|ForEach-Object {'"'+$_.FullName+'"'})
$response=Join-Path $dest "$label.rsp"
Set-Content -LiteralPath $response -Value $argsList -Encoding UTF8
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" "@$response" 2>&1|Tee-Object -FilePath (Join-Path $dest "$label.log")
if($LASTEXITCODE -ne 0){throw "Runtime source compile failed: $label"}
Write-Output "Source compiled ($label defines). Unity/player/tests were not launched; output is QA-local only."
