using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphQualitySources
{
    internal static void Copy(string root, string[] selected)
    {
        const string mvp = "src/SMSR.App/Mvp", web = "src/SMSR.App/WebAssets", runtime = "src/SMSR.App/GraphRuntime";
        var helpers = new[] { "GraphVault*.cs", "GraphRoleBatch*.cs", "GraphRoleJob*.cs" }
            .SelectMany(pattern => Directory.GetFiles(mvp, pattern))
            .Where(path => !Path.GetFileName(path).Contains("SelfCheck", StringComparison.Ordinal));
        var paths = selected.Concat(helpers).Concat(new[] { web + "/graph-explorer-trace-result.js",
            web + "/graph-explorer-labels.js", runtime + "/cache_protection.py", runtime + "/cache_paths.py" });
        foreach (var path in paths.Select(p => p.Replace('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var target = Path.Combine(root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(Environment.CurrentDirectory, path), target);
        }
    }
}
