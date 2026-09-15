using SMSR.App.ViewModels;

namespace SMSR.App.Views;

public partial class SettingsAiPanel : System.Windows.Controls.UserControl
{
    private bool _isGeminiApiKeyVisible;

    public SettingsAiPanel() => InitializeComponent();

    private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && string.IsNullOrEmpty(GeminiApiKey.Password))
            GeminiApiKey.Password = viewModel.LoadGeminiApiKey();
    }

    private void ShowGeminiApiKey_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _isGeminiApiKeyVisible = !_isGeminiApiKeyVisible;
        if (_isGeminiApiKeyVisible)
        {
            VisibleGeminiApiKey.Text = GeminiApiKey.Password;
            GeminiApiKey.Visibility = System.Windows.Visibility.Collapsed;
            VisibleGeminiApiKey.Visibility = System.Windows.Visibility.Visible;
            ShowGeminiApiKey.ToolTip = "API 키 숨기기";
            return;
        }
        GeminiApiKey.Password = VisibleGeminiApiKey.Text;
        VisibleGeminiApiKey.Visibility = System.Windows.Visibility.Collapsed;
        GeminiApiKey.Visibility = System.Windows.Visibility.Visible;
        ShowGeminiApiKey.ToolTip = "API 키 보기";
    }

    private async void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel) return;
        var apiKey = _isGeminiApiKeyVisible ? VisibleGeminiApiKey.Text : GeminiApiKey.Password;
        await viewModel.SaveGeminiApiKeyAsync(apiKey);
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
        VisibleGeminiApiKey.Clear();
    }
}
