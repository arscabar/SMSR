using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace SMSR.App.Mvp;

internal static partial class OAuthSelfCheck
{
    public static async Task<string> RunAsync(string address)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var resource = $"{address}/mcp";
        var metadata = await client.GetStringAsync($"{address}/.well-known/oauth-authorization-server");
        if (!metadata.Contains("registration_endpoint") || !metadata.Contains("S256")) Fail("OAuth 메타데이터");

        var registeredRedirect = "http://127.0.0.1/callback/smsr-test";
        using var registration = await client.PostAsync($"{address}/oauth/register", Json(new
        {
            redirect_uris = new[] { registeredRedirect }, client_name = "SMSR self-check",
            token_endpoint_auth_method = "none", grant_types = new[] { "authorization_code", "refresh_token" },
            response_types = new[] { "code" }
        }));
        var registrationJson = await registration.Content.ReadAsStringAsync();
        var clientId = JsonDocument.Parse(registrationJson).RootElement.GetProperty("client_id").GetString()!;
        var redirect = "http://127.0.0.1:54321/callback/smsr-test";
        var verifier = new string('v', 64);
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var authorize = QueryHelpers.AddQueryString($"{address}/oauth/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code", ["client_id"] = clientId, ["redirect_uri"] = redirect,
            ["state"] = "smsr-state", ["code_challenge"] = challenge, ["code_challenge_method"] = "S256",
            ["scope"] = OAuthUris.Scope, ["resource"] = resource
        });
        using var consentResponse = await client.GetAsync(authorize);
        var consent = await consentResponse.Content.ReadAsStringAsync();
        var policy = consentResponse.Headers.GetValues("Content-Security-Policy").Single();
        if (!policy.Contains("http://127.0.0.1:54321", StringComparison.Ordinal)) Fail("OAuth callback CSP");
        var requestId = RequestIdRegex().Match(consent).Groups[1].Value;
        using var approval = await client.PostAsync($"{address}/oauth/authorize", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["request_id"] = requestId, ["decision"] = "approve"
        }));
        var callback = approval.Headers.Location ?? throw new InvalidOperationException("OAuth callback이 없습니다.");
        var code = QueryHelpers.ParseQuery(callback.Query)["code"].ToString();
        using var token = await client.PostAsync($"{address}/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["client_id"] = clientId, ["code"] = code,
            ["redirect_uri"] = redirect, ["code_verifier"] = verifier, ["resource"] = resource
        }));
        var tokenJson = await token.Content.ReadAsStringAsync();
        var tokenRoot = JsonDocument.Parse(tokenJson).RootElement;
        var access = tokenRoot.GetProperty("access_token").GetString()!;
        var refresh = tokenRoot.GetProperty("refresh_token").GetString()!;
        using var refreshed = await client.PostAsync($"{address}/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["client_id"] = clientId,
            ["refresh_token"] = refresh, ["resource"] = resource
        }));
        var refreshedJson = await refreshed.Content.ReadAsStringAsync();
        if (refreshed.IsSuccessStatusCode)
            access = JsonDocument.Parse(refreshedJson).RootElement.GetProperty("access_token").GetString()!;
        using var initialize = new HttpRequestMessage(HttpMethod.Post, resource) { Content = Json(new
        {
            jsonrpc = "2.0", id = 1, method = "initialize", @params = new
            {
                protocolVersion = "2025-06-18", capabilities = new { }, clientInfo = new { name = "oauth-self-check", version = "1.0" }
            }
        }) };
        initialize.Headers.Authorization = new("Bearer", access);
        initialize.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using var initialized = await client.SendAsync(initialize);
        using var toolsRequest = new HttpRequestMessage(HttpMethod.Post, resource) { Content = Json(new
        {
            jsonrpc = "2.0", id = 2, method = "tools/list", @params = new { }
        }) };
        toolsRequest.Headers.Authorization = new("Bearer", access);
        toolsRequest.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using var toolsResponse = await client.SendAsync(toolsRequest);
        var toolsJson = await toolsResponse.Content.ReadAsStringAsync();
        string[] expectedTools = ["save_plan", "get_plan", "list_workflows", "record_event",
            "record_heartbeat", "get_state", "generate_summary", "save_summary", "export_workflow",
            "record_daily_activity", "get_daily_summary_request", "save_daily_summary_result"];
        if (!registration.IsSuccessStatusCode || approval.StatusCode != HttpStatusCode.Redirect
            || !token.IsSuccessStatusCode || !initialized.IsSuccessStatusCode
            || !refreshed.IsSuccessStatusCode || !refreshedJson.Contains("refresh_token", StringComparison.Ordinal)
            || !toolsResponse.IsSuccessStatusCode || expectedTools.Any(tool => !toolsJson.Contains($"\"{tool}\"")))
            Fail("OAuth 전체 흐름과 MCP 도구 목록");
        return access;
    }

    private static StringContent Json(object value) => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static void Fail(string step) => throw new InvalidOperationException($"{step} 검증이 실패했습니다.");

    [GeneratedRegex("name=\"request_id\" value=\"([^\"]+)\"")]
    private static partial Regex RequestIdRegex();
}
