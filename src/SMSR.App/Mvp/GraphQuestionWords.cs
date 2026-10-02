using System.Text.RegularExpressions;
namespace SMSR.App.Mvp;
internal static class GraphQuestionWords
{
    internal static string[] Split(string text) => Regex.Matches(Regex.Replace(text,
        "([a-z0-9])([A-Z])", "$1 $2", RegexOptions.None, TimeSpan.FromMilliseconds(200)),
        @"[\p{L}\p{N}]{2,64}", RegexOptions.None, TimeSpan.FromMilliseconds(200))
        .Select(m => m.Value.ToLowerInvariant()).Distinct().Take(40).ToArray();
    internal static Dictionary<string,int> Vocabulary(IReadOnlyList<GraphNode> nodes)
        => nodes.SelectMany(n => Split(n.Label + " " + n.SourcePath)).GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());
    internal static string[] Expand(string question, IReadOnlyDictionary<string,int> vocabulary)
    {
        var raw = Split(question).ToList();
        foreach (var (intent, aliases) in new[] { ("저장", new[]{"save","store","persist"}),
            ("인증",new[]{"auth","login","authentication"}), ("색인",new[]{"index","indexing"}),
            ("검색",new[]{"search","query"}), ("오류",new[]{"error","exception","failure"}) })
            if (question.Contains(intent, StringComparison.Ordinal)) raw.AddRange(aliases);
        return raw.Where(vocabulary.ContainsKey).Distinct().Take(12).ToArray();
    }
}
