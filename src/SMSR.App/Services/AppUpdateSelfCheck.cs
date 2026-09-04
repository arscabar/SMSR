using System.Net;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SMSR.App.Services;

internal static class AppUpdateSelfCheck
{
    public static async Task RunAsync(string root)
    {
        var directory = Path.Combine(root, "update-self-check");
        Directory.CreateDirectory(directory);
        var installer = Encoding.UTF8.GetBytes("verified SMSR installer");
        var hash = Convert.ToHexString(SHA256.HashData(installer));
        var releaseUri = new Uri("https://updates.test/latest");
        using var client = new HttpClient(new FakeHandler(request => Response(request, installer, hash)));
        var service = new AppUpdateService(directory, client, releaseUri);
        var result = await service.CheckAndDownloadAsync(new Version(1, 0));
        var start = AppUpdateService.CreateInstallerStartInfo(result.InstallerPath!);
        if (!result.Available || result.Version != new Version(9, 1, 0)
            || !File.ReadAllBytes(result.InstallerPath!).SequenceEqual(installer)
            || !start.Arguments.Contains("/SMSRAUTORESTART=1", StringComparison.Ordinal)
            || !start.Arguments.Contains("/VERYSILENT", StringComparison.Ordinal))
            throw new InvalidOperationException("자동 업데이트 다운로드·검증·설치 인수 검사가 실패했습니다.");

        using var corruptClient = new HttpClient(new FakeHandler(request => Response(request, installer, new string('0', 64))));
        var corrupt = new AppUpdateService(Path.Combine(directory, "corrupt"), corruptClient, releaseUri);
        try
        {
            await corrupt.CheckAndDownloadAsync(new Version(1, 0));
            throw new InvalidOperationException("잘못된 업데이트 체크섬이 허용됐습니다.");
        }
        catch (InvalidDataException) { }
    }

    public static async Task RunPublishedReleaseAsync(string root)
    {
        var result = await new AppUpdateService(root).CheckAndDownloadAsync(new Version(0, 0));
        if (!result.Available || result.InstallerPath is null || !File.Exists(result.InstallerPath))
            throw new InvalidOperationException("공개 릴리스 업데이트 검증이 실패했습니다.");
    }

    private static HttpResponseMessage Response(HttpRequestMessage request, byte[] installer, string hash)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("latest", StringComparison.Ordinal))
        {
            var release = new GitHubRelease("v9.1.0", false, false,
            [
                new("SMSR-Setup-9.1.0.0-win-x64.exe", "https://updates.test/installer"),
                new("SMSR-Setup-9.1.0.0-win-x64.exe.sha256", "https://updates.test/checksum")
            ]);
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(release), Encoding.UTF8, "application/json") };
        }
        return request.RequestUri.AbsolutePath.EndsWith("checksum", StringComparison.Ordinal)
            ? new(HttpStatusCode.OK) { Content = new StringContent(hash + "  SMSR-Setup-9.1.0.0-win-x64.exe") }
            : new(HttpStatusCode.OK) { Content = new ByteArrayContent(installer) };
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response(request));
    }
}
