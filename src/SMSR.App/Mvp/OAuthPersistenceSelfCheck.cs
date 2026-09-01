using System.IO;

namespace SMSR.App.Mvp;

internal static class OAuthPersistenceSelfCheck
{
    public static void Run(string directory)
    {
        var path = Path.Combine(directory, "oauth-persistence-test.bin");
        var audience = "http://127.0.0.1:49783/mcp";
        var first = new LocalOAuthStore(path);
        var client = first.Register(["http://127.0.0.1/callback/test"], "persistence-self-check");
        var issued = first.Issue(client.ClientId, audience, OAuthUris.Scope);

        var afterRestart = new LocalOAuthStore(path);
        if (!afterRestart.HasActiveAuthorization)
            throw new InvalidOperationException("재시작 후 OAuth 승인 복원이 실패했습니다.");
        var rotated = afterRestart.RotateRefresh(issued.RefreshToken, client.ClientId, audience)
            ?? throw new InvalidOperationException("재시작 후 OAuth 갱신이 실패했습니다.");
        if (afterRestart.RotateRefresh(issued.RefreshToken, client.ClientId, audience) is not null)
            throw new InvalidOperationException("사용한 OAuth 갱신 토큰이 재사용되었습니다.");

        var afterSecondRestart = new LocalOAuthStore(path);
        if (afterSecondRestart.RotateRefresh(rotated.RefreshToken, client.ClientId, audience) is null)
            throw new InvalidOperationException("토큰 회전 후 OAuth 갱신 상태 복원이 실패했습니다.");
    }
}
