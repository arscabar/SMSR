namespace SMSR.App.Mvp;

public sealed record GraphRoleBatchRequest(string ProjectId, string[] Folders, int ExpectedRevision, bool Confirm = false);
public sealed record GraphRoleBatchFile(string NodeId, string Path, string Status, string? Reason, string? Fingerprint);
public sealed record GraphRoleBatchPreview(int Revision, string[] Folders, GraphRoleBatchFile[] Files)
{
    public int Eligible => Files.Count(f => f.Status == "READY");
    public int Current => Files.Count(f => f.Status == "CURRENT");
    public int Excluded => Files.Count(f => f.Status == "EXCLUDED");
}
public sealed record GraphRoleBatchItem(string NodeId, string Fingerprint, string? Error = null, bool Retry = false);
public sealed record GraphRoleBatch(string Id, string[] Folders, GraphRoleBatchItem[] Items, bool Paused, int Reused, int Excluded);
public sealed record GraphRoleBatchStatus(GraphRoleBatch? Batch, int Pending, int Queued, int Running, int Success, int Failed);
public sealed record GraphRoleBatchControl(string ProjectId, string Action);
