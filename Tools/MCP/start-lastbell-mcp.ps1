#requires -Version 7.0
. "$PSScriptRoot/common.ps1"
$runtime = Join-Path $script:RepoRoot 'Temp/MCP'
$null = New-Item -ItemType Directory -Force -Path $runtime
$listeners = @([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() | Where-Object Port -eq 8093)
if ($listeners.Count) {
    throw 'Port 8093 is already occupied. Nothing was stopped. Use status-lastbell-mcp.ps1 to inspect the existing server.'
}
$uv = (Get-Command uv -ErrorAction Stop).Source
$arguments = @('run', '--no-project', '--cache-dir', "`"$runtime/uv-cache`"", '--with', "mcpforunityserver==$script:ServerVersion",
    'python', "`"$PSScriptRoot/run_lastbell_server.py`"")
$env:UNITY_MCP_TELEMETRY_ENABLED = 'false'
$env:UNITY_MCP_SKIP_STARTUP_CONNECT = 'true'
$process = Start-Process -FilePath $uv -ArgumentList $arguments -WorkingDirectory $script:ProjectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput "$runtime/server.stdout.log" -RedirectStandardError "$runtime/server.stderr.log"
[ordered]@{ endpoint = $script:Endpoint; launcherPid = $process.Id; defaultInstance = $script:Instance; serverVersion = $script:ServerVersion; logs = $runtime } | ConvertTo-Json
