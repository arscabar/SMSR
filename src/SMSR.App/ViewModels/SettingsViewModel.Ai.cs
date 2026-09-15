namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private string _geminiStatus = "Gemini를 사용하려면 API 키를 저장하세요.";
    private IReadOnlyList<string> _geminiModels = [Services.GeminiSummaryClient.DefaultModel];

    public bool HasGeminiApiKey => _geminiCredentials.Exists;
    public IReadOnlyList<string> GeminiModels
    {
        get => _geminiModels;
        private set => SetField(ref _geminiModels, value);
    }
    public string GeminiModel
    {
        get => _settings.Current.GeminiModel;
        set
        {
            var model = Services.GeminiSummaryClient.NormalizeModel(value);
            if (model == GeminiModel) return;
            Update(_settings.Current with { GeminiModel = model }, nameof(GeminiModel));
            OnPropertyChanged(nameof(GeminiKeyLabel));
            GeminiStatus = $"{model} 모델을 저장했습니다. 연결 확인을 실행하세요.";
        }
    }
    public string GeminiKeyLabel => HasGeminiApiKey
        ? $"Gemini API 키 설정됨 · {GeminiModel}"
        : "Gemini API 키 미설정 · 요약 시 Codex 요청으로 전환";
    public string GeminiStatus
    {
        get => _geminiStatus;
        private set => SetField(ref _geminiStatus, value);
    }

    public string LoadGeminiApiKey()
    {
        try { return _geminiCredentials.Read() ?? ""; }
        catch (Exception exception) { GeminiStatus = $"Gemini 키를 불러오지 못했습니다: {exception.Message}"; return ""; }
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
            GeminiModels = await _gemini.GetModelsAsync();
            if (!GeminiModels.Contains(GeminiModel))
                throw new InvalidOperationException($"{GeminiModel} 모델을 이 API 키에서 사용할 수 없습니다.");
            await _gemini.TestAsync(GeminiModel);
            GeminiStatus = $"API와 {GeminiModel} 응답을 확인했습니다. 요약 모델 후보 {GeminiModels.Count}개.";
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
