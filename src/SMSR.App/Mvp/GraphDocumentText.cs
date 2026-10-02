namespace SMSR.App.Mvp;

internal static class GraphDocumentText
{
    internal static GraphDocument Convert(string path, (string Text, string Hash, int Revision) source)
    {
        var excluded = GraphDocumentRedaction.Excluded(source.Text);
        var lines = source.Text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length > 5000 || source.Text.Length > 1_000_000 || lines.Any(l => l.Length > 32000))
            throw new InvalidOperationException("문서 본문은 5천 줄·백만 자·줄당 3만2천 자 이하여야 합니다.");
        return new(source.Hash, "text-v2", excluded ? [] : lines.Select((text, i) =>
            new GraphDocumentBlock($"lines:{i+1}-{i+1}", text, i+1)).Where(b => !string.IsNullOrWhiteSpace(b.Text)).ToArray(),
            excluded ? "SECRET_EXCLUDED" : "TEXT_AVAILABLE", excluded ? 1 : 0, source.Revision);
    }
}
