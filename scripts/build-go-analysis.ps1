param([Parameter(Mandatory=$true)][string]$Go)
$ErrorActionPreference='Stop'
$goPath=(Resolve-Path -LiteralPath $Go).Path
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source=Join-Path $root 'src/SMSR.GoAnalysis'
$output=Join-Path $root 'artifacts/go-analysis'
$cache=Join-Path $root 'artifacts/go-build-cache'
$saved=@{}
$settings=@{GOENV='off';GO111MODULE='off';GOWORK='off';GOTOOLCHAIN='local';GOPROXY='off';GOSUMDB='off';GOTELEMETRY='off';CGO_ENABLED='0';GOFLAGS='';GOEXPERIMENT='';GODEBUG='';GOCACHEPROG='';GOOS='windows';GOARCH='amd64';GOCACHE=$cache;GOROOT=(Split-Path (Split-Path $goPath))}
try {
 foreach($key in $settings.Keys){$saved[$key]=[Environment]::GetEnvironmentVariable($key);[Environment]::SetEnvironmentVariable($key,$settings[$key])}
 New-Item -ItemType Directory -Path $output,$cache -Force | Out-Null
 Push-Location (Join-Path $settings.GOROOT 'src')
 try { $exports=& $goPath list -export -deps -f '{{.ImportPath}}|{{.Export}}' std
  if($LASTEXITCODE -ne 0){throw 'Go standard-library export preparation failed'}
 } finally {Pop-Location}
 foreach($line in $exports){
  $parts=$line.Split('|');if($parts.Length -ne 2){throw 'Unexpected Go export row'}
  if(!$parts[1]){continue}
  if($parts[0] -notmatch '^[A-Za-z0-9_./-]+$' -or $parts[0].Split('/') -contains '..'){throw 'Invalid standard package path'}
  $target=Join-Path $output ('exports/'+$parts[0]+'.a')
  New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
  Copy-Item -LiteralPath $parts[1] -Destination $target
 }
 Push-Location $source
 try {
  & $goPath test -count=1 .
  if($LASTEXITCODE -ne 0){throw 'Go analyzer self-check failed'}
  & $goPath build -trimpath -o (Join-Path $output 'SMSR.GoAnalysis.exe') .
  if($LASTEXITCODE -ne 0){throw 'Go analyzer build failed'}
 } finally {Pop-Location}
 Copy-Item -LiteralPath (Join-Path $settings.GOROOT 'LICENSE') -Destination (Join-Path $output 'GO-LICENSE.txt')
} finally {
 foreach($key in $saved.Keys){[Environment]::SetEnvironmentVariable($key,$saved[$key])}
}
Write-Output 'Go helper and standard-library type data prepared. Target repository code was not built.'
