namespace SMSR.App.Mvp;
internal static class GraphMediaRegion
{
    internal static GraphDocumentBlock? Evidence(GraphDocument document,string location,string quote)
    {
        if(document.ExtractorVersion is not("media-v1" or "document-v1"))return null;
        if(location.StartsWith("page:",StringComparison.Ordinal))
        {
            var page=location.Split(':',3);
            if(page.Length!=3||!int.TryParse(page[1],out var number)||number is <1 or >50)return null;
            var prefix=$"page:{number}:";
            var subset=document with{Blocks=document.Blocks.Where(b=>b.Location.StartsWith(prefix,StringComparison.Ordinal)).Select(b=>b with{Location=b.Location[prefix.Length..]}).ToArray()};
            var block=Evidence(subset,page[2],quote);return block is null?null:block with{Location=location};
        }
        if(!location.StartsWith("image:",StringComparison.Ordinal))return null;
        var parts=location[6..].Split(',');
        if(parts.Length!=4||parts.Any(p=>!int.TryParse(p,out _)))return null;
        var values=parts.Select(int.Parse).ToArray();
        var full=document.Blocks.FirstOrDefault(b=>b.Location.StartsWith("image:0,0,",StringComparison.Ordinal));
        if(full is null||!full.Text.Contains(quote,StringComparison.Ordinal))return null;
        var size=full.Location[6..].Split(',').Select(int.Parse).ToArray();
        if(values[0]<0||values[1]<0||values[2]<=0||values[3]<=0||values[0]>(long)size[2]-values[2]||values[1]>(long)size[3]-values[3])return null;
        return full with{Location=location};
    }
}
