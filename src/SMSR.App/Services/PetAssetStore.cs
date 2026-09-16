using System.IO;

namespace SMSR.App.Services;

internal static class PetAssetStore
{
    private const long MaximumBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = [".png", ".jpg", ".jpeg", ".bmp"];

    public static string Register(string sourcePath, string dataPath)
    {
        var source = new FileInfo(sourcePath);
        var extension = source.Extension.ToLowerInvariant();
        if (!source.Exists || source.Length is 0 or > MaximumBytes || !Extensions.Contains(extension))
            throw new InvalidOperationException("펫 이미지는 10MB 이하 PNG, JPG, BMP 파일만 등록할 수 있습니다.");
        using (var stream = source.OpenRead())
        {
            Span<byte> header = stackalloc byte[8];
            if (stream.Read(header) < 3 || !HasImageSignature(extension, header))
                throw new InvalidOperationException("이미지 형식과 파일 내용이 일치하지 않습니다.");
        }
        var directory = Path.Combine(dataPath, "Pet");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, "pet" + extension);
        File.Copy(source.FullName, destination, true);
        return destination;
    }

    private static bool HasImageSignature(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".png" => header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
        ".jpg" or ".jpeg" => header[..3].SequenceEqual(new byte[] { 0xff, 0xd8, 0xff }),
        ".bmp" => header[..2].SequenceEqual(new byte[] { 0x42, 0x4d }),
        _ => false
    };
}
