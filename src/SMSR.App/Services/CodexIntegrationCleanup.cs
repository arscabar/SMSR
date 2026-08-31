namespace SMSR.App.Services;

internal static class CodexIntegrationCleanup
{
    public static void Run()
    {
        new WindowsStartupRegistration().Disable();
        var configPath = CodexDesktopLocator.GetConfigPath();
        CodexAutoTrackingHook.Unregister(configPath);
        CodexMcpConfig.Unregister(configPath);
    }
}
