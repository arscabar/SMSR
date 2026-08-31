using System.Windows.Input;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private ICommand? _resetPlanningPromptCommand;

    public bool RequirePlanReview
    {
        get => _settings.Current.RequirePlanReview;
        set => Update(_settings.Current with { RequirePlanReview = value }, nameof(RequirePlanReview));
    }

    public string PlanningPrompt
    {
        get => _settings.Current.PlanningPrompt;
        set => Update(_settings.Current with
        {
            PlanningPrompt = PlanningPromptSettings.Normalize(value)
        }, nameof(PlanningPrompt));
    }

    public ICommand ResetPlanningPromptCommand => _resetPlanningPromptCommand ??= new RelayCommand(() =>
    {
        Update(_settings.Current with { PlanningPrompt = PlanningPromptSettings.Default }, nameof(PlanningPrompt));
        StatusMessage = "작업계획서 프롬프트를 기본값으로 복원했습니다.";
    });
}
