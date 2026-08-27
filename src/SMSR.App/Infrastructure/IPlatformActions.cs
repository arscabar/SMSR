namespace SMSR.App.Infrastructure;

public interface IPlatformActions
{
    bool TryCopyToClipboard(string value);
    bool TryOpenBrowser(string url);
}
