namespace SMSR.App.Mvp;

internal static class WorkflowIdGenerator
{
    public static string Create(string projectId, DateTimeOffset? now = null)
    {
        var project = projectId.Trim();
        if (project.Length > 96) project = project[..96];
        return $"{project}__{(now ?? DateTimeOffset.Now):yyyyMMdd-HHmmssfff}";
    }
}
