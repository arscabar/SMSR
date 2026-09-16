using System.IO;

namespace SMSR.App.Services;

internal static class PetMediaSelector
{
    public static string? Select(AppSettings settings, int progress)
        => Rules(settings).FirstOrDefault(rule => progress >= rule.StartProgress && progress <= rule.EndProgress)?.MediaPath;

    public static IReadOnlyList<PetMediaRule> Rules(AppSettings settings)
    {
        var rules = settings.PetMediaRules?.Where(rule => File.Exists(rule.MediaPath))
            .OrderBy(rule => rule.StartProgress).ToArray() ?? [];
        if (rules.Length > 0) return rules;
        return File.Exists(settings.PetImagePath) ? [new(0, 100, settings.PetImagePath)] : [];
    }

    public static IReadOnlyList<PetMediaRule> Distribute(IEnumerable<string> mediaPaths)
    {
        var paths = mediaPaths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return paths.Select((path, index) => new PetMediaRule(
            index * 101 / paths.Length,
            index == paths.Length - 1 ? 100 : ((index + 1) * 101 / paths.Length) - 1,
            path)).ToArray();
    }

    public static string? Validate(IReadOnlyList<PetMediaRule> rules)
    {
        if (rules.Count == 0) return "미디어를 하나 이상 등록하세요.";
        var sorted = rules.OrderBy(rule => rule.StartProgress).ToArray();
        if (sorted.Any(rule => rule.StartProgress < 0 || rule.EndProgress > 100 || rule.StartProgress > rule.EndProgress))
            return "진행률 구간은 0~100 사이이며 시작값이 종료값보다 클 수 없습니다.";
        if (sorted[0].StartProgress != 0 || sorted[^1].EndProgress != 100
            || sorted.Zip(sorted.Skip(1)).Any(pair => pair.First.EndProgress + 1 != pair.Second.StartProgress))
            return "진행률 구간은 겹치거나 비지 않게 0%부터 100%까지 이어져야 합니다.";
        if (sorted.Any(rule => !File.Exists(rule.MediaPath))) return "등록한 미디어 파일을 찾을 수 없습니다.";
        return null;
    }
}
