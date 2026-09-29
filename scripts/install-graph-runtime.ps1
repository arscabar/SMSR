param([string]$Python = "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe")
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Python)) { $Python = 'python' }
$runtime = Join-Path $env:LOCALAPPDATA 'SMSR/graph-runtime'
$source = Join-Path $PSScriptRoot '../src/SMSR.App/GraphRuntime'
& $Python -m venv $runtime
if ($LASTEXITCODE -ne 0) { throw 'Python 3.12 runtime creation failed' }
$runner = Join-Path $runtime 'Scripts/python.exe'
& $runner -m pip install -r (Join-Path $source 'requirements.txt')
if ($LASTEXITCODE -ne 0) { throw 'Graph dependencies installation failed' }
& $runner (Join-Path $source 'embedding.py')
if ($LASTEXITCODE -ne 0) { throw 'Local multilingual model setup failed' }
Write-Output 'Local graph runtime ready. Git global hooks were not changed.'
