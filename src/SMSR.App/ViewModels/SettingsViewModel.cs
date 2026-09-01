using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettingsService _settings;
    private readonly IPlatformActions _platform;
    private string _statusMessage = "변경 사항은 현재 Windows 사용자에게 자동 저장됩니다.";

    public SettingsViewModel(AppSettingsService settings, LocalServerHost host, IPlatformActions platform)
    {
        _settings = settings;
        _platform = platform;
        DataPath = host.DataPath;
        LogPath = System.IO.Path.GetDirectoryName(host.LogPath) ?? host.DataPath;
        OpenDataFolderCommand = new RelayCommand(() => Open(DataPath, "데이터"));
        OpenLogFolderCommand = new RelayCommand(() => Open(LogPath, "로그"));
        _startWithWindows = ReadStartupState();
        _settings.Changed += OnSettingsChanged;
    }

    public bool StartServerAutomatically
    {
        get => _settings.Current.StartServerAutomatically;
        set => Update(_settings.Current with { StartServerAutomatically = value }, nameof(StartServerAutomatically));
    }

    public bool AutomateCodexIntegration
    {
        get => _settings.Current.AutomateCodexIntegration;
        set => Update(_settings.Current with { AutomateCodexIntegration = value }, nameof(AutomateCodexIntegration));
    }

    public bool MinimizeToTray
    {
        get => _settings.Current.MinimizeToTray;
        set => Update(_settings.Current with { MinimizeToTray = value }, nameof(MinimizeToTray));
    }

    public string DashboardTheme
    {
        get => _settings.Current.DashboardTheme;
        set => Update(_settings.Current with { DashboardTheme = DashboardThemes.Normalize(value) }, nameof(DashboardTheme));
    }

    public string DataPath { get; }
    public string LogPath { get; }
    public string McpEndpoint => CodexMcpConfig.Endpoint;
    public string McpConnectionMode => CodexMcpConfig.ConnectionMode;
    public IReadOnlyList<string> DashboardThemeOptions { get; } = [DashboardThemes.Dark, DashboardThemes.Light];
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public ICommand OpenDataFolderCommand { get; }
    public ICommand OpenLogFolderCommand { get; }

    private void Update(AppSettings value, string propertyName)
    {
        try { _settings.Save(value); StatusMessage = "설정을 저장했습니다."; OnPropertyChanged(propertyName); }
        catch (Exception exception) { StatusMessage = $"설정을 저장하지 못했습니다: {exception.Message}"; }
    }

    private void Open(string path, string label)
        => StatusMessage = _platform.TryOpenPath(path) ? $"{label} 폴더를 열었습니다." : $"{label} 폴더를 열지 못했습니다.";

}
