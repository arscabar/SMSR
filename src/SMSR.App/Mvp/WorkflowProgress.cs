namespace SMSR.App.Mvp;

internal static class WorkflowProgress
{
    public static int Value(string status, int? progressPercentage)
        => status == "SUCCESS" ? 100 : Math.Clamp(progressPercentage ?? 0, 0, 100);

    public static int Value(StateNode? node)
        => node is null ? 0 : Value(node.Status, node.ProgressPercentage);
}
