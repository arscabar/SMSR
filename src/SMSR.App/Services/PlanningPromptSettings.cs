namespace SMSR.App.Services;

public static class PlanningPromptSettings
{
    public const int MaximumLength = 6000;
    internal const string LegacyDefault = """
        새 기능 개발이나 여러 파일을 변경하는 작업은 구현 전에 작업계획서를 작성하세요. 계획에는 목표, 범위, 계층형 작업, 완료 조건, 검증 방법을 포함하세요. 계획을 사용자에게 보여주고 승인 또는 수정 요청을 받은 뒤 구현을 시작하세요. 사용자가 계획 검토 생략을 명시하면 바로 진행할 수 있습니다.
        """;
    internal const string PreviousDefault = """
        사용자의 원래 작업 요청을 변경하거나 요구사항을 추가하지 말고, 구현 전에 실행 가능한 작업계획서 초안을 작성하세요. 계획 초안을 사용자에게 보여주고 수정 의견 또는 진행 승인을 받은 뒤 작업을 시작하세요. 사용자가 계획 검토를 생략하라고 명시한 경우에는 바로 진행하세요.
        """;
    public const string Default = """
        사용자의 원래 작업 요청을 변경하거나 요구사항을 추가하지 말고, 프로젝트의 기존 작업지시와 코드 패턴을 우선하여 구현 전 작업계획서 초안을 작성하세요.

        작업계획서에는 필요한 범위에서 다음 형식을 사용하세요.
        1. 목표와 작업 범위 및 제외 범위
        2. 계층형 작업 목록: 작업 ID, 제목, 상위 작업, 선행 작업, 담당 역할
        3. 각 작업의 구체적인 완료 기준
        4. 테스트, 빌드, 화면 확인 등 검증 방법
        5. 예상 산출물과 사용자 결정이 필요한 사항

        서로 독립적인 작업은 병렬 진행 가능 여부를 표시하세요. 같은 원인으로 3회 실패하면 재시도를 중단하고 실패 원인, 시도 내역과 필요한 결정을 보고하세요. 모든 필수 작업과 검증이 성공했을 때만 완료로 판정하세요. 계획 초안을 사용자에게 보여주고 수정 의견 또는 진행 승인을 받은 뒤 작업을 시작하세요. 사용자가 계획 검토 생략을 명시하면 바로 진행하세요. SMSR 그래프는 사용자가 명시적으로 요청했거나 설정에서 허용한 복잡한 프로젝트 변경에만 적용하고, 계산·검색·질문·작은 단일 수정에는 만들지 마세요.
        """;

    public static string Normalize(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? Default : value.Trim();
        if (text is LegacyDefault or PreviousDefault) text = Default;
        return text.Length <= MaximumLength ? text : text[..MaximumLength];
    }

    public static string Expand(string value, string projectId, string taskId)
        => Normalize(value).Replace("{projectId}", projectId, StringComparison.Ordinal)
            .Replace("{taskId}", taskId, StringComparison.Ordinal);
}
