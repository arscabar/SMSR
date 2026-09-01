using System.IO;
using SMSR.App.Services;

namespace SMSR.App.Mvp;

internal static class ActivityStoreSelfCheck
{
    public static async Task RunAsync(string dataPath)
    {
        var store = new ActivityJsonlStore(dataPath);
        await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
            store.Append(new(DateTimeOffset.UtcNow, "store", "concurrency", "session",
                "TOOL_COMPLETED", "TOOL", ActivityId: $"parallel-{index}")))));
        var records = store.ReadLatest("store", "concurrency", 100);
        var copy = Path.Combine(dataPath, "activity-copy-test.jsonl");
        if (records.Count != 16 || !store.CopyTo("store", "concurrency", copy)
            || File.ReadLines(copy).Count() != 16)
            throw new InvalidOperationException("활동 JSONL 동시 기록·내보내기 검증이 실패했습니다.");

        var staleId = "stale-session";
        var sessions = new TrackingSessionStore(dataPath);
        sessions.Save(staleId, new("demo", "old", null, DateTimeOffset.UtcNow.AddDays(-31)));
        if (sessions.Load(staleId) is not null)
            throw new InvalidOperationException("오래된 활동 매핑 만료가 실패했습니다.");
    }
}
