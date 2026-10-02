$ErrorActionPreference='Stop'
$taskRoot=Join-Path ([IO.Path]::GetTempPath()) ('smsr-skills-check-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskRoot | Out-Null
$taskInstaller=Join-Path $PSScriptRoot 'install-smsr-graph-skills.ps1'
try {
 $taskPreview=@(& $taskInstaller -Destination $taskRoot)
 if($taskPreview.Count -ne 5 -or (Get-ChildItem -LiteralPath $taskRoot).Count){throw 'Preview wrote files'}
 $taskBackup=& $taskInstaller -Mode Install -Destination $taskRoot
 if(-not (Test-Path -LiteralPath (Join-Path $taskRoot 'smsr-explain/SKILL.md'))){throw 'Skill install missing'}
 $taskRejected=$false
 try{& $taskInstaller -Mode Install -Destination $taskRoot}catch{$taskRejected=$true}
 if(-not $taskRejected){throw 'Implicit overwrite allowed'}
 & $taskInstaller -Mode Rollback -Destination $taskRoot -Backup $taskBackup
 if(Test-Path -LiteralPath (Join-Path $taskRoot 'smsr-explain')){throw 'Rollback kept installed target'}
 $taskBackup=& $taskInstaller -Mode Install -Destination $taskRoot
 $taskHash=(Get-FileHash -LiteralPath (Join-Path $taskRoot 'smsr-query/SKILL.md')).Hash
 $taskReplacement=& $taskInstaller -Mode Install -Destination $taskRoot -Replace
 & $taskInstaller -Mode Rollback -Destination $taskRoot -Backup $taskReplacement
 if((Get-FileHash -LiteralPath (Join-Path $taskRoot 'smsr-query/SKILL.md')).Hash -ne $taskHash){throw 'Original skill not restored'}
 Write-Output 'Skill preview/install/overwrite guard/backup/rollback OK'
} finally {
 $taskResolved=[IO.Path]::GetFullPath($taskRoot)
 if(-not $taskResolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($taskResolved) -notlike 'smsr-skills-check-*'){throw 'Unsafe fixture cleanup'}
 Remove-Item -LiteralPath $taskResolved -Recurse -Force
}
