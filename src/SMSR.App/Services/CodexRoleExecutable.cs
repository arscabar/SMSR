using System.IO;

namespace SMSR.App.Services;

internal static class CodexRoleExecutable
{
    internal static string? Find()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenAI", "Codex", "bin");
        return Directory.Exists(root) ? Directory.EnumerateDirectories(root)
            .Where(folder => !new DirectoryInfo(folder).Attributes.HasFlag(FileAttributes.ReparsePoint))
            .Select(folder => Path.Combine(folder, "codex.exe")).Where(File.Exists)
            .Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() : null;
    }
}
