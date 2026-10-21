#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$script:ProjectRoot = 'C:/Github/Test2/Client'
$script:RepoRoot = Split-Path $script:ProjectRoot -Parent
$script:Endpoint = 'http://127.0.0.1:8093/mcp'
$script:ServerVersion = '10.1.0'
$sha = [System.Security.Cryptography.SHA1]::Create()
try {
    $hash = [Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes("$script:ProjectRoot/Assets"))).ToLowerInvariant().Substring(0, 16)
} finally { $sha.Dispose() }
$script:Instance = "Client@$hash"

function Invoke-McpRequest {
    param([string]$Method, [hashtable]$Params = @{}, [switch]$Notification)
    $body = @{ jsonrpc = '2.0'; method = $Method; params = $Params }
    if (!$Notification) { $script:RequestId++; $body.id = $script:RequestId }
    $response = Invoke-WebRequest -Uri $script:Endpoint -Method Post -Headers $script:Headers -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 60 -Compress) -TimeoutSec 55
    if ($response.Headers['mcp-session-id']) {
        $script:Headers['Mcp-Session-Id'] = [string]@($response.Headers['mcp-session-id'])[0]
    }
    if ($Notification) { return }
    $content = [string]$response.Content
    if ($content -match '(?m)^data:') {
        $messages = @($content -split '\r?\n' | Where-Object { $_.StartsWith('data:') } | ForEach-Object { $_.Substring(5).Trim() | ConvertFrom-Json -Depth 60 })
        $rpc = $messages | Where-Object { $_.id -eq $body.id } | Select-Object -Last 1
    } else { $rpc = $content | ConvertFrom-Json -Depth 60 }
    if (!$rpc) { throw "No JSON-RPC response for $Method." }
    if ($rpc.error) { throw ($rpc.error | ConvertTo-Json -Depth 20 -Compress) }
    if ($rpc.result.isError) { throw ($rpc.result | ConvertTo-Json -Depth 20 -Compress) }
    return $rpc.result
}

function Open-McpSession {
    $script:Headers = @{ Accept = 'application/json, text/event-stream' }
    $script:RequestId = 0
    $result = Invoke-McpRequest 'initialize' @{
        protocolVersion = '2024-11-05'; capabilities = @{}
        clientInfo = @{ name = 'LastBellClient'; version = '1.0' }
    }
    $script:Headers['MCP-Protocol-Version'] = $result.protocolVersion
    Invoke-McpRequest 'notifications/initialized' -Notification
    return $result
}

function Read-McpResource {
    param([string]$Uri)
    $result = Invoke-McpRequest 'resources/read' @{ uri = $Uri }
    $value = $result.contents[0].text | ConvertFrom-Json -Depth 60
    if ($value.success -eq $false) { throw ($value | ConvertTo-Json -Depth 20 -Compress) }
    return $value
}

function Select-LastBellInstance {
    $instances = Read-McpResource 'mcpforunity://instances'
    if (!@($instances.instances | Where-Object id -eq $script:Instance).Count) {
        throw "Last Bell is not connected: expected $script:Instance at $script:ProjectRoot. Found: $($instances.instances.id -join ', ')."
    }
    $selected = Invoke-McpRequest 'tools/call' @{ name = 'set_active_instance'; arguments = @{ instance = $script:Instance } }
    $selection = $selected.content[0].text | ConvertFrom-Json -Depth 60
    if (!$selection.success) { throw "Could not select Last Bell: $($selected.content[0].text)" }
    $project = Read-McpResource 'mcpforunity://project/info'
    $actualRoot = ([string]$project.data.projectRoot).Replace('\', '/').TrimEnd('/')
    if ($actualRoot -ine $script:ProjectRoot) { throw "Project mismatch: expected $script:ProjectRoot; received $actualRoot. No requested tool was called." }
    return $project
}
