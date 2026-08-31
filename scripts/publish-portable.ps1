[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string] $Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "src\SMSR.App\SMSR.App.csproj"
$quickStartPath = Join-Path $repositoryRoot "docs\portable-quickstart.md"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$artifactRoot = Join-Path $repositoryRoot "artifacts"
$publishPath = Join-Path $artifactRoot "portable\$Runtime\$stamp\SMSR"
$archivePath = Join-Path $artifactRoot "SMSR-$Runtime-$stamp.zip"

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
$publishArguments = @(
    "publish", $projectPath,
    "--configuration", "Release",
    "--runtime", $Runtime,
    "--self-contained", "true",
    "--nologo",
    "-p:PublishProfile=Portable",
    "-p:PublishDir=$publishPath"
)

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath $quickStartPath -Destination (Join-Path $publishPath "README.md")
Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $archivePath

Get-Item -LiteralPath $archivePath | Select-Object FullName, Length, LastWriteTime
