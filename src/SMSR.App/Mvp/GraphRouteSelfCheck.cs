using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphRouteSelfCheck
{
    public static void Run()
    {
        var cases = new Dictionary<string, string> {
            ["cs"] = "// app.MapGet(\"/comment\", X);\n/* app.MapPost(\"/block\",X); */\nvar s = \"app.MapGet(\\\"/string\\\",X)\";\napp.MapGet(\"/one\",X); app.MapPost(\"/two\",Y);\napp.MapGet(\"/api_key=hidden\",X);",
            ["py"] = "# @app.get('/comment')\n\"\"\"@app.get('/docstring')\"\"\"\n@app.get('/one')\ndef one(): pass\n@router.post('/two')\ndef two(): pass",
            ["js"] = "// app.get('/comment',x);\nconst s = `app.get('/string',x)`;\napp.get(`/dynamic/${id}`,x);\napp.get('/one',x); app.post('/two',x);",
            ["ts"] = "/* app.get('/comment',x) */\napp.get('/one',x); app.post('/two',x);"
        };
        foreach (var (extension, source) in cases)
        {
            var routes = GraphRouteExtractor.Extract("routes." + extension, Encoding.UTF8.GetBytes(source));
            if (!routes.Select(r => r.Value).SequenceEqual(new[] { "/one", "/two" }))
                throw new Exception(extension + " 경로 지도 문자열·주석·동적·비밀 제외 실패");
            if (routes.Any(r => r.Line < 1)) throw new Exception("경로 지도 근거 줄 누락");
        }
        var auth = GraphRouteExtractor.Extract("auth.py", Encoding.UTF8.GetBytes("\"\"\"\"\"\"\n@app.post('/oauth/token')\ndef login(): pass"));
        if (auth.Single().Value != "/oauth/token" || auth.Single().Line != 2)
            throw new Exception("빈 문서문자열 또는 정상 인증 경로 누락");
    }
}
