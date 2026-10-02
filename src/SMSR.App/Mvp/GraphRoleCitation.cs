namespace SMSR.App.Mvp;

internal static class GraphRoleCitation
{
    internal static int? Line(GraphRoleEvidence evidence, string quote)
    {
        if (string.IsNullOrEmpty(quote)) return null;
        var offset = evidence.Quote.IndexOf(quote, StringComparison.Ordinal);
        return offset < 0 ? null : evidence.Line + evidence.Quote[..offset].Count(c => c == '\n');
    }
}
