using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SMSR.App.Services;

internal sealed partial class AppUpdateService
{
    private static readonly Uri LatestRelease = new("https://api.github.com/repos/arscabar/SMSR/releases/latest");
    private readonly string _directory;
    private readonly HttpClient _client;
    private readonly Uri _releaseUri;

    public AppUpdateService(string dataPath) : this(dataPath, new HttpClient(), LatestRelease) { }

    internal AppUpdateService(string dataPath, HttpClient client, Uri releaseUri)
    {
        _directory = Path.Combine(dataPath, "updates");
        _client = client;
        _releaseUri = releaseUri;
        if (!_client.DefaultRequestHeaders.UserAgent.Any())
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("SMSR-Updater/1.0");
    }

    public static Version CurrentVersion
        => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);

    public async Task<UpdateCheckResult> CheckAndDownloadAsync(Version current,
        CancellationToken cancellationToken = default)
    {
        var release = await _client.GetFromJsonAsync<GitHubRelease>(_releaseUri, cancellationToken)
            ?? throw new InvalidDataException("최신 릴리스 정보를 읽지 못했습니다.");
        if (release.Draft || release.Prerelease)
            return new(false, "설치 가능한 정식 릴리스가 없습니다.");
        if (!TryVersion(release.TagName, out var latest))
            return new(false, "설치 가능한 정식 릴리스가 없습니다.");
        if (latest <= current) return new(false, $"현재 버전 {current.ToString(3)}이 최신입니다.", latest);

        var installer = release.Assets.FirstOrDefault(asset =>
            asset.Name.StartsWith("SMSR-Setup-", StringComparison.OrdinalIgnoreCase)
            && asset.Name.EndsWith("-win-x64.exe", StringComparison.OrdinalIgnoreCase));
        var checksum = installer is null ? null : release.Assets.FirstOrDefault(asset =>
            asset.Name.Equals(installer.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
        if (installer is null || checksum is null)
            throw new InvalidDataException("설치 파일 또는 SHA-256 파일이 없는 릴리스입니다.");

        var checksumText = await _client.GetStringAsync(checksum.DownloadUrl, cancellationToken);
        var match = HashPattern().Match(checksumText);
        if (!match.Success) throw new InvalidDataException("SHA-256 파일 형식이 올바르지 않습니다.");
        var versionDirectory = Path.Combine(_directory, "v" + latest.ToString(3));
        Directory.CreateDirectory(versionDirectory);
        var path = Path.Combine(versionDirectory, Path.GetFileName(installer.Name));
        if (!File.Exists(path) || !HashEquals(path, match.Value))
            await DownloadAsync(installer.DownloadUrl, path, cancellationToken);
        if (!HashEquals(path, match.Value))
        {
            File.Delete(path);
            throw new InvalidDataException("다운로드한 설치 파일의 SHA-256이 일치하지 않습니다.");
        }
        return new(true, $"새 버전 {latest.ToString(3)}을 확인했습니다.", latest, path);
    }

    public static ProcessStartInfo CreateInstallerStartInfo(string path) => new(path)
    {
        UseShellExecute = true,
        Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /SMSRAUTORESTART=1",
        WorkingDirectory = Path.GetDirectoryName(path)!
    };

    public bool StartInstaller(string path)
    {
        try { return Process.Start(CreateInstallerStartInfo(path)) is not null; }
        catch { return false; }
    }

    private async Task DownloadAsync(string url, string path, CancellationToken cancellationToken)
    {
        var temporary = path + ".download";
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using (var output = File.Create(temporary))
            await input.CopyToAsync(output, cancellationToken);
        File.Move(temporary, path, true);
    }

    private static bool HashEquals(string path, string expected)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryVersion(string tag, out Version version)
        => Version.TryParse(tag.Trim().TrimStart('v', 'V'), out version!);

    [GeneratedRegex("[A-Fa-f0-9]{64}")]
    private static partial Regex HashPattern();
}
