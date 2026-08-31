[CustomMessages]
korean.BrandFooter=SMSR · 로컬 우선 에이전트 워크플로우 대시보드
english.BrandFooter=SMSR · Local-first agent workflow dashboard
korean.WelcomeTitle=SMSR 설치를 시작합니다
english.WelcomeTitle=Welcome to SMSR
korean.WelcomeBody=에이전트 계획과 실행 상태를 로컬에서 안전하게 추적합니다.%n%n계속하려면 다음을 선택하세요.
english.WelcomeBody=Track agent plans and execution locally and securely.%n%nChoose Next to continue.
korean.FinishedTitle=SMSR 설치가 완료되었습니다
english.FinishedTitle=SMSR is ready
korean.FinishedBody=SMSR을 실행한 뒤 Codex를 다시 열면 로컬 MCP 연결을 시작할 수 있습니다.
english.FinishedBody=Launch SMSR, then reopen Codex to start the local MCP connection.
korean.UninstallTitle=SMSR 제거
english.UninstallTitle=Uninstall SMSR

[Code]
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
