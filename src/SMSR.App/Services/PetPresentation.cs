using SMSR.App.Mvp;

namespace SMSR.App.Services;

internal sealed record PetPresentation(string Status, string Label, int Progress)
{
    private static readonly string[] Priority =
        ["BLOCKED", "FAILED", "RETRYING", "VALIDATING", "IN_PROGRESS", "CANCELLED", "PENDING", "SUCCESS"];

    public static PetPresentation From(IReadOnlyList<StateNode> nodes, IReadOnlyList<PlanNodeState>? planNodes = null)
    {
        if (nodes.Count == 0) return new("PENDING", "대기 중", 0);
        var status = Priority.FirstOrDefault(value => nodes.Any(node => node.Status == value)) ?? "PENDING";
        var progress = planNodes is { Count: > 0 }
            ? WorkflowProgress.Overall(new("", "", planNodes), new("", "", nodes))
            : (int)Math.Round(nodes.Average(node => WorkflowProgress.Value(node.Status, node.ProgressPercentage)));
        var label = status switch
        {
            "BLOCKED" => "확인이 필요해요", "FAILED" => "문제가 생겼어요", "RETRYING" => "다시 시도 중",
            "VALIDATING" => "검증 상태", "IN_PROGRESS" => "진행 상태", "CANCELLED" => "작업 중단",
            "SUCCESS" => "작업 완료", _ => "대기 중"
        };
        return new(status, label, Math.Clamp(progress, 0, 100));
    }
}
