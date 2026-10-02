param([string]$Output=(Join-Path $PSScriptRoot '../artifacts/media-acceptance'))
$ErrorActionPreference='Stop'
$taskOutput=[IO.Path]::GetFullPath($Output)
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))+[IO.Path]::DirectorySeparatorChar
if(-not $taskOutput.StartsWith($taskRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture output outside workspace'}
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
Add-Type -AssemblyName System.Speech
foreach($taskCase in @(@{Name='english';Voice='Microsoft Zira Desktop';Text='The graph stores evidence. Validate the input, save the result, and create a report.'},
 @{Name='korean';Voice='Microsoft Heami Desktop';Text='안녕하세요. 이것은 음성 인식 테스트입니다. 입력을 검증하고 결과를 저장합니다.'})) {
 $taskSpeech=New-Object System.Speech.Synthesis.SpeechSynthesizer
 try{$taskSpeech.SelectVoice($taskCase.Voice);$taskSpeech.SetOutputToWaveFile((Join-Path $taskOutput ($taskCase.Name+'.wav')));$taskSpeech.Speak($taskCase.Text)}finally{$taskSpeech.Dispose()}
}
Write-Output $taskOutput
