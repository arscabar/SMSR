using System.IO;
using System.Text;

namespace SMSR.App.Services;

internal static partial class CodexMcpConfig
{
    public static string? Unregister(string path)
    {
        if (!File.Exists(path)) return null;
        var original = File.ReadAllText(path);
        var lines = NewlineRegex().Split(original);
        var start = Array.FindIndex(lines, line => SmsrHeaderRegex().IsMatch(line));
        if (start < 0) return null;

        var updated = string.Concat(lines[..start]) + string.Concat(lines[FindEnd(lines, start)..]);
        var temporary = path + ".smsr.tmp";
        File.WriteAllText(temporary, updated, new UTF8Encoding(false));
        var backup = path + ".smsr.bak";
        File.Replace(temporary, path, backup, true);
        return backup;
    }
}
