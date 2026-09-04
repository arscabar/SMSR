namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private string _geminiStatus = "Gemini를 사용하려면 API 키를 저장하세요.";

    public bool HasGeminiApiKey => _geminiCredentials.Exists;
    public string GeminiKeyLabel => HasGeminiApiKey
        ? $"Gemini API 키 설정됨 · {Services.GeminiSummaryClient.Model}"
        : "Gemini API 키 미설정 · 요약 시 Codex 요청으로 전환";
    public string GeminiStatus
    {
        get => _geminiStatus;
        private set => SetField(ref _geminiStatus, value);
    }

    public async Task SaveGeminiApiKeyAsync(string apiKey)
    {
        try
        {
            _geminiCredentials.Save(apiKey);
            NotifyGeminiChanged();
            GeminiStatus = "API 키를 암호화해 저장했습니다. 연결을 확인하는 중입니다…";
            await TestGeminiAsync();
        }
        catch (Exception exception) { GeminiStatus = $"Gemini 키를 저장하지 못했습니다: {exception.Message}"; }
    }

    public async Task TestGeminiAsync()
    {
        if (!HasGeminiApiKey) { GeminiStatus = "저장된 Gemini API 키가 없습니다."; return; }
        try
        {
            GeminiStatus = "Gemini 연결을 확인하는 중입니다…";
            await _gemini.TestAsync();
            GeminiStatus = "Gemini 연결과 요약 응답을 확인했습니다.";
        }
        catch (Exception exception) { GeminiStatus = $"Gemini 연결 실패: {exception.Message}"; }
    }

    public void DeleteGeminiApiKey()
    {
        try
        {
            _geminiCredentials.Delete();
            NotifyGeminiChanged();
            GeminiStatus = "Gemini API 키를 삭제했습니다. 요약은 Codex 요청으로 전환됩니다.";
        }
        catch (Exception exception) { GeminiStatus = $"Gemini 키를 삭제하지 못했습니다: {exception.Message}"; }
    }

    private void NotifyGeminiChanged()
    {
        OnPropertyChanged(nameof(HasGeminiApiKey));
        OnPropertyChanged(nameof(GeminiKeyLabel));
    }
}
