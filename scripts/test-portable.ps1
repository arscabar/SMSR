[CmdletBinding()]
param([string] $ArchivePath)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($ArchivePath)) {
    $archive = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot "artifacts") -Filter "SMSR-win-*.zip" |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
} else {
    $archive = Get-Item -LiteralPath $ArchivePath
}
if ($null -eq $archive) { throw "Portable archive not found." }

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("smsr-portable-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $testRoot | Out-Null
Expand-Archive -LiteralPath $archive.FullName -DestinationPath $testRoot
$executable = Join-Path $testRoot "SMSR.App.exe"
if (-not (Test-Path -LiteralPath $executable)) { throw "Published executable not found." }
$bridge = Join-Path $testRoot "SMSR.Bridge.exe"
if (-not (Test-Path -LiteralPath $bridge)) { throw "Published console bridge not found." }

$results = foreach ($argument in @("--codex-config-self-test", "--tracking-self-test", "--oauth-self-test", "--self-test")) {
    $process = Start-Process -FilePath $executable -ArgumentList $argument -WorkingDirectory $testRoot -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "$argument failed with exit code $($process.ExitCode)." }
    "$argument : OK"
}

$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $bridge
$startInfo.Arguments = "--smsr-auto-track-hook"
$startInfo.WorkingDirectory = $testRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$hookProcess = [Diagnostics.Process]::Start($startInfo)
$hookProcess.StandardInput.WriteLine('{"session_id":"portable-session","cwd":"C:\\portable-project","prompt":"DO_NOT_COPY"}')
$hookProcess.StandardInput.Close()
$hookOutput = $hookProcess.StandardOutput.ReadToEnd()
$hookProcess.WaitForExit()
if ($hookProcess.ExitCode -ne 0 -or -not $hookOutput.Contains("portable-session") -or $hookOutput.Contains("DO_NOT_COPY")) {
    throw "Portable hook mode failed: exit=$($hookProcess.ExitCode), session=$($hookOutput.Contains('portable-session')), promptOmitted=$(-not $hookOutput.Contains('DO_NOT_COPY'))."
}

$results
"hook mode : OK"
& (Join-Path $PSScriptRoot "test-mcp-stdio.ps1") -ApplicationPath $bridge -ExpectedToolCount 10
"archive : $($archive.FullName) ($($archive.Length) bytes)"
"extracted executable : $executable"
Get-ChildItem -LiteralPath $testRoot -File -Recurse | ForEach-Object { "file : $($_.Name) ($($_.Length) bytes)" }
