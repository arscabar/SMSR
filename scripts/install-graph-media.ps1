param([switch]$DownloadModels,[switch]$InstallOcr)
$ErrorActionPreference='Stop'
$taskPython=Join-Path $env:LOCALAPPDATA 'SMSR/graph-runtime/Scripts/python.exe'
if(-not (Test-Path -LiteralPath $taskPython)){throw 'Run install-graph-runtime.ps1 first'}
& $taskPython -m pip install -r (Join-Path $PSScriptRoot '../src/SMSR.App/GraphRuntime/requirements-media.txt')
if($LASTEXITCODE -ne 0){throw 'Media dependencies installation failed'}
if($InstallOcr){
 & winget install --id UB-Mannheim.TesseractOCR --exact --silent --accept-package-agreements --accept-source-agreements
 if($LASTEXITCODE -ne 0){throw 'OCR installation failed; existing installation was not removed'}
}
if($DownloadModels){
 foreach($taskScript in @('install-media-models.py','install-ocr-models.py')){
  & $taskPython (Join-Path $PSScriptRoot $taskScript)
  if($LASTEXITCODE -ne 0){throw 'Explicit local model setup failed'}
 }
}
Write-Output 'Media dependencies ready. Models/OCR are downloaded only with explicit switches. Files were not uploaded; hooks were not changed.'
