[CustomMessages]
korean.BrandFooter=SMSR · 로컬 우선 에이전트 워크플로우 대시보드
english.BrandFooter=SMSR · Local-first agent workflow dashboard
korean.WelcomeTitle=SMSR 설치를 시작합니다
english.WelcomeTitle=Welcome to SMSR
korean.WelcomeBody=에이전트 계획과 실행 상태를 로컬에서 안전하게 추적합니다.%n%n계속하려면 다음을 선택하세요.
english.WelcomeBody=Track agent plans and execution locally and securely.%n%nChoose Next to continue.
korean.FinishedTitle=SMSR 설치가 완료되었습니다
english.FinishedTitle=SMSR is ready
korean.FinishedBody=SMSR을 실행한 뒤 Codex를 한 번 다시 열면 인증창 없이 로컬 MCP 연결이 시작됩니다.
english.FinishedBody=Launch SMSR, then reopen Codex once to start the local MCP connection without browser authentication.
korean.UninstallTitle=SMSR 제거
english.UninstallTitle=Uninstall SMSR

[Code]
function IsAutomaticUpdate: Boolean;
begin
  Result := ExpandConstant('{param:SMSRAUTORESTART|0}') = '1';
end;

procedure InitializeWizard;
begin
  WizardForm.Caption := 'SMSR';
  WizardForm.BeveledLabel.Caption := CustomMessage('BrandFooter');
  WizardForm.WelcomeLabel1.Caption := CustomMessage('WelcomeTitle');
  WizardForm.WelcomeLabel2.Caption := CustomMessage('WelcomeBody');
  WizardForm.FinishedHeadingLabel.Caption := CustomMessage('FinishedTitle');
  WizardForm.FinishedLabel.Caption := CustomMessage('FinishedBody');
  WizardForm.PageNameLabel.Font.Style := [fsBold];
  WizardForm.PageNameLabel.Font.Size := 12;
  WizardForm.PageDescriptionLabel.Font.Size := 9;
  WizardForm.StatusLabel.Font.Style := [fsBold];
  WizardForm.NextButton.Font.Style := [fsBold];
  WizardForm.ProgressGauge.Height := ScaleY(12);
end;

procedure InitializeUninstallProgressForm;
begin
  UninstallProgressForm.Caption := CustomMessage('UninstallTitle');
  UninstallProgressForm.StatusLabel.Font.Style := [fsBold];
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM SMSR.App.exe', '',
    SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
