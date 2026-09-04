using System.Text.Json.Serialization;

namespace SMSR.App.Services;

internal sealed record UpdateCheckResult(bool Available, string Message,
    Version? Version = null, string? InstallerPath = null);

internal sealed record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("draft")] bool Draft,
    [property: JsonPropertyName("prerelease")] bool Prerelease,
    [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset> Assets);

internal sealed record GitHubAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("browser_download_url")] string DownloadUrl);
