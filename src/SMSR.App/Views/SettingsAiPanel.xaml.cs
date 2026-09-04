using SMSR.App.ViewModels;

namespace SMSR.App.Views;

public partial class SettingsAiPanel : System.Windows.Controls.UserControl
{
    public SettingsAiPanel() => InitializeComponent();

    private async void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel) return;
        await viewModel.SaveGeminiApiKeyAsync(GeminiApiKey.Password);
        GeminiApiKey.Clear();
    }

    private async void TestButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel) await viewModel.TestGeminiAsync();
    }

    private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel) return;
        viewModel.DeleteGeminiApiKey();
        GeminiApiKey.Clear();
    }
}
