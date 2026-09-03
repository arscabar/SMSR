using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal sealed class TrackingSessionStore(string dataPath)
{
    private readonly string _root = Path.Combine(dataPath, "tracking-sessions");

    public TrackingSession? Load(string sessionId)
    {
        var path = PathFor(sessionId);
        if (!File.Exists(path)) return null;
        try
        {
            var session = JsonSerializer.Deserialize<TrackingSession>(File.ReadAllText(path));
            if (session is not null && session.UpdatedAtUtc < DateTimeOffset.UtcNow.AddDays(-30))
            {
                File.Delete(path);
                return null;
            }
            return session;
        }
        catch { return null; }
    }

    public void Save(string sessionId, TrackingSession session)
    {
        Directory.CreateDirectory(_root);
        var path = PathFor(sessionId);
        var temp = path + ".tmp." + Environment.ProcessId;
        File.WriteAllText(temp, JsonSerializer.Serialize(session), new UTF8Encoding(false));
        File.Move(temp, path, true);
    }

    public void Remove(string sessionId)
    {
        var path = PathFor(sessionId);
        if (File.Exists(path)) File.Delete(path);
    }

    public void RemoveWorkflow(string projectId, string workflowId)
        => RemoveWhere(item => item.ProjectId == projectId && item.WorkflowId == workflowId);

    public void RemoveProject(string projectId)
        => RemoveWhere(item => item.ProjectId == projectId);

    public void Clear()
    {
        if (!Directory.Exists(_root)) return;
        foreach (var path in Directory.EnumerateFiles(_root, "*.json"))
            try { File.Delete(path); }
            catch (IOException) { }
    }

    private void RemoveWhere(Func<TrackingSession, bool> predicate)
    {
        if (!Directory.Exists(_root)) return;
        foreach (var path in Directory.EnumerateFiles(_root, "*.json"))
            try
            {
                var item = JsonSerializer.Deserialize<TrackingSession>(File.ReadAllText(path));
                if (item is not null && predicate(item)) File.Delete(path);
            }
            catch { }
    }

    private string PathFor(string sessionId)
        => Path.Combine(_root, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)))[..24] + ".json");
}
