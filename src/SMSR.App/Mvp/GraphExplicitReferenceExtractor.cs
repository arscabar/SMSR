using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SMSR.App.Mvp;

internal sealed record GraphExplicitReference(string RawPath, int Line);

internal static class GraphExplicitReferenceExtractor
{
    public static IReadOnlyList<GraphExplicitReference> Extract(string path, byte[] bytes, string? markdown)
    {
        if (markdown is not null) return GraphMarkdown.ExplicitLinks(markdown);
        if (Path.GetExtension(path).ToLowerInvariant() is not (".csproj" or ".slnx")) return [];
        try
        {
            using var text = new StringReader(Encoding.UTF8.GetString(bytes));
            using var reader = XmlReader.Create(text, new XmlReaderSettings
                { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var xml = XDocument.Load(reader, LoadOptions.SetLineInfo);
            return xml.Descendants().Where(element => element.Name.LocalName is "ProjectReference" or "Project")
                .Select(element => new { Element = element, Path = element.Attribute("Include")?.Value ?? element.Attribute("Path")?.Value })
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .Select(item => new GraphExplicitReference(item.Path!, ((IXmlLineInfo)item.Element).LineNumber))
                .ToArray();
        }
        catch (XmlException) { return []; }
    }
}
