namespace SMSR.App.Mvp;
internal static class GraphVaultNoteSelfCheck
{
    internal static void Run()
    {
        var node = new GraphNode("file:a.py", "a.py", "code", "<script>a</script>", "a.py", 1, "hash");
        var shared = node with { NodeId = "symbol:namespace", SourcePath = "b.py", Details = new("namespace", "code", "graphify", "test") };
        var declaration = new GraphEdge(node.NodeId, shared.NodeId, "CONTAINS", "a.py", 1, "RESOLVED", "EXTRACTED");
        if (GraphVaultService.IsFileLink(declaration, node, shared)
            || GraphVaultService.IsFileLink(declaration with { Relation = "IMPORTS" }, node, shared))
            throw new Exception("Shared namespace incorrectly projected as file dependency");
        var method = shared with { Details = shared.Details with { EntityKind = "function" } };
        if (!GraphVaultService.IsFileLink(declaration with { Relation = "CALLS" }, node, method)
            || GraphVaultService.IsFileLink(declaration, node, method))
            throw new Exception("File relation meaning was not preserved");
        var note = GraphVaultNote.Render("sample", node, new("MISSING", null), ["- 들어옴 · [[other|Other]] · CALLS"]);
        if (!note.Contains("role_status: MISSING") || !note.Contains("[[other|Other]]") || note.Contains("<script>")
            || !note.Contains("아직 역할 설명")) throw new Exception("Vault note role/state/escaping failure");
        if (!GraphVaultNote.Render("sample", node, new("STALE", null), []).Contains("갱신이 필요"))
            throw new Exception("Vault stale note fabricated current role");
        if (GraphVaultPaths.Name(node) == GraphVaultPaths.Name(node with { NodeId = "file:b/a.py" }))
            throw new Exception("Vault filename collision");
        if (!GraphVaultNote.Render("sample", node with { SourcePath = "a.pdf", Kind = "document" }, null, []).Contains("/graph/document?"))
            throw new Exception("Vault document source route wrong");
        var evidence = new GraphRoleEvidence("e0", "a.py", 10, "header\nbody\nreturn 1", "hash");
        var claim = new GraphRoleClaim("ROLE", "ROLE: 원문 역할", "INFERRED", ["e0"], "return 1");
        var report = new GraphRoleReport(node.NodeId, "fingerprint", 1, "test", [claim], [evidence], DateTimeOffset.UtcNow);
        var cited = GraphVaultNote.Render("sample", node, new("CURRENT", report), []);
        if (!cited.Contains("`a.py`:12") || !cited.Contains("&line=12)") || !cited.Contains("<details><summary>설명 근거 펼치기")
            || !cited.Contains("### 핵심 역할") || cited.Contains("ROLE:") || GraphRoleCitation.Line(evidence, "absent") is not null)
            throw new Exception("Vault citation start line is not the actual quoted line");
    }
}
