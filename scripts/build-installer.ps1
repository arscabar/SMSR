[CmdletBinding()]
param([ValidateSet("win-x64")] [string] $Runtime = "win-x64")

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "src\SMSR.App\SMSR.App.csproj"
$installerRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot "artifacts\installer"))
$publishPath = [IO.Path]::GetFullPath((Join-Path $installerRoot "publish\$Runtime"))
$installerScript = Join-Path $repositoryRoot "installer\SMSR.iss"
$quickStart = Join-Path $repositoryRoot "docs\installer-quickstart.md"

$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
    (Join-Path ([Environment]::GetFolderPath("ProgramFilesX86")) "Inno Setup 6\ISCC.exe")
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw "Inno Setup 6 is required. Install: winget install --id JRSoftware.InnoSetup -e --source winget --scope user"
}

if (-not $publishPath.StartsWith($installerRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) { throw "Publish path escaped the installer artifact directory." }
if (Test-Path -LiteralPath $publishPath) { Remove-Item -LiteralPath $publishPath -Recurse -Force }
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
$publishArguments = @(
    "publish", $projectPath,
    "--configuration", "Release",
    "--runtime", $Runtime,
    "--self-contained", "true",
    "--nologo",
    "-p:PublishProfile=Portable",
    "-p:PublishSingleFile=false",
    "-p:IncludeNativeLibrariesForSelfExtract=false",
    "-p:EnableCompressionInSingleFile=false",
    "-p:PublishDir=$publishPath"
)
& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

$publishedApplication = Join-Path $publishPath "SMSR.App.exe"
$bridgeProcess = Start-Process -FilePath $publishedApplication -ArgumentList "--ensure-bridge" -Wait -PassThru -WindowStyle Hidden
if ($bridgeProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $publishPath "SMSR.Bridge.exe"))) {
    throw "Console MCP bridge generation failed with exit code $($bridgeProcess.ExitCode)."
}

Copy-Item -LiteralPath $quickStart -Destination (Join-Path $publishPath "README.md")
& $compiler $installerScript
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }

$installer = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot "artifacts\installer") -Filter "SMSR-Setup-*-win-x64.exe" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installer) { throw "Installer output was not created." }
$installerHash = Get-FileHash -LiteralPath $installer.FullName -Algorithm SHA256
$checksumPath = "$($installer.FullName).sha256"
Set-Content -LiteralPath $checksumPath -Encoding ascii -NoNewline -Value "$($installerHash.Hash.ToLowerInvariant())  $($installer.Name)"
$installerHash
Get-Item -LiteralPath $checksumPath | Select-Object FullName, Length, LastWriteTime
Get-Item -LiteralPath $installer.FullName | Select-Object FullName, Length, LastWriteTime
