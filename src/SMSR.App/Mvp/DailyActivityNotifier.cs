namespace SMSR.App.Mvp;

public sealed class DailyActivityNotifier
{
    public event EventHandler<DailyActivityChangedEventArgs>? Changed;
    public void Publish(string projectId) => Changed?.Invoke(this, new(projectId));
}

public sealed class DailyActivityChangedEventArgs(string projectId) : EventArgs
{
    public string ProjectId { get; } = projectId;
}
