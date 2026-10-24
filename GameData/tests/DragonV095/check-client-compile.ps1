$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$unityData = 'C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data'
$evidenceRoot = Join-Path $projectRoot 'Planning/Production/DragonV095ClientValidation'
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$references = @(Get-ChildItem -LiteralPath "$unityData/NetStandard/ref/2.1.0" -Filter '*.dll')
$references += Get-ChildItem -LiteralPath "$unityData/Managed/UnityEngine" -Filter '*.dll'
$arguments = @('-nologo','-nostdlib+','-target:library','-langversion:9','-define:UNITY_STANDALONE_WIN')
$arguments += $references | ForEach-Object { '-r:"' + $_.FullName + '"' }
$arguments += '-out:"' + (Join-Path $evidenceRoot 'LastBell.ClientRules.dll') + '"'
foreach($folder in @('Core','Data')) {
    $arguments += Get-ChildItem -LiteralPath "$projectRoot/Client/Assets/Game/Runtime/$folder" -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
}
$arguments += '"' + "$projectRoot/Client/Assets/Game/Runtime/Bootstrap/JsonSaveRepository.cs" + '"'
$response = Join-Path $evidenceRoot 'client.rsp'
Set-Content -LiteralPath $response -Value $arguments -Encoding utf8
& "$unityData/NetCoreRuntime/dotnet.exe" "$unityData/DotNetSdkRoslyn/csc.dll" "@$response" 2>&1 | Tee-Object -FilePath (Join-Path $evidenceRoot 'client-compile.log')
if($LASTEXITCODE -ne 0) { throw 'Core/Data/Persistence compile failed' }
Write-Output 'Core/Data/JsonSaveRepository compiled; no Unity process, scene or prefab operation.'

