using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SMSR.App.Views;

internal sealed class PetImagePlayer(System.Windows.Controls.Image image) : IDisposable
{
    private readonly DispatcherTimer _timer = new();
    private IReadOnlyList<BitmapFrame> _frames = [];
    private IReadOnlyList<int> _delays = [];
    private int _index;

    public void Load(string path)
    {
        _timer.Stop();
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        _frames = decoder.Frames.Select(BitmapFrame.Create).ToArray();
        _delays = decoder.Frames.Select(Delay).ToArray();
        _index = 0;
        image.Source = _frames[0];
        if (_frames.Count < 2) return;
        _timer.Tick -= NextFrame;
        _timer.Tick += NextFrame;
        _timer.Interval = TimeSpan.FromMilliseconds(_delays[0]);
        _timer.Start();
    }

    public void Dispose() => _timer.Stop();

    private void NextFrame(object? sender, EventArgs e)
    {
        _index = (_index + 1) % _frames.Count;
        image.Source = _frames[_index];
        _timer.Interval = TimeSpan.FromMilliseconds(_delays[_index]);
    }

    private static int Delay(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata metadata
                && metadata.GetQuery("/grctlext/Delay") is ushort delay && delay > 0)
                return Math.Max(20, delay * 10);
        }
        catch { }
        return 100;
    }
}
