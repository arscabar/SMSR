namespace SMSR.App.Mvp;

public sealed class GraphValidationService(EventStore store, GraphEvidenceService evidence)
{
    public async Task<GraphValidationGaps> GetAsync(string projectId, string workflowId,
        CancellationToken ct = default)
    {
        var links = await evidence.GetAsync(projectId, workflowId, ct);
        var verifications = await store.GetWorkflowVerificationsAsync(projectId, workflowId, ct);
        var files = links.Matches.Count(item => item.FileNode is not null);
        var unresolved = links.Matches.Count - files;
        return new(workflowId, files, unresolved, verifications,
            files > 0 && verifications.Count == 0, links.Truncated, links.Revision);
    }
}
