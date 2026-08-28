using System.IO;

namespace SMSR.App.Mvp;

internal sealed class OAuthAuditLog(string directory)
{
    private const long MaxBytes = 1_000_000;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string Path { get; } = System.IO.Path.Combine(directory, "oauth.log");

    public async Task WriteAsync(string stage, string outcome)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(directory);
            if (File.Exists(Path) && new FileInfo(Path).Length >= MaxBytes)
                File.Move(Path, Path + ".previous", true);
            var line = $"{DateTimeOffset.UtcNow:O} {stage} {outcome}{Environment.NewLine}";
            await File.AppendAllTextAsync(Path, line).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }
}
