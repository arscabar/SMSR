using System.Net;

namespace SMSR.App.Mvp;

internal static class OAuthConsentPage
{
    public static string Render(string requestId, string clientName)
    {
        var id = WebUtility.HtmlEncode(requestId);
        var name = WebUtility.HtmlEncode(clientName);
        return $$"""
            <!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
            <title>SMSR MCP 인증</title><style>
            body{font-family:Segoe UI,sans-serif;background:#f4f5fb;color:#202338;margin:0;display:grid;place-items:center;min-height:100vh}
            main{background:#fff;border:1px solid #d9ddec;border-radius:14px;padding:28px;max-width:460px;box-shadow:0 10px 30px #252a5520}
            h1{font-size:22px;margin:0 0 12px}p{line-height:1.55;color:#62677d}button{border:0;border-radius:8px;padding:11px 18px;font-weight:600;cursor:pointer}
            .allow{background:#5957e8;color:#fff}.deny{background:#eceef6;color:#303449;margin-left:8px}</style></head>
            <body><main><h1>SMSR MCP 연결 승인</h1><p><strong>{{name}}</strong>이(가) 이 Windows 사용자 계정의 SMSR 계획과 작업 상태를 읽고 기록하도록 허용합니다.</p>
            <p>서버는 127.0.0.1에만 열리며 발급 토큰은 Windows DPAPI로 보호됩니다.</p>
            <form method="post" action="/oauth/authorize"><input type="hidden" name="request_id" value="{{id}}">
            <button class="allow" name="decision" value="approve">연결 승인</button><button class="deny" name="decision" value="deny">거부</button></form></main></body></html>
            """;
    }
}
