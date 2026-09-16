using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SMSR.App.Services;

namespace SMSR.App.Views;

public partial class PetWindow : Window
{
    private string _imagePath = "";
    private readonly PetImagePlayer _imagePlayer;
    public PetWindow()
    {
        InitializeComponent();
        _imagePlayer = new(PetImage);
        Loaded += (_, _) => PlaceBottomRight();
        Closed += (_, _) => _imagePlayer.Dispose();
    }

    internal void UpdatePet(string imagePath, string name, string graphTitle, PetPresentation presentation)
    {
        if (_imagePath != imagePath)
        {
            LoadMedia(imagePath);
            _imagePath = imagePath;
        }
        PetNameText.Text = string.IsNullOrWhiteSpace(name) ? "SMSR 펫" : name;
        PetStatusText.Text = $"{presentation.Label} · {presentation.Progress}%\n{graphTitle}";
        PetProgress.Value = presentation.Progress;
        Animate(presentation.Status);
    }

    private void Animate(string status)
    {
        PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
        PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
        PetMove.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
        PetVisual.BeginAnimation(OpacityProperty, null);
        var repeat = RepeatBehavior.Forever;
        if (status is "IN_PROGRESS" or "VALIDATING" or "RETRYING")
        {
            var pulse = new DoubleAnimation(1, 1.06, TimeSpan.FromMilliseconds(700)) { AutoReverse = true, RepeatBehavior = repeat };
            PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pulse);
            PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pulse);
        }
        else if (status is "BLOCKED" or "FAILED")
            PetVisual.BeginAnimation(OpacityProperty, new DoubleAnimation(1, .55, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = repeat });
        else if (status == "SUCCESS")
            PetMove.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(0, -8, TimeSpan.FromMilliseconds(450)) { AutoReverse = true, RepeatBehavior = new(3) });
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
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
