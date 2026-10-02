param(
 [ValidateSet('Preview','Install','Rollback')][string]$Mode='Preview',
 [string]$Destination=(Join-Path $env:USERPROFILE '.codex/skills'),
 [string]$Backup,
 [switch]$Replace
)
$ErrorActionPreference='Stop'
$taskNames=@('smsr-query','smsr-explain','smsr-impact','smsr-maintain','smsr-report')
$taskSource=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.agents/skills'))
$taskDestination=[IO.Path]::GetFullPath($Destination)
if($Mode -eq 'Preview') {Write-Output ($taskNames | ForEach-Object {Join-Path $taskDestination $_}); return}
if(-not (Test-Path -LiteralPath $taskDestination)){New-Item -ItemType Directory -Path $taskDestination | Out-Null}
if((Get-Item -LiteralPath $taskDestination).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked skill destination is not supported'}
if($Mode -eq 'Rollback') {
 $taskBackup=(Resolve-Path -LiteralPath $Backup).Path
 if(-not $taskBackup.StartsWith($taskDestination+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Backup outside destination'}
 $taskManifest=Get-Content -LiteralPath (Join-Path $taskBackup 'manifest.json') -Raw | ConvertFrom-Json
 foreach($taskEntry in $taskManifest){
  if($taskNames -notcontains $taskEntry.Name){throw 'Unexpected skill name'}
  $taskTarget=Join-Path $taskDestination $taskEntry.Name
  if((Get-FileHash -LiteralPath (Join-Path $taskTarget 'SKILL.md')).Hash -ne $taskEntry.Hash){throw 'Installed skill changed; preserve edits before rollback'}
 }
 foreach($taskEntry in $taskManifest){
  $taskTarget=Join-Path $taskDestination $taskEntry.Name
  Move-Item -LiteralPath $taskTarget -Destination (Join-Path $taskBackup ($taskEntry.Name+'.installed'))
  $taskOld=Join-Path $taskBackup $taskEntry.Name
  if(Test-Path -LiteralPath $taskOld){Move-Item -LiteralPath $taskOld -Destination $taskTarget}
 }
 return
}
foreach($taskName in $taskNames){
 if(-not (Test-Path -LiteralPath (Join-Path $taskSource "$taskName/SKILL.md"))){throw 'Source skill missing'}
 if((Test-Path -LiteralPath (Join-Path $taskDestination $taskName)) -and -not $Replace){throw 'Existing skill: use explicit -Replace for backup and replacement'}
 if((Test-Path -LiteralPath (Join-Path $taskDestination $taskName)) -and ((Get-Item -LiteralPath (Join-Path $taskDestination $taskName)).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked skill target is not supported'}
}
$taskBackup=Join-Path $taskDestination ('.smsr-backup-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskBackup | Out-Null
$taskStage=Join-Path $taskBackup 'stage'
New-Item -ItemType Directory -Path $taskStage | Out-Null
foreach($taskName in $taskNames){Copy-Item -LiteralPath (Join-Path $taskSource $taskName) -Destination (Join-Path $taskStage $taskName) -Recurse}
$taskManifest=@($taskNames | ForEach-Object {@{Name=$_;Hash=(Get-FileHash -LiteralPath (Join-Path $taskStage "$_/SKILL.md")).Hash}})
$taskManifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskBackup 'manifest.json') -Encoding utf8
$taskChanged=@()
try {
 foreach($taskName in $taskNames){
  $taskTarget=Join-Path $taskDestination $taskName
  if(Test-Path -LiteralPath $taskTarget){Move-Item -LiteralPath $taskTarget -Destination (Join-Path $taskBackup $taskName)}
  $taskChanged+=$taskName
  Move-Item -LiteralPath (Join-Path $taskStage $taskName) -Destination $taskTarget
 }
} catch {
 foreach($taskName in $taskChanged){
  $taskTarget=Join-Path $taskDestination $taskName
  if(Test-Path -LiteralPath $taskTarget){Move-Item -LiteralPath $taskTarget -Destination (Join-Path $taskBackup ($taskName+'.partial'))}
  $taskOld=Join-Path $taskBackup $taskName
  if(Test-Path -LiteralPath $taskOld){Move-Item -LiteralPath $taskOld -Destination $taskTarget}
 }
 throw
}
Write-Output $taskBackup
