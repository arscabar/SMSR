param([string]$Javac = 'javac')
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '../src/SMSR.JavaAnalysis'
$output = Join-Path $PSScriptRoot '../artifacts/java-analysis'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$files = @(Get-ChildItem -LiteralPath $source -Filter '*.java' | ForEach-Object FullName)
& $Javac --release 17 -encoding UTF-8 -proc:none -d $output @files
if ($LASTEXITCODE -ne 0) { throw 'Java analysis helper compilation failed' }
Write-Output 'Java 17 analysis helper compiled. No SDK installed.'
