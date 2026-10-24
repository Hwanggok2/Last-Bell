$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityEditorData = 'C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data'
$checkFolder = Join-Path $projectRoot 'Planning/Production/DragonUiValidation'
New-Item -ItemType Directory -Path $checkFolder -Force | Out-Null
$references = @()
$references += Get-ChildItem -LiteralPath "$unityEditorData/NetStandard/ref/2.1.0" -Filter '*.dll'
$references += Get-ChildItem -LiteralPath "$unityEditorData/Managed/UnityEngine" -Filter '*.dll'
$references += Get-ChildItem -LiteralPath "$unityEditorData/Managed" -Filter 'UnityEditor*.dll' | Where-Object Name -ne 'UnityEditor.dll'
$references += Get-Item -LiteralPath "$projectRoot/Client/Library/ScriptAssemblies/UnityEngine.UI.dll"
$referenceArgs = $references | ForEach-Object { '-r:"' + $_.FullName + '"' }
$baseArgs = @('-nostdlib+','-target:library','-langversion:9','-define:UNITY_EDITOR,UNITY_STANDALONE_WIN,UNITY_6000_0_OR_NEWER,ENABLE_LEGACY_INPUT_MANAGER')
foreach ($assembly in @('Runtime','Editor')) {
    $sourceFiles = Get-ChildItem -LiteralPath "$projectRoot/Client/Assets/Game/$assembly" -Filter '*.cs' -Recurse
    $arguments = $baseArgs + $referenceArgs + @('-out:"' + "$checkFolder/LastBell.$assembly.dll" + '"')
    if ($assembly -eq 'Editor') { $arguments += '-r:"' + "$checkFolder/LastBell.Runtime.dll" + '"' }
    $arguments += $sourceFiles | ForEach-Object { '"' + $_.FullName + '"' }
    $responsePath = "$checkFolder/$assembly.rsp"
    Set-Content -LiteralPath $responsePath -Value $arguments -Encoding UTF8
    & "$unityEditorData/NetCoreRuntime/dotnet.exe" "$unityEditorData/DotNetSdkRoslyn/csc.dll" "@$responsePath" 2>&1 | Tee-Object -FilePath "$checkFolder/$assembly.log"
    if ($LASTEXITCODE -ne 0) { throw "$assembly C# compile failed ($LASTEXITCODE)" }
}
Write-Output 'Runtime and Editor C# compiled without launching Unity or persisting scenes/prefabs.'
