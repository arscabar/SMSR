namespace SMSR.App.Mvp;

public sealed class GraphFeedbackService(EventStore store)
{
    public Task<GraphFeedback> RecordAsync(GraphFeedbackRequest request, CancellationToken ct = default)
    {
        var error = EventValidation.ValidateWorkflowIds(request.ProjectId, "graph");
        if (error is not null || new[] { request.SourceId, request.TargetId, request.Relation, request.OwnerPath }
                .Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 1024)
            || request.SourceLine is < 1 or > 1_000_000
            || request.Verdict is not ("USEFUL" or "ERROR" or "CORRECTED"))
            throw new ArgumentException(error ?? "관계 피드백 값이 올바르지 않습니다.");
        return store.RecordGraphFeedbackAsync(request, ct);
    }

    public Task<IReadOnlyList<GraphFeedback>> GetAsync(string projectId, string sourceId,
        CancellationToken ct = default)
    {
        var error = EventValidation.ValidateWorkflowIds(projectId, "graph");
        if (error is not null || string.IsNullOrWhiteSpace(sourceId) || sourceId.Length > 1024)
            throw new ArgumentException(error ?? "노드 ID가 올바르지 않습니다.");
        return store.GetGraphFeedbackAsync(projectId, sourceId, ct);
    }
}
