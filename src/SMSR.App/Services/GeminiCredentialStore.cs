using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Services;

internal sealed class GeminiCredentialStore(string dataPath)
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SMSR-gemini-api-key-v1");
    private readonly string _path = Path.Combine(dataPath, "gemini-api-key.bin");

    public bool Exists => File.Exists(_path);

    public void Save(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("Gemini API 키가 비어 있습니다.");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey.Trim()), Entropy,
            DataProtectionScope.CurrentUser);
        var temporary = _path + ".tmp";
        File.WriteAllBytes(temporary, encrypted);
        File.Move(temporary, _path, true);
    }

    public string? Read()
    {
        if (!File.Exists(_path)) return null;
        var clear = ProtectedData.Unprotect(File.ReadAllBytes(_path), Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(clear);
    }

    public void Delete()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
