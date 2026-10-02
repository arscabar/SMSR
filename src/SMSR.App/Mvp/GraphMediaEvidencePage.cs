using System.Globalization;
namespace SMSR.App.Mvp;
internal static class GraphMediaEvidencePage
{
    internal static string Render(string projectId,string path,GraphDocument doc,string? location)
    {
        if(path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase))
        {
            var page=1;var parts=location?.Split(':',3);
            if(parts is not null&&parts.Length>=2&&parts[0]=="page")int.TryParse(parts[1],out page);
            if(page is <1 or >50)return "<p>PDF 페이지 미리보기 한도: 1~50.</p>";
            var prefix=$"page:{page}:";var subset=doc with{Blocks=doc.Blocks.Where(b=>b.Location.StartsWith(prefix,StringComparison.Ordinal)).Select(b=>b with{Location=b.Location[prefix.Length..]}).ToArray()};
            return Image("/graph/document-page-image?projectId="+Uri.EscapeDataString(projectId)+"&amp;path="+Uri.EscapeDataString(path)+"&amp;page="+page,subset,parts?.Length==3?parts[2]:null);
        }
        if(!GraphMediaTypes.TryGet(path,out var type)||path.EndsWith(".svg",StringComparison.OrdinalIgnoreCase))return "";
        var url="/graph/media?projectId="+Uri.EscapeDataString(projectId)+"&amp;path="+Uri.EscapeDataString(path);
        if(type.Kind=="image")return Image(url,doc,location);
        var seconds=0d;
        if(location?.StartsWith("time:",StringComparison.Ordinal)==true)
            double.TryParse(location.Split(':').ElementAtOrDefault(1),NumberStyles.Float,CultureInfo.InvariantCulture,out seconds);
        if(!double.IsFinite(seconds)||seconds<0)seconds=0;
        return "<"+type.Kind+" controls preload=\"metadata\" style=\"max-width:100%;max-height:500px\" src=\""+url+"#t="+seconds.ToString(CultureInfo.InvariantCulture)+"\"></"+type.Kind+"><p>근거 시간: "+seconds.ToString("0.###",CultureInfo.InvariantCulture)+"초</p>";
    }
    private static string Image(string url,GraphDocument doc,string? location)
    {
        var overlay="";var full=doc.Blocks.FirstOrDefault(b=>b.Location.StartsWith("image:0,0,",StringComparison.Ordinal));
        if(location is not null&&full is not null&&GraphMediaRegion.Evidence(doc,location,full.Text)is not null)
        {
            var box=location[6..].Split(',').Select(int.Parse).ToArray();var size=full.Location[6..].Split(',').Select(int.Parse).ToArray();
            string P(int value,int maximum)=>(100d*value/maximum).ToString("0.###",CultureInfo.InvariantCulture)+"%";
            overlay=$"<span class=\"media-region\" style=\"position:absolute;pointer-events:none;box-sizing:border-box;border:3px solid #ffbd4a;left:{P(box[0],size[2])};top:{P(box[1],size[3])};width:{P(box[2],size[2])};height:{P(box[3],size[3])}\"></span>";
        }
        return "<div style=\"position:relative;display:inline-block;max-width:100%\"><img alt=\"이미지 원문\" style=\"display:block;width:100%;max-width:900px;height:auto\" src=\""+url+"\">"+overlay+"</div><p>영역 위치: "+GraphPage.Encode(location??"전체 이미지")+" · 시각 해석은 후보 근거입니다.</p>";
    }
}
