namespace SMSR.App.Mvp;

public sealed class WorkflowEventNotifier
{
    private readonly object _gate = new();
    private TaskCompletionSource _signal = NewSignal();
    private long _version;

    public long Version => Interlocked.Read(ref _version);

    public void Publish()
    {
        TaskCompletionSource completed;
        lock (_gate)
        {
            Interlocked.Increment(ref _version);
            completed = _signal;
            _signal = NewSignal();
        }
        completed.TrySetResult();
    }

    public async Task WaitForChangeAsync(long version, CancellationToken cancellationToken)
    {
        Task signal;
        lock (_gate) signal = _version == version ? _signal.Task : Task.CompletedTask;
        await signal.WaitAsync(cancellationToken);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
