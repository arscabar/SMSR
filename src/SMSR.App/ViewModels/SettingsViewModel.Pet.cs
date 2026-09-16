using System.IO;
using SMSR.App.Infrastructure;
using SMSR.App.Services;

namespace SMSR.App.ViewModels;

public sealed partial class SettingsViewModel
{
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
    public bool HasPetImage => File.Exists(PetImagePath);
    public string PetImageLabel => HasPetImage ? PetImagePath : "등록되지 않음";
    public RelayCommand RegisterPetCommand => _registerPetCommand;
    public RelayCommand RemovePetCommand => _removePetCommand;

    private readonly RelayCommand _registerPetCommand;
    private readonly RelayCommand _removePetCommand;

    private void RegisterPet()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "이미지|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var path = PetAssetStore.Register(dialog.FileName, DataPath);
            _settings.Save(_settings.Current with { PetImagePath = path, PetEnabled = true });
            StatusMessage = "펫 이미지를 등록하고 표시했습니다.";
        }
        catch (Exception exception) { StatusMessage = $"펫 이미지를 등록하지 못했습니다: {exception.Message}"; }
    }

    private void RemovePet()
    {
        _settings.Save(_settings.Current with { PetEnabled = false, PetImagePath = "" });
        StatusMessage = "펫 등록을 해제했습니다. 복사된 이미지는 데이터 폴더에 보존됩니다.";
    }
}
