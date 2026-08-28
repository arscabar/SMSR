using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed class OAuthProtectedFile(string path)
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SMSR-local-oauth-v1");

    public OAuthPersistedState Load()
    {
        if (!File.Exists(path)) return new();
        var json = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<OAuthPersistedState>(json) ?? new();
    }

    public void Save(OAuthPersistedState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var encrypted = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(state), Entropy, DataProtectionScope.CurrentUser);
        var temporary = path + ".tmp";
        File.WriteAllBytes(temporary, encrypted);
        File.Move(temporary, path, true);
    }
}
