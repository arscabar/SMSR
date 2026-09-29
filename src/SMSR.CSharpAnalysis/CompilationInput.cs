using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SMSR.CSharpAnalysis;

internal static class CompilationInput
{
    internal static CSharpCompilation Create(AnalysisInput input, int maxFiles = 500)
    {
        if (input.Files is null || input.Files.Length < 1 || input.Files.Length > maxFiles ||
            input.LanguageVersion is null || !input.LanguageVersion.StartsWith("CSharp", StringComparison.Ordinal) ||
            !Enum.TryParse<LanguageVersion>(input.LanguageVersion, out var version) ||
            !Enum.IsDefined(version) || version is LanguageVersion.Default or LanguageVersion.Latest or LanguageVersion.Preview)
            throw new ArgumentException("Explicit supported language version required");
        var defines = input.Defines ?? [];
        if (defines.Length > 128 || defines.Any(d => d is null || d.Length > 128 || !SyntaxFacts.IsValidIdentifier(d)))
            throw new ArgumentException("Invalid conditional symbols");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trees = new List<SyntaxTree>();
        long bytes = 0;
        foreach (var file in input.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Path) || file.Path.Length > 1024 ||
                file.Path.Contains('\\') || file.Path.Contains(':') ||
                file.Path.Any(char.IsControl) || !file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                file.Path.Split('/').Any(p => p is "" or "." or "..") ||
                !paths.Add(file.Path.Normalize()) || file.Text is null)
                throw new ArgumentException("Invalid or duplicate source path");
            var size = Encoding.UTF8.GetByteCount(file.Text);
            bytes += size;
            if (size > 2_000_000 || bytes > 16 * 1024 * 1024) throw new ArgumentException("Source limit exceeded");
            trees.Add(CSharpSyntaxTree.ParseText(file.Text, new CSharpParseOptions(version,
                preprocessorSymbols: defines), file.Path, Encoding.UTF8));
        }
        // Trusted framework metadata only; never load input-provided assemblies or run analyzers/generators.
        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)! + Path.DirectorySeparatorChar;
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Framework metadata unavailable");
        var references = trusted.Split(Path.PathSeparator)
            .Where(p => p.StartsWith(runtime, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal).Select(p => MetadataReference.CreateFromFile(p));
        return CSharpCompilation.Create("SMSR.InputBundle", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }
}
