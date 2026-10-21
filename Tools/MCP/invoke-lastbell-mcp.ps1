#requires -Version 7.0
param(
    [ValidateSet('tools/list', 'resources/list', 'resources/read')][string]$Method = 'tools/list',
    [string]$Tool,
    [string]$ArgumentsJson = '{}',
    [string]$ResourceUri
)
. "$PSScriptRoot/common.ps1"
$null = Open-McpSession
if ($Tool) {
    if ($Tool -eq 'set_active_instance') { throw 'Instance switching is managed by this Last Bell wrapper.' }
    $arguments = $ArgumentsJson | ConvertFrom-Json -AsHashtable -Depth 60
    if ($arguments.ContainsKey('unity_instance') -and $arguments.unity_instance -ne $script:Instance) {
        throw 'A different Unity instance is not permitted.'
    }
    $null = Select-LastBellInstance
    $arguments.unity_instance = $script:Instance
    $result = Invoke-McpRequest 'tools/call' @{ name = $Tool; arguments = $arguments }
} elseif ($Method -eq 'resources/read') {
    if (!$ResourceUri) { throw '-ResourceUri is required.' }
    if ($ResourceUri -ne 'mcpforunity://instances') { $null = Select-LastBellInstance }
    $result = Read-McpResource $ResourceUri
} else { $result = Invoke-McpRequest $Method }
$result | ConvertTo-Json -Depth 60
