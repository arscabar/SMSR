using System.IO;

namespace SMSR.App.Services;

public sealed class LocalActivityLog(string directory)
{
    public string Path { get; } = System.IO.Path.Combine(directory, "activity.log");

    public async Task WriteAsync(string message)
    {
        Directory.CreateDirectory(directory);
        await File.AppendAllTextAsync(Path, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
    }
}
