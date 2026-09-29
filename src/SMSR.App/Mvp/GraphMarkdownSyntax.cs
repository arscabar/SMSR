using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace SMSR.App.Mvp;

internal sealed record GraphMarkdownItem(int Position, int Line, string Kind, string Text);

internal static class GraphMarkdownSyntax
{
    // CommonMark owns fences, escaping, nested destinations and reference definitions.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();

    public static IReadOnlyList<GraphMarkdownItem> Parse(string markdown)
    {
        var document = Markdown.Parse(WithoutFrontMatter(markdown), Pipeline);
        var items = new List<GraphMarkdownItem>();
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var label = heading.Inline is null ? "" : string.Concat(heading.Inline.Descendants().Select(inline => inline switch
            {
                LiteralInline text => text.Content.ToString(), CodeInline code => code.Content,
                HtmlInline html => html.Tag, LineBreakInline => " ", _ => ""
            }));
            items.Add(new(heading.Span.Start, heading.Line + 1, "heading", label));
        }
        foreach (var link in document.Descendants<LinkInline>().Where(link => link.Url is not null))
            items.Add(new(link.Span.Start, link.Line + 1, "link", link.Url!));
        foreach (var code in document.Descendants<CodeInline>())
        {
            var separator = code.Content.IndexOf("::", StringComparison.Ordinal);
            if (separator > 0 && separator + 2 < code.Content.Length && code.Content.Length <= 512)
                items.Add(new(code.Span.Start, code.Line + 1, "citation", code.Content[..separator]));
        }
        return items.OrderBy(item => item.Position).ThenBy(item => item.Kind == "heading" ? 0 : 1).ToArray();
    }

    private static string WithoutFrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---\n", StringComparison.Ordinal) && !markdown.StartsWith("---\r\n", StringComparison.Ordinal))
            return markdown;
        var start = markdown.IndexOf('\n') + 1;
        for (var lineStart = start; lineStart < markdown.Length;)
        {
            var lineEnd = markdown.IndexOf('\n', lineStart);
            if (lineEnd < 0) lineEnd = markdown.Length;
            if (markdown[lineStart..lineEnd].TrimEnd('\r') is "---" or "...")
            {
                var chars = markdown.ToCharArray();
                for (var i = 0; i < lineEnd; i++) if (chars[i] is not ('\r' or '\n')) chars[i] = ' ';
                return new string(chars);
            }
            lineStart = lineEnd + 1;
        }
        return markdown;
    }
}
