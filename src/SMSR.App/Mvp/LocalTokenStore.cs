using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMSR.App.Mvp;

public sealed class LocalTokenStore(string tokenPath)
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SMSR-local-token-v1");

    public string GetOrCreate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(tokenPath)!);
        if (File.Exists(tokenPath))
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(tokenPath), Entropy, DataProtectionScope.CurrentUser));

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        File.WriteAllBytes(tokenPath, ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser));
        return token;
    }
}
