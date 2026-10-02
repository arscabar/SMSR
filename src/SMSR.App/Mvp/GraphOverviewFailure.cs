namespace SMSR.App.Mvp;

internal static class GraphOverviewFailure
{
    internal static string Describe(Task task)
    {
        var error=task.Exception?.GetBaseException();
        if(task.IsCanceled||error is OperationCanceledException)
            return "ANALYSIS_CANCELLED · 앱 종료 또는 분석 취소입니다. 앱 실행 상태를 확인한 뒤 다시 조회하세요.";
        if(error is OutOfMemoryException)
            return "MEMORY_LIMIT · 분석 메모리가 부족합니다. 범위를 줄인 뒤 다시 조회하세요.";
        if(error is TimeoutException||error is InvalidOperationException&&error.Message.StartsWith("로컬 분석이 ",StringComparison.Ordinal))
            return "TIME_LIMIT · 분석 시간 제한을 초과했습니다. 범위를 줄인 뒤 다시 조회하세요.";
        if(error is ArgumentException)
            return "INPUT_LIMIT · 입력 크기 또는 분석 계약을 확인하세요.";
        return "WORKER_FAILURE · 로컬 작업기 또는 결과 저장에 실패했습니다. 도구 상태·리비전을 확인한 뒤 다시 조회하세요.";
    }
}
