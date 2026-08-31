namespace SMSR.App.Mvp;

public sealed class McpConnectionTracker
{
    private long _lastActivityUtcTicks;

    public event EventHandler? Changed;
    public bool IsConnected => Interlocked.Read(ref _lastActivityUtcTicks) != 0;
    public DateTimeOffset? LastActivityAt => Interlocked.Read(ref _lastActivityUtcTicks) is var ticks && ticks != 0
        ? new DateTimeOffset(ticks, TimeSpan.Zero) : null;

    public void MarkActivity()
    {
        var previous = Interlocked.Exchange(ref _lastActivityUtcTicks, DateTimeOffset.UtcNow.UtcTicks);
        if (previous == 0) Changed?.Invoke(this, EventArgs.Empty);
    }
}
