#define PublishDir AddBackslash(SourcePath) + "..\artifacts\installer\publish\win-x64"
#define AppExe PublishDir + "\SMSR.App.exe"
#define AppVersion GetVersionNumbersString(AppExe)

[Setup]
AppId={{4E998662-81B7-49DA-B0C8-C598F7A9F716}
AppName=SMSR
AppVersion={#AppVersion}
AppPublisher=SMSR
AppPublisherURL=https://github.com/arscabar/SMSR
AppSupportURL=https://github.com/arscabar/SMSR/issues
AppUpdatesURL=https://github.com/arscabar/SMSR
DefaultDirName={localappdata}\Programs\SMSR
DefaultGroupName=SMSR
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#SourcePath}..\artifacts\installer
OutputBaseFilename=SMSR-Setup-{#AppVersion}-win-x64
SetupIconFile={#SourcePath}SMSR-Setup.ico
UninstallDisplayIcon={app}\SMSR.App.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern dynamic polar includetitlebar
WizardSizePercent=120
DisableWelcomePage=no
WizardSmallImageFile={#SourcePath}..\src\SMSR.App\Assets\SMSR.png
WizardSmallImageFileDynamicDark={#SourcePath}..\src\SMSR.App\Assets\SMSR.png
WizardImageFile={#SourcePath}SMSR-Wizard.png
WizardImageFileDynamicDark={#SourcePath}SMSR-Wizard.png
CloseApplications=force
CloseApplicationsFilter=SMSR.App.exe
RestartApplications=no
VersionInfoVersion={#AppVersion}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\SMSR"; Filename: "{app}\SMSR.App.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\SMSR"; Filename: "{app}\SMSR.App.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SMSR"; ValueData: """{app}\SMSR.App.exe"" --background"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\SMSR.App.exe"; Description: "{cm:LaunchProgram,SMSR}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\SMSR.App.exe"; Parameters: "--uninstall-cleanup"; WorkingDir: "{app}"; RunOnceId: "SMSRCodexCleanup"; Flags: runhidden waituntilterminated skipifdoesntexist

#include "SMSR.UI.iss"
