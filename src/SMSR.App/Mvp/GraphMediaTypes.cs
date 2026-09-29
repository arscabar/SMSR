using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphMediaTypes
{
    private static readonly Dictionary<string, (string Kind, string Mime)> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = ("image", "image/png"), [".jpg"] = ("image", "image/jpeg"),
        [".jpeg"] = ("image", "image/jpeg"), [".gif"] = ("image", "image/gif"),
        [".webp"] = ("image", "image/webp"), [".bmp"] = ("image", "image/bmp"),
        [".avif"] = ("image", "image/avif"), [".mp4"] = ("video", "video/mp4"),
        [".webm"] = ("video", "video/webm"), [".ogv"] = ("video", "video/ogg"),
        [".mp3"] = ("audio", "audio/mpeg"), [".wav"] = ("audio", "audio/wav"),
        [".ogg"] = ("audio", "audio/ogg"), [".m4a"] = ("audio", "audio/mp4")
    };

    internal static bool TryGet(string path, out (string Kind, string Mime) type)
        => Types.TryGetValue(Path.GetExtension(path), out type);
}
