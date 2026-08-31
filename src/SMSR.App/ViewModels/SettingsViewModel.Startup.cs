using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private readonly WindowsStartupRegistration _startup = new();
    private bool _startWithWindows;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            try
            {
                if (value) _startup.Enable(); else _startup.Disable();
                SetField(ref _startWithWindows, value);
                StatusMessage = value ? "Windows 로그인 자동 시작을 켰습니다." : "Windows 로그인 자동 시작을 껐습니다.";
            }
            catch (Exception exception)
            {
                StatusMessage = $"Windows 자동 시작을 변경하지 못했습니다: {exception.Message}";
                OnPropertyChanged(nameof(StartWithWindows));
            }
        }
    }

    private bool ReadStartupState()
    {
        try { return _startup.IsEnabled(); }
        catch { return false; }
    }

    private void OnSettingsChanged(object? sender, EventArgs eventArgs)
    {
        _startWithWindows = ReadStartupState();
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(StartServerAutomatically));
        OnPropertyChanged(nameof(AutomateCodexIntegration));
        OnPropertyChanged(nameof(MinimizeToTray));
        OnPropertyChanged(nameof(DashboardTheme));
        OnPropertyChanged(nameof(RequirePlanReview));
        OnPropertyChanged(nameof(PlanningPrompt));
    }
}
