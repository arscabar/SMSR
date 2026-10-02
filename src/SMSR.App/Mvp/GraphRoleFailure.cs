namespace SMSR.App.Mvp;

internal static class GraphRoleFailure
{
    internal static string Code(string diagnostic)
    {
        // Classify in memory only; never persist provider messages, raw output or source text.
        foreach (var (needle, code) in new[] {
            ("invalid_request", "REQUEST"), ("Unauthorized", "AUTH"), ("401", "AUTH"),
            ("not logged", "AUTH"), ("rate limit", "LIMIT"), ("quota", "LIMIT"),
            ("output schema", "SCHEMA"), ("Invalid schema", "SCHEMA"),
            ("unexpected argument", "FLAGS"), ("sandbox", "SANDBOX"),
            ("config", "CONFIG"), ("error sending request", "NETWORK"),
            ("UTF-8", "ENCODING"), ("utf8", "ENCODING") })
            if (diagnostic.Contains(needle, StringComparison.OrdinalIgnoreCase)) return code;
        return "UNKNOWN";
    }
}
