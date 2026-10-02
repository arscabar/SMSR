param(
    [Parameter(Mandatory=$true)][string]$ApplicationPath,
    [Parameter(Mandatory=$true)][string]$ToolName,
    [Parameter(Mandatory=$true)][string]$ArgumentsJson
)
# A fresh native MCP connection when the desktop's old stdio transport was closed.
$ErrorActionPreference='Stop'
$taskArguments=ConvertFrom-Json -InputObject $ArgumentsJson
$taskStart=[Diagnostics.ProcessStartInfo]::new()
$taskStart.FileName=(Resolve-Path -LiteralPath $ApplicationPath).Path
$taskStart.Arguments='--mcp-stdio --isolated-test'
$taskStart.UseShellExecute=$false
$taskStart.CreateNoWindow=$true
$taskStart.RedirectStandardInput=$true
$taskStart.RedirectStandardOutput=$true
$taskStart.RedirectStandardError=$true
$taskStart.StandardOutputEncoding=[Text.Encoding]::UTF8
$taskStart.StandardInputEncoding=[Text.UTF8Encoding]::new($false)
$taskProcess=[Diagnostics.Process]::Start($taskStart)
function Read-McpReply([int]$Id) {
    while($true) {
        $taskRead=$taskProcess.StandardOutput.ReadLineAsync()
        if(-not $taskRead.Wait(190000)){throw 'MCP reply timeout'}
        if($null -eq $taskRead.Result){throw 'MCP transport closed'}
        $taskReply=$taskRead.Result | ConvertFrom-Json
        if($taskReply.id -eq $Id){return $taskReply}
    }
}
try {
    $taskProcess.StandardInput.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"smsr-validation","version":"1.0"}}}')
    $taskProcess.StandardInput.Flush()
    $null=Read-McpReply 1
    $taskProcess.StandardInput.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
    $taskRequest=@{jsonrpc='2.0';id=2;method='tools/call';params=@{name=$ToolName;arguments=$taskArguments}} | ConvertTo-Json -Depth 50 -Compress
    $taskProcess.StandardInput.WriteLine($taskRequest)
    $taskProcess.StandardInput.Flush()
    $taskReply=Read-McpReply 2
    if($taskReply.error -or $taskReply.result.isError){throw 'MCP tool returned an error'}
    $taskReply.result.content | Where-Object type -eq 'text' | ForEach-Object {Write-Output $_.text}
} finally {
    $taskProcess.StandardInput.Close()
    if(-not $taskProcess.WaitForExit(5000)){$taskProcess.Kill()}
    $taskProcess.Dispose()
}
