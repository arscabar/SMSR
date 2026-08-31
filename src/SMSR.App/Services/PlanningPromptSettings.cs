namespace SMSR.App.Services;

public static class PlanningPromptSettings
{
    public const int MaximumLength = 6000;
    public const string Default = """
        새 기능 개발이나 여러 파일을 변경하는 작업은 구현 전에 작업계획서를 작성하세요. 계획에는 목표, 범위, 계층형 작업, 완료 조건, 검증 방법을 포함하세요. 계획을 사용자에게 보여주고 승인 또는 수정 요청을 받은 뒤 구현을 시작하세요. 사용자가 계획 검토 생략을 명시하면 바로 진행할 수 있습니다.
        """;

    public static string Normalize(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? Default : value.Trim();
        return text.Length <= MaximumLength ? text : text[..MaximumLength];
    }

    public static string Expand(string value, string projectId, string taskId)
        => Normalize(value).Replace("{projectId}", projectId, StringComparison.Ordinal)
            .Replace("{taskId}", taskId, StringComparison.Ordinal);
}
