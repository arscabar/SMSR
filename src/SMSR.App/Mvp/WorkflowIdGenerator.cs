using System.Text;

namespace SMSR.App.Mvp;

internal static class WorkflowIdGenerator
{
    public static string Create(string projectId, string? taskTitle = null, DateTimeOffset? now = null)
    {
        var timestamp = (now ?? DateTimeOffset.Now).ToString("yyyyMMdd-HHmmssfff");
        return $"{timestamp}__{Slug(projectId, 32, "project")}__{Slug(taskTitle, 64, "workflow")}";
    }

    private static string Slug(string? value, int maxLength, string fallback)
    {
        var result = new StringBuilder();
        foreach (var character in value?.Trim() ?? string.Empty)
        {
            if (char.IsLetterOrDigit(character)) result.Append(character);
            else if (result.Length > 0 && result[^1] != '-') result.Append('-');
            if (result.Length == maxLength) break;
        }
        return result.ToString().Trim('-') is { Length: > 0 } slug ? slug : fallback;
    }
}
