using System.Collections.ObjectModel;
using System.IO;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed class PetMediaRuleEditor(int startProgress, int endProgress, string mediaPath)
{
    public int StartProgress { get; } = startProgress;
    public int EndProgress { get; } = endProgress;
    public string MediaPath { get; } = mediaPath;
    public string FileName => Path.GetFileName(MediaPath);
    public string RangeLabel => $"{StartProgress} ~ {(EndProgress == 100 ? 100 : EndProgress + 1)}%";
}

public sealed record PetBoundaryEditor(int Index, int Value, int Minimum, int Maximum)
{
    public string Label => $"미디어 {Index + 1} · {Index + 2} 경계";
    public string ValueLabel => $"{Value}%";
}

public sealed partial class SettingsViewModel
{
    private PetMediaRuleEditor? _selectedPetMediaRule;
    private readonly RelayCommand _registerPetCommand;
    private readonly RelayCommand _removePetCommand;
    private readonly RelayCommand _removePetMediaRuleCommand;
    private readonly RelayCommand _registerIdlePetCommand;
    private readonly RelayCommand _removeIdlePetCommand;

    public ObservableCollection<PetMediaRuleEditor> PetMediaRules { get; } = [];
    public ObservableCollection<PetBoundaryEditor> PetBoundaries { get; } = [];
    public PetMediaRuleEditor? SelectedPetMediaRule
    {
        get => _selectedPetMediaRule;
        set { if (SetField(ref _selectedPetMediaRule, value)) _removePetMediaRuleCommand.NotifyCanExecuteChanged(); }
    }

    public bool PetEnabled
    {
        get => _settings.Current.PetEnabled;
        set => Update(_settings.Current with { PetEnabled = value && HasPetImage }, nameof(PetEnabled));
    }

    public string PetName
    {
        get => _settings.Current.PetName;
        set
        {
            var normalized = (value ?? "").Trim();
            if (normalized.Length > 40) normalized = normalized[..40];
            Update(_settings.Current with { PetName = normalized }, nameof(PetName));
        }
    }

    public string PetImagePath => _settings.Current.PetImagePath;
    public bool HasPetImage => PetMediaSelector.Rules(_settings.Current).Count > 0;
    public bool HasIdlePet => File.Exists(_settings.Current.PetIdleMediaPath);
    public string IdlePetMediaName => HasIdlePet ? Path.GetFileName(_settings.Current.PetIdleMediaPath) : "미등록";
    public int PetSizePercent
    {
        get => _settings.Current.PetSizePercent;
        set
        {
            var size = Math.Clamp(value, 60, 180);
            if (size == _settings.Current.PetSizePercent) return;
            Update(_settings.Current with { PetSizePercent = size }, nameof(PetSizePercent));
            OnPropertyChanged(nameof(PetSizeLabel));
        }
    }
    public string PetSizeLabel => $"{PetSizePercent}%";
    public RelayCommand RegisterPetCommand => _registerPetCommand;
    public RelayCommand RemovePetCommand => _removePetCommand;
    public RelayCommand RemovePetMediaRuleCommand => _removePetMediaRuleCommand;
    public RelayCommand RegisterIdlePetCommand => _registerIdlePetCommand;
    public RelayCommand RemoveIdlePetCommand => _removeIdlePetCommand;

    private void LoadPetRules()
    {
        SelectedPetMediaRule = null;
        PetMediaRules.Clear();
        PetBoundaries.Clear();
        var rules = PetMediaSelector.Rules(_settings.Current);
        foreach (var rule in rules)
            PetMediaRules.Add(new(rule.StartProgress, rule.EndProgress, rule.MediaPath));
        for (var index = 0; index < rules.Count - 1; index++)
            PetBoundaries.Add(new(index, rules[index + 1].StartProgress, rules[index].StartProgress + 1,
                index + 2 < rules.Count ? rules[index + 2].StartProgress - 1 : 100));
    }

    public void SetPetBoundary(int index, int value)
    {
        var current = PetMediaRules.Select(rule => new PetMediaRule(
            rule.StartProgress, rule.EndProgress, rule.MediaPath)).ToArray();
        ApplyPetRules(PetMediaSelector.MoveBoundary(current, index, value), null);
        StatusMessage = "펫 진행률 경계를 저장했습니다.";
    }

}
