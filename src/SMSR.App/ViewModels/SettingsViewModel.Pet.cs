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

public sealed partial class SettingsViewModel
{
    private PetMediaRuleEditor? _selectedPetMediaRule;
    private readonly RelayCommand _registerPetCommand;
    private readonly RelayCommand _removePetCommand;
    private readonly RelayCommand _removePetMediaRuleCommand;

    public ObservableCollection<PetMediaRuleEditor> PetMediaRules { get; } = [];
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
    public RelayCommand RegisterPetCommand => _registerPetCommand;
    public RelayCommand RemovePetCommand => _removePetCommand;
    public RelayCommand RemovePetMediaRuleCommand => _removePetMediaRuleCommand;

    private void LoadPetRules()
    {
        SelectedPetMediaRule = null;
        PetMediaRules.Clear();
        foreach (var rule in PetMediaSelector.Distribute(PetMediaSelector.Rules(_settings.Current).Select(item => item.MediaPath)))
            PetMediaRules.Add(new(rule.StartProgress, rule.EndProgress, rule.MediaPath));
    }

}
