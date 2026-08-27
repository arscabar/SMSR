using System.IO;

namespace SMSR.App.Services;

public sealed class LocalActivityLog(string directory)
{
    private const long MaxBytes = 1_000_000;
    public string Path { get; } = System.IO.Path.Combine(directory, "activity.log");
    public string PreviousPath { get; } = System.IO.Path.Combine(directory, "activity.previous.log");

    public async Task WriteAsync(string message)
    {
        Directory.CreateDirectory(directory);
        if (File.Exists(Path) && new FileInfo(Path).Length >= MaxBytes)
            File.Move(Path, PreviousPath, true);
        await File.AppendAllTextAsync(Path, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
    }
}
