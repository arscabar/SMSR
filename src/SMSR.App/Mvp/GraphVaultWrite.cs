using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace SMSR.App.Mvp;

internal static class GraphVaultWrite
{
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static int Note(string area, string name, string text, GraphVaultEntry? owned)
    {
        var path = GraphVaultPaths.Inside(area, name); var bytes = new UTF8Encoding(false).GetBytes(text);
        if (bytes.Length > 2_000_000) throw new InvalidOperationException("생성 노트 크기 제한을 초과했습니다.");
        if (!File.Exists(path))
        {
            using var fresh = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            fresh.Write(bytes); fresh.Flush(true); return 1;
        }
        if (owned is null) return -1;
        using var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        if (file.Length > 2_000_000) return -1;
        var old = new byte[checked((int)file.Length)]; file.ReadExactly(old);
        if (Hash(old) != owned.Hash) return -1;
        if (old.AsSpan().SequenceEqual(bytes)) return 0;
        var backups = GraphVaultPaths.Validate(Path.Combine(area, ".smsr-backups")); Directory.CreateDirectory(backups);
        var backup = GraphVaultPaths.Validate(Path.Combine(backups, owned.Hash + ".bak"));
        if (!File.Exists(backup))
        { using var saved = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None); saved.Write(old); saved.Flush(true); }
        try { file.Position = 0; file.Write(bytes); file.SetLength(bytes.Length); file.Flush(true); }
        catch { file.Position = 0; file.Write(old); file.SetLength(old.Length); file.Flush(true); throw; }
        return 1;
    }
}
