namespace SMSR.App.Mvp;

internal static class GraphCodeMask
{
    // Lexical exclusion only; this does not resolve framework types or dynamic registrations.
    public static bool[] Create(string source, bool python)
    {
        var code = new bool[source.Length];
        for (var i = 0; i < source.Length;)
        {
            if (python && source[i] == '#' || !python && source.AsSpan(i).StartsWith("//"))
            {
                while (i < source.Length && source[i] != '\n') i++;
                continue;
            }
            if (!python && source.AsSpan(i).StartsWith("/*"))
            {
                var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? source.Length : end + 2;
                continue;
            }
            var quote = source[i];
            if (quote is '\'' or '"' or '`')
            {
                var run = 1;
                while (i + run < source.Length && source[i + run] == quote) run++;
                if (run >= 3 && quote != '`')
                {
                    if (python && run >= 6) { i += 6; continue; }
                    var end = source.IndexOf(new string(quote, run), i + run, StringComparison.Ordinal);
                    i = end < 0 ? source.Length : end + run;
                    continue;
                }
                var verbatim = !python && i > 0 && source[i - 1] == '@';
                i++;
                while (i < source.Length)
                {
                    if (!verbatim && source[i] == '\\') { i = Math.Min(source.Length, i + 2); continue; }
                    if (source[i++] != quote) continue;
                    if (verbatim && i < source.Length && source[i] == quote) { i++; continue; }
                    break;
                }
                continue;
            }
            code[i++] = true;
        }
        return code;
    }
}
