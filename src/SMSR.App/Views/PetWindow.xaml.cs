using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SMSR.App.Services;

namespace SMSR.App.Views;

public partial class PetWindow : Window
{
    private string _imagePath = "";
    public PetWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PlaceBottomRight();
    }

    internal void UpdatePet(string imagePath, string name, string graphTitle, PetPresentation presentation)
    {
        if (_imagePath != imagePath)
        {
            PetImage.Source = LoadImage(imagePath);
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
        PetImage.BeginAnimation(OpacityProperty, null);
        var repeat = RepeatBehavior.Forever;
        if (status is "IN_PROGRESS" or "VALIDATING" or "RETRYING")
        {
            var pulse = new DoubleAnimation(1, 1.06, TimeSpan.FromMilliseconds(700)) { AutoReverse = true, RepeatBehavior = repeat };
            PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pulse);
            PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pulse);
        }
        else if (status is "BLOCKED" or "FAILED")
            PetImage.BeginAnimation(OpacityProperty, new DoubleAnimation(1, .55, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = repeat });
        else if (status == "SUCCESS")
            PetMove.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(0, -8, TimeSpan.FromMilliseconds(450)) { AutoReverse = true, RepeatBehavior = new(3) });
    }

    private static BitmapImage LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
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
