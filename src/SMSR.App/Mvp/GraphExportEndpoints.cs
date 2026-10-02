using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
namespace SMSR.App.Mvp;
internal static class GraphExportEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/api/graph/knowledge-export",async(string projectId,string? nodeId,string? format,int? depth,GraphExportService service,CancellationToken ct)=>
        {
            try
            {
                if(format is not(null or "json" or "html" or "svg" or "graphml" or "wiki" or "obsidian"))throw new ArgumentException("형식은 json/html/svg/graphml/wiki/obsidian입니다.");
                var data=await service.GetAsync(projectId,nodeId,depth??2,ct);
                if(format is "wiki" or "obsidian")
                {
                    var archive=GraphExportWiki.Render(data,format=="obsidian");
                    if(archive.Length>64_000_000)throw new InvalidOperationException("ZIP 내보내기 64MB 한도를 초과했습니다.");
                    return Results.File(archive,"application/zip","smsr-knowledge-"+format+".zip");
                }
                var content=format switch{"html"=>GraphExportHtml.Render(data),"svg"=>GraphExportSvg.Render(data),"graphml"=>GraphExportGraphML.Render(data),_=>JsonSerializer.Serialize(data,GraphWorker.Json)};
                if(Encoding.UTF8.GetByteCount(content)>64_000_000)throw new InvalidOperationException("64MB 내보내기 한도를 초과했습니다. 선택 범위를 줄이세요.");
                return Results.File(Encoding.UTF8.GetBytes(content),format=="html"?"text/html":format=="svg"?"image/svg+xml":format=="graphml"?"application/graphml+xml":"application/json","smsr-knowledge."+(format??"json"));
            }
            catch(ArgumentException e){return Results.BadRequest(new{error=e.Message});}
            catch(KeyNotFoundException e){return Results.NotFound(new{error=e.Message});}
            catch(InvalidOperationException e){return Results.Conflict(new{error=e.Message});}
        });
    }
}
