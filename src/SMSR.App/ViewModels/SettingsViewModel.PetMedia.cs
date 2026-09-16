using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private const string PetMediaFilter = "펫 미디어|*.png;*.jpg;*.jpeg;*.apng;*.gif;*.mp4";

    private void RegisterPet()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = PetMediaFilter,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var path = PetAssetStore.Register(dialog.FileName, DataPath);
            ApplyPetMedia(PetMediaRules.Select(item => item.MediaPath).Append(path), path);
            StatusMessage = $"미디어 {PetMediaRules.Count}개를 진행률 구간에 자동 배치했습니다.";
        }
        catch (Exception exception) { StatusMessage = $"펫 미디어를 등록하지 못했습니다: {exception.Message}"; }
    }

    private void RegisterIdlePet()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = PetMediaFilter, CheckFileExists = true };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var path = PetAssetStore.Register(dialog.FileName, DataPath);
            _settings.Save(_settings.Current with { PetIdleMediaPath = path });
            OnPropertyChanged(nameof(HasIdlePet));
            OnPropertyChanged(nameof(IdlePetMediaName));
            _removeIdlePetCommand.NotifyCanExecuteChanged();
            StatusMessage = "완료 확인 후 표시할 대기 미디어를 저장했습니다.";
        }
        catch (Exception exception) { StatusMessage = $"대기 미디어를 등록하지 못했습니다: {exception.Message}"; }
    }

    private void RemoveIdlePet()
    {
        _settings.Save(_settings.Current with { PetIdleMediaPath = "" });
        OnPropertyChanged(nameof(HasIdlePet));
        OnPropertyChanged(nameof(IdlePetMediaName));
        _removeIdlePetCommand.NotifyCanExecuteChanged();
        StatusMessage = "대기 미디어 등록을 해제했습니다.";
    }

    private void RemoveSelectedPetMediaRule()
    {
        if (SelectedPetMediaRule is null) return;
        ApplyPetMedia(PetMediaRules.Where(item => item != SelectedPetMediaRule).Select(item => item.MediaPath), null);
        StatusMessage = PetMediaRules.Count == 0 ? "펫 등록을 해제했습니다." : "남은 미디어 구간을 자동으로 다시 배치했습니다.";
    }

    private void ApplyPetMedia(IEnumerable<string> mediaPaths, string? selectedPath)
        => ApplyPetRules(PetMediaSelector.Distribute(mediaPaths), selectedPath);

    private void ApplyPetRules(IReadOnlyList<PetMediaRule> rules, string? selectedPath)
    {
        _settings.Save(_settings.Current with
        {
            PetMediaRules = rules,
            PetImagePath = rules.FirstOrDefault()?.MediaPath ?? "",
            PetEnabled = rules.Count > 0
        });
        LoadPetRules();
        SelectedPetMediaRule = PetMediaRules.FirstOrDefault(item => item.MediaPath == selectedPath);
    }

    private void RemovePet()
    {
        _settings.Save(_settings.Current with { PetEnabled = false, PetImagePath = "", PetMediaRules = [] });
        PetMediaRules.Clear();
        PetBoundaries.Clear();
        SelectedPetMediaRule = null;
        StatusMessage = "펫 등록을 해제했습니다. 복사된 미디어는 데이터 폴더에 보존됩니다.";
    }
}
