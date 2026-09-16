using System.Windows;
using System.Windows.Media.Animation;

namespace SMSR.App.Views;

public partial class PetWindow
{
    private int _sizePercent;

    private void ApplySize(int percent)
    {
        percent = Math.Clamp(percent, 60, 180);
        if (_sizePercent == percent) return;
        _sizePercent = percent;
        var size = 150 * percent / 100d;
        PetImage.Width = PetImage.Height = size;
        PetVideo.Width = PetVideo.Height = size;
    }

    private void Animate(string status)
    {
        PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
        PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
        PetMove.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);
        PetMediaVisual.BeginAnimation(OpacityProperty, null);
        var repeat = RepeatBehavior.Forever;
        if (status is "IN_PROGRESS" or "VALIDATING" or "RETRYING" or "IDLE")
        {
            var duration = status == "IDLE" ? 1500 : 700;
            var target = status == "IDLE" ? 1.04 : 1.06;
            var pulse = new DoubleAnimation(1, target, TimeSpan.FromMilliseconds(duration))
                { AutoReverse = true, RepeatBehavior = repeat };
            PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, pulse);
            PetScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, pulse);
            if (status == "IDLE")
                PetMove.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
                    new DoubleAnimation(0, -4, TimeSpan.FromMilliseconds(duration))
                        { AutoReverse = true, RepeatBehavior = repeat });
        }
        else if (status is "BLOCKED" or "FAILED")
            PetMediaVisual.BeginAnimation(OpacityProperty, new DoubleAnimation(1, .55,
                TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = repeat });
        else if (status == "SUCCESS")
            PetMove.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
                new DoubleAnimation(0, -8, TimeSpan.FromMilliseconds(450))
                    { AutoReverse = true, RepeatBehavior = new(3) });
    }
}
