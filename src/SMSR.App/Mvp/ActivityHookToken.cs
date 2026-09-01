using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

internal sealed class ActivityHookToken(string dataPath)
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SMSR-activity-hook-v1");
    private readonly string _path = Path.Combine(dataPath, "activity-hook-token.bin");
    private string? _value;

    public string Value => _value ??= LoadOrCreate();

    public bool Validate(string candidate)
    {
        if (string.IsNullOrEmpty(candidate)) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Value), Encoding.UTF8.GetBytes(candidate));
    }

    private string LoadOrCreate()
    {
        if (File.Exists(_path)) return Unprotect(File.ReadAllBytes(_path));
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        try
        {
            using var stream = new FileStream(_path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser));
            return value;
        }
        catch (IOException) { return Unprotect(File.ReadAllBytes(_path)); }
    }

    private static string Unprotect(byte[] bytes)
        => Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser));
}
