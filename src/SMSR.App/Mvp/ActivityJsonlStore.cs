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

    public bool CopyTo(string projectId, string workflowId, string destination)
        => ActivityFileLock.Run(Key(projectId, workflowId), () =>
        {
            var path = PathFor(projectId, workflowId);
            if (!File.Exists(path)) return false;
            File.Copy(path, destination, true);
            return true;
        });

    private static ActivityRecord? Parse(string line)
    {
        try { return JsonSerializer.Deserialize<ActivityRecord>(line, Json); }
        catch { return null; }
    }

    private static void TryRotate(string path)
    {
        try { File.Move(path, path + ".previous", true); }
        catch (IOException) { }
    }

    private static string Key(string projectId, string workflowId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(projectId + "\n" + workflowId)))[..24];
}
