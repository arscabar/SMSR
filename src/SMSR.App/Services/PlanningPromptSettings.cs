namespace SMSR.App.Services;

public static class PlanningPromptSettings
{
    public const int MaximumLength = 6000;
    internal const string LegacyDefault = """
        새 기능 개발이나 여러 파일을 변경하는 작업은 구현 전에 작업계획서를 작성하세요. 계획에는 목표, 범위, 계층형 작업, 완료 조건, 검증 방법을 포함하세요. 계획을 사용자에게 보여주고 승인 또는 수정 요청을 받은 뒤 구현을 시작하세요. 사용자가 계획 검토 생략을 명시하면 바로 진행할 수 있습니다.
        """;
    public const string Default = """
        사용자의 원래 작업 요청을 변경하거나 요구사항을 추가하지 말고, 구현 전에 실행 가능한 작업계획서 초안을 작성하세요. 계획 초안을 사용자에게 보여주고 수정 의견 또는 진행 승인을 받은 뒤 작업을 시작하세요. 사용자가 계획 검토를 생략하라고 명시한 경우에는 바로 진행하세요.
        """;

    public static string Normalize(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? Default : value.Trim();
        if (text == LegacyDefault) text = Default;
        return text.Length <= MaximumLength ? text : text[..MaximumLength];
    }

    public static string Expand(string value, string projectId, string taskId)
        => Normalize(value).Replace("{projectId}", projectId, StringComparison.Ordinal)
            .Replace("{taskId}", taskId, StringComparison.Ordinal);
}
