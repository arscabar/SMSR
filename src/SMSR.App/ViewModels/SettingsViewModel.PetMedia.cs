using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
    private void RegisterPet()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "펫 미디어|*.png;*.jpg;*.jpeg;*.apng;*.gif;*.mp4",
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

    private void RemoveSelectedPetMediaRule()
    {
        if (SelectedPetMediaRule is null) return;
        ApplyPetMedia(PetMediaRules.Where(item => item != SelectedPetMediaRule).Select(item => item.MediaPath), null);
        StatusMessage = PetMediaRules.Count == 0 ? "펫 등록을 해제했습니다." : "남은 미디어 구간을 자동으로 다시 배치했습니다.";
    }

    private void ApplyPetMedia(IEnumerable<string> mediaPaths, string? selectedPath)
    {
        var rules = PetMediaSelector.Distribute(mediaPaths);
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
        SelectedPetMediaRule = null;
        StatusMessage = "펫 등록을 해제했습니다. 복사된 미디어는 데이터 폴더에 보존됩니다.";
    }
}
