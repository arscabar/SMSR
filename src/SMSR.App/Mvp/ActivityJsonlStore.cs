using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed class ActivityJsonlStore(string dataPath)
{
    private const long MaxBytes = 5_000_000;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly string _root = Path.Combine(dataPath, "activity");

    public string PathFor(string projectId, string workflowId)
        => Path.Combine(_root, Key(projectId, workflowId) + ".jsonl");

    public bool Append(ActivityRecord record)
    {
        Directory.CreateDirectory(_root);
        var path = PathFor(record.ProjectId, record.WorkflowId);
        return ActivityFileLock.Run(Key(record.ProjectId, record.WorkflowId), () =>
        {
            if (!string.IsNullOrWhiteSpace(record.ActivityId) && File.Exists(path)
                && File.ReadLines(path).TakeLast(100).Select(Parse)
                .Any(item => item?.ActivityId == record.ActivityId)) return false;
            if (File.Exists(path) && new FileInfo(path).Length >= MaxBytes)
                TryRotate(path);
            var line = JsonSerializer.Serialize(record, Json) + Environment.NewLine;
            File.AppendAllText(path, line, new UTF8Encoding(false));
            return true;
        });
    }

    public IReadOnlyList<ActivityRecord> ReadLatest(string projectId, string workflowId, int count = 20)
    {
        var key = Key(projectId, workflowId);
        return ActivityFileLock.Run(key, () =>
        {
            var path = PathFor(projectId, workflowId);
            if (!File.Exists(path)) return [];
            return File.ReadLines(path).TakeLast(Math.Clamp(count, 1, 100)).Select(Parse)
                .Where(item => item is not null).Cast<ActivityRecord>().Reverse().ToArray();
        });
    }

    public TokenUsageSummary ReadTokenUsage(string projectId, string workflowId)
    {
        var graph = ReadAll(PathFor(projectId, workflowId)).ToArray();
        var goalId = graph.OrderByDescending(item => item.TimestampUtc)
            .Select(item => item.GoalId).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        IReadOnlyList<ActivityRecord> goal = string.IsNullOrWhiteSpace(goalId) ? []
            : graph.Where(item => item.GoalId == goalId).ToArray();
        var graphUsage = SumLatest(graph, item => item.GraphInputTokens, item => item.GraphOutputTokens);
        var goalUsage = SumLatest(goal, item => item.SessionInputTokens, item => item.SessionOutputTokens);
        var goalLatest = Latest(goal, item => item.SessionInputTokens, item => item.SessionOutputTokens);
        var hasBreakdown = goalLatest.Length > 0
            && goalLatest.All(item => item.SessionCachedInputTokens is not null);
        return new(goalUsage.Input, goalUsage.Output, graphUsage.Input, graphUsage.Output,
            goalUsage.HasValue, graphUsage.HasValue,
            hasBreakdown ? goalLatest.Sum(item => item.SessionCachedInputTokens!.Value) : 0,
            hasBreakdown);
    }

    public bool CopyTo(string projectId, string workflowId, string destination)
        => ActivityFileLock.Run(Key(projectId, workflowId), () =>
        {
            var path = PathFor(projectId, workflowId);
            if (!File.Exists(path)) return false;
            File.Copy(path, destination, true);
            return true;
        });

    public void Delete(string projectId, string workflowId)
        => DeleteKey(Key(projectId, workflowId));

    public void DeleteProject(string projectId)
    {
        if (!Directory.Exists(_root)) return;
        foreach (var path in Directory.EnumerateFiles(_root, "*.jsonl*"))
            try
            {
                var record = File.ReadLines(path).Select(Parse).FirstOrDefault(item => item is not null);
                if (record?.ProjectId == projectId) DeleteKey(Path.GetFileName(path).Split('.')[0]);
            }
            catch (IOException) { }
    }

    public void Clear()
    {
        if (!Directory.Exists(_root)) return;
        var keys = Directory.EnumerateFiles(_root, "*.jsonl*")
            .Select(path => Path.GetFileName(path).Split('.')[0]).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var key in keys) DeleteKey(key);
    }

    private static ActivityRecord? Parse(string line)
    {
        try { return JsonSerializer.Deserialize<ActivityRecord>(line, Json); }
        catch { return null; }
    }

    private static IEnumerable<ActivityRecord> ReadAll(string path)
    {
        foreach (var candidate in new[] { path + ".previous", path })
        {
            if (!File.Exists(candidate)) continue;
            IEnumerable<string> lines;
            try { lines = File.ReadLines(candidate).ToArray(); }
            catch (IOException) { continue; }
            foreach (var line in lines)
                if (Parse(line) is { } record) yield return record;
        }
    }

    private static (long Input, long Output, bool HasValue) SumLatest(IEnumerable<ActivityRecord> records,
        Func<ActivityRecord, long?> input, Func<ActivityRecord, long?> output)
    {
        var latest = Latest(records, input, output);
        return (latest.Sum(item => input(item)!.Value), latest.Sum(item => output(item)!.Value), latest.Length > 0);
    }

    private static ActivityRecord[] Latest(IEnumerable<ActivityRecord> records,
        Func<ActivityRecord, long?> input, Func<ActivityRecord, long?> output)
        => records.Where(item => input(item) is not null && output(item) is not null)
            .GroupBy(item => item.SessionId, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(item => item.TimestampUtc).First()).ToArray();

    private static void TryRotate(string path)
    {
        try { File.Move(path, path + ".previous", true); }
        catch (IOException) { }
    }

    private void DeleteKey(string key)
        => ActivityFileLock.Run(key, () =>
        {
            var path = Path.Combine(_root, key + ".jsonl");
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".previous")) File.Delete(path + ".previous");
            return true;
        });

    private static string Key(string projectId, string workflowId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(projectId + "\n" + workflowId)))[..24];
}
