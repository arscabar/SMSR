using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private bool _isCheckingForUpdates;

    public string CurrentVersionLabel => $"현재 버전 {AppUpdateService.CurrentVersion.ToString(3)}";

    public Task CheckForUpdatesOnStartupAsync()
        => AutoUpdateEnabled ? CheckForUpdatesAsync(true) : Task.CompletedTask;

    private async Task CheckForUpdatesAsync(bool automatic)
    {
        if (_isCheckingForUpdates) return;
        _isCheckingForUpdates = true;
        ((RelayCommand)CheckForUpdatesCommand).NotifyCanExecuteChanged();
        StatusMessage = "새 버전을 확인하고 있습니다.";
        try
        {
            var result = await _updates.CheckAndDownloadAsync(AppUpdateService.CurrentVersion);
            StatusMessage = result.Message;
            if (!result.Available || result.InstallerPath is null) return;
            if (!automatic && !_platform.Confirm("SMSR 업데이트",
                    $"SMSR {result.Version?.ToString(3)}을 설치합니다. 앱이 잠시 종료됩니다."))
            {
                StatusMessage = "업데이트 설치를 취소했습니다.";
                return;
            }
            if (!_updates.StartInstaller(result.InstallerPath))
            {
                StatusMessage = "업데이트 설치 프로그램을 시작하지 못했습니다.";
                return;
            }
            StatusMessage = "업데이트 설치를 시작했습니다.";
            _exitApplication();
        }
        catch (Exception exception) { StatusMessage = $"업데이트 확인 실패: {exception.Message}"; }
        finally
        {
            _isCheckingForUpdates = false;
            ((RelayCommand)CheckForUpdatesCommand).NotifyCanExecuteChanged();
        }
    }
}
