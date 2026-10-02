using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;

internal static class GraphVaultNote
{
    internal static string Text(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("[", "\\[").Replace("]", "\\]").Replace("\r", " ");
    internal static string Link(GraphNode node) => "[[" + Path.GetFileNameWithoutExtension(GraphVaultPaths.Name(node)) + "|" + Text(node.Label).Replace("|", " ").Replace("\n", " ") + "]]";
    internal static string Render(string project, GraphNode node, GraphRoleStatus? role, string[] relations, string? address = null)
    {
        address ??= $"http://127.0.0.1:{LocalServer.Port}";
        if (!Uri.TryCreate(address, UriKind.Absolute, out var server) || !server.IsLoopback || server.Scheme != "http"
            || server.UserInfo.Length != 0 || server.Query.Length != 0 || server.Fragment.Length != 0 || server.AbsolutePath != "/")
            throw new ArgumentException("노트 원문 링크는 로컬 SMSR 서버만 허용합니다.");
        var state = role?.Status ?? "NOT_APPLICABLE";
        var tag = node.Kind is "code" or "document" or "image" or "video" or "audio" ? node.Kind : "other";
        var body = $"---\nsmsr_owned: true\nsource_file: {JsonSerializer.Serialize(node.SourcePath)}\nsource_hash: {JsonSerializer.Serialize(node.Hash)}\nrole_status: {state}\ntags:\n  - smsr/{tag}\n---\n\n# {Text(node.Label)}\n\n";
        body += $"원문: `{Text(node.SourcePath).Replace("`", "").Replace("\n", " ")}`:{node.Line}\n\n";
        var route = node.Kind is "image" or "audio" or "video" ? "/graph/media"
            : new[] { ".pdf", ".docx", ".xlsx", ".pptx" }.Contains(Path.GetExtension(node.SourcePath).ToLowerInvariant()) ? "/graph/document" : "/graph/source";
        body += $"[SMSR 원문·근거 열기]({server.GetLeftPart(UriPartial.Authority)}{route}?projectId={Uri.EscapeDataString(project)}&path={Uri.EscapeDataString(node.SourcePath)}&line={node.Line})\n\n## 역할과 동작\n\n";
        if (role?.Report is { } report)
            body += GraphVaultRoleNote.Render(project, server.GetLeftPart(UriPartial.Authority), report);
        else body += (state == "STALE" ? "원문 또는 관계가 변경되어 설명 갱신이 필요합니다.\n\n"
            : role is null ? "이 항목은 코드 역할 설명 대상이 아닙니다. 저장된 관계와 원문을 확인하세요.\n\n"
            : "아직 역할 설명을 생성하지 않았습니다. SMSR에서 설명 생성을 요청하세요.\n\n");
        body += "## 관련 파일과 근거\n\n" + (relations.Length == 0 ? "확인된 파일 간 관계가 없습니다.\n" : string.Join("\n", relations) + "\n");
        return body + "\n정적 관계는 실제 실행 순서가 아닙니다. 추론·미해소 관계는 확정 호출로 해석하지 마세요.\n";
    }
}
