using System.Collections.Concurrent;

namespace SMSR.App.Mvp;

public sealed class WorkflowEventNotifier
{
    // ponytail: one signal per observed workflow; add expiry only if observed workflow count becomes large.
    private readonly ConcurrentDictionary<(string ProjectId, string WorkflowId), Signal> _signals = [];

    public long Version(string projectId, string workflowId) => Get(projectId, workflowId).Version;

    public void Publish(string projectId, string workflowId)
    {
        if (_signals.TryGetValue((projectId, workflowId), out var signal)) signal.Publish();
    }

    public Task WaitForChangeAsync(string projectId, string workflowId, long version, CancellationToken cancellationToken)
        => Get(projectId, workflowId).WaitForChangeAsync(version, cancellationToken);

    private Signal Get(string projectId, string workflowId) => _signals.GetOrAdd((projectId, workflowId), _ => new());

    private sealed class Signal
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
}
