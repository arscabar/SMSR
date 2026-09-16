using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SMSR.App.Services;

namespace SMSR.App.Views;

public partial class PetWindow : Window
{
    private string _imagePath = "";
    private readonly PetImagePlayer _imagePlayer;
    public event EventHandler? CompletionAcknowledged;
    public event EventHandler? OpenRequested;
    public PetWindow()
    {
        InitializeComponent();
        _imagePlayer = new(PetImage);
        Loaded += (_, _) => PlaceBottomRight();
        Closed += (_, _) => _imagePlayer.Dispose();
    }

    internal void UpdatePet(string imagePath, PetPresentation presentation, int sizePercent)
    {
        if (_imagePath != imagePath)
        {
            LoadMedia(imagePath);
            _imagePath = imagePath;
        }
        PetProgressText.Text = $"{presentation.Progress}%";
        PetProgressPanel.Visibility = presentation.Status == "IDLE" ? Visibility.Collapsed : Visibility.Visible;
        System.Windows.Controls.ContextMenuService.SetIsEnabled(
            PetRoot, presentation is { Status: "SUCCESS", Progress: 100 });
        ApplySize(sizePercent);
        Animate(presentation.Status);
    }

    private void LoadMedia(string path)
    {
        PetVideo.Stop();
        var video = System.IO.Path.GetExtension(path).Equals(".mp4", StringComparison.OrdinalIgnoreCase);
        PetImage.Visibility = video ? Visibility.Collapsed : Visibility.Visible;
        PetVideo.Visibility = video ? Visibility.Visible : Visibility.Collapsed;
        if (video)
        {
            _imagePlayer.Dispose();
            PetVideo.Source = new Uri(path, UriKind.Absolute);
            PetVideo.Play();
        }
        else _imagePlayer.Load(path);
    }

    private void PetVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        PetVideo.Position = TimeSpan.Zero;
        PetVideo.Play();
    }

    private void PlaceBottomRight()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 18;
        Top = area.Bottom - Height - 18;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            OpenRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void CompleteMenuItem_Click(object sender, RoutedEventArgs e)
        => CompletionAcknowledged?.Invoke(this, EventArgs.Empty);
}
