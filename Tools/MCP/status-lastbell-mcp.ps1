#requires -Version 7.0
param([switch]$Smoke, [string]$OutputPath)
. "$PSScriptRoot/common.ps1"
$status = [ordered]@{
    checkedAt = [DateTimeOffset]::UtcNow.ToOffset([TimeSpan]::FromHours(9)).ToString('o')
    endpoint = $script:Endpoint; expectedProject = $script:ProjectRoot; expectedInstance = $script:Instance
    serverInitialized = $false; toolCount = 0; unityConnected = $false; projectVerified = $false; readSmokePassed = $false
}
$failed = $false
try {
    $init = Open-McpSession
    $status.serverInitialized = $true
    $status.serverInfo = $init.serverInfo
    $status.protocolVersion = $init.protocolVersion
    $listed = Invoke-McpRequest 'tools/list'
    $status.toolCount = @($listed.tools).Count
    $status.instances = (Read-McpResource 'mcpforunity://instances').instances
    $status.unityConnected = @($status.instances | Where-Object id -eq $script:Instance).Count -eq 1
    if ($status.unityConnected) {
        $status.project = Select-LastBellInstance
        $status.projectVerified = $true
        if ($Smoke) {
            $status.readSmoke = Invoke-McpRequest 'tools/call' @{ name = 'manage_scene'; arguments = @{ action = 'get_active'; unity_instance = $script:Instance } }
            $scene = $status.readSmoke.content[0].text | ConvertFrom-Json -Depth 60
            if (!$scene.success) { throw 'Scene read returned success=false.' }
            $status.readSmokePassed = $true
        }
    } elseif ($Smoke) { throw 'Server is available, but the Last Bell Unity Editor is not connected.' }
} catch { $status.error = $_.Exception.Message; $failed = $true }
$json = $status | ConvertTo-Json -Depth 60
if ($OutputPath) { Set-Content -LiteralPath $OutputPath -Value $json -Encoding utf8 }
$json
if ($failed) { exit 1 }
