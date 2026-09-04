param(
    [Parameter(Mandatory = $true)]
    [string]$ApplicationPath,
    [int]$ExpectedToolCount = 12
)

$ErrorActionPreference = "Stop"
$resolvedApplication = (Resolve-Path -LiteralPath $ApplicationPath).Path
function Read-ResponseLine([IO.StreamReader]$Reader, [Diagnostics.Process]$Process) {
    $read = $Reader.ReadLineAsync()
    if (-not $read.Wait(30000)) {
        try { $Process.Kill() } catch { }
        throw "Timed out waiting for the stdio bridge response."
    }
    $read.Result
}
$requests = @(
    '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"release-check","version":"1.0"}}}',
    '{"jsonrpc":"2.0","method":"notifications/initialized"}',
    '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
)
$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $resolvedApplication
$startInfo.Arguments = "--mcp-stdio"
$startInfo.WorkingDirectory = [IO.Path]::GetDirectoryName($resolvedApplication)
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$process = [Diagnostics.Process]::Start($startInfo)
$process.StandardInput.WriteLine($requests[0])
$process.StandardInput.Flush()
$initializeLine = Read-ResponseLine $process.StandardOutput $process
$process.StandardInput.WriteLine($requests[1])
$process.StandardInput.WriteLine($requests[2])
$process.StandardInput.Flush()
$toolListLine = Read-ResponseLine $process.StandardOutput $process
$process.StandardInput.Close()
$remainingOutput = $process.StandardOutput.ReadToEnd()
$rawOutput = @($initializeLine, $toolListLine, $remainingOutput) -join "`n"
$standardError = $process.StandardError.ReadToEnd()
$process.WaitForExit()
if ($process.ExitCode -ne 0) {
    throw "stdio bridge exited with $($process.ExitCode): $standardError"
}

    $responses = @($rawOutput -split "`r?`n" | ForEach-Object {
        try { $_ | ConvertFrom-Json } catch { $null }
    })
    $initialize = $responses | Where-Object { $_.id -eq 1 } | Select-Object -First 1
    $toolList = $responses | Where-Object { $_.id -eq 2 } | Select-Object -First 1
    $tools = if ($null -eq $toolList) { @() } else { @($toolList.result.tools) }
    if ($initialize.result.protocolVersion -ne "2025-11-25" -or $tools.Count -ne $ExpectedToolCount) {
        throw "Unexpected stdio response. protocol=$($initialize.result.protocolVersion), tools=$($tools.Count), stdout=$rawOutput, stderr=$standardError"
    }
    [pscustomobject]@{
        Protocol = $initialize.result.protocolVersion
        ToolCount = $tools.Count
        Tools = ($tools.name | Sort-Object) -join ", "
    }
