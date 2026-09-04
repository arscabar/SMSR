using System.Collections.Concurrent;

namespace SMSR.App.Mvp;

public sealed record PendingDailySummary(string RequestId, DateTime Date, string Prompt, DateTimeOffset CreatedAtUtc);
public sealed class DailySummaryCompletedEventArgs(string requestId, DateTime date, string content) : EventArgs
{
    public string RequestId { get; } = requestId;
    public DateTime Date { get; } = date;
    public string Content { get; } = content;
}

public sealed class DailySummaryCoordinator
{
    private readonly ConcurrentDictionary<string, PendingDailySummary> _pending = new();
    public event EventHandler<DailySummaryCompletedEventArgs>? Completed;

    public PendingDailySummary Create(DateTime date, string prompt)
    {
        Prune();
        var request = new PendingDailySummary(Guid.NewGuid().ToString("N"), date.Date, prompt,
            DateTimeOffset.UtcNow);
        _pending[request.RequestId] = request;
        return request;
    }

    public PendingDailySummary? Get(string requestId)
        => _pending.TryGetValue(requestId, out var request) ? request : null;

    public bool Complete(string requestId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 20_000) return false;
        if (!_pending.TryRemove(requestId, out var request)) return false;
        Completed?.Invoke(this, new(requestId, request.Date, content.Trim()));
        return true;
    }

    private void Prune()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-1);
        foreach (var item in _pending.Where(item => item.Value.CreatedAtUtc < cutoff))
            _pending.TryRemove(item.Key, out _);
    }
}
