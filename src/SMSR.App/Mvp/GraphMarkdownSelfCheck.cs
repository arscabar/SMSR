using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphMarkdownSelfCheck
{
    public static void Run()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "markdown-oracle"));
        var paths = new HashSet<string>(["docs/a.md", "src/Widget.cs", "src/a(b).cs", "src/file name.cs", "src/한글.cs"], StringComparer.OrdinalIgnoreCase);
        var cases = new (string Markdown, string? Target)[]
        {
            ("[x](../src/Widget.cs)", "src/Widget.cs"),
            ("[x](../src/widget.cs)", "src/Widget.cs"),
            ("[x](../src/a(b).cs)", "src/a(b).cs"),
            ("[x](<../src/file name.cs>)", "src/file name.cs"),
            ("[x](../src/file%20name.cs \"title\")", "src/file name.cs"),
            ("[x][ref]\n\n[ref]: ../src/Widget.cs", "src/Widget.cs"),
            ("[ref][]\n\n[ref]: ../src/Widget.cs", "src/Widget.cs"),
            ("[ref]\n\n[ref]: ../src/Widget.cs", "src/Widget.cs"),
            ("[x](/src/Widget.cs#member)", "src/Widget.cs"),
            ("[x](../src/%ED%95%9C%EA%B8%80.cs)", "src/한글.cs"),
            ("`../src/Widget.cs::Widget`", "src/Widget.cs"),
            ("````\n```\n[x](../src/Widget.cs)\n````", null),
            ("~~~\n```\n[x](../src/Widget.cs)\n~~~", null),
            ("    [x](../src/Widget.cs)", null),
            ("`[x](../src/Widget.cs)`", null),
            ("<!-- [x](../src/Widget.cs) -->", null),
            ("[x](https://example.com/src/Widget.cs)", null),
            ("[x](../../outside.cs)", null),
            ("[x](missing/Widget.cs)", null),
            ("\\[x](../src/Widget.cs)", null)
        };
        foreach (var test in cases)
        {
            var result = GraphMarkdown.Extract(root, new(new("docs/a.md", "hash", "document"), test.Markdown, [], []), paths);
            var links = result.Edges.Where(edge => edge.Relation != "CONTAINS").ToArray();
            if (test.Target is null ? links.Length != 0 : links.Length != 1 || links[0].TargetId != "file:" + test.Target)
                throw new InvalidOperationException("Markdown 정답집 실패: " + test.Markdown);
        }
        var headings = GraphMarkdownSyntax.Parse("제목\n====\n\n# 두 번째\n\n[x](../src/Widget.cs)");
        if (headings.Count(x => x.Kind == "heading") != 2 || headings.Last().Line != 6)
            throw new InvalidOperationException("제목·근거 줄 추출 실패");
        var frontMatter = GraphMarkdownSyntax.Parse("---\nname: smsr-tracking\ndescription: A long skill description\n---\n\n# Actual heading\n");
        if (frontMatter.Count != 1 || frontMatter[0].Kind != "heading" || frontMatter[0].Text != "Actual heading" || frontMatter[0].Line != 6)
            throw new InvalidOperationException("YAML 머리말이 제목으로 색인됨");
    }
}
