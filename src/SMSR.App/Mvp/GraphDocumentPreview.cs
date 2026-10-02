using System.IO;
using System.Text.Json;
namespace SMSR.App.Mvp;
public sealed partial class GraphDocumentService
{
    public async Task<byte[]> PreviewAsync(string projectId,string path,int page,CancellationToken ct)
    {
        if(EventValidation.ValidateWorkflowIds(projectId,"graph")is{}error)throw new ArgumentException(error);
        if(string.IsNullOrWhiteSpace(path)||path.Length>1024||path.Contains('\\')||path.Any(char.IsControl)
            ||!path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase)||GraphFilePolicy.Sensitive(path)||page is <1 or >50)
            throw new ArgumentException("PDF 페이지·경로 제한을 확인하세요.");
        var info=await store.GetGraphInfoAsync(projectId,ct)??throw new KeyNotFoundException("색인이 없습니다.");
        var node=await store.GetGraphNodeAsync(projectId,"file:"+path,ct,info.Revision);
        if(node is null||node.SourcePath!=path||node.Kind!="document")throw new KeyNotFoundException("색인된 PDF가 아닙니다.");
        var result=await worker.RunPdfAsync(new{operation="document-preview",root=info.RootPath,
            path=Path.GetFullPath(Path.Combine(info.RootPath,path)),hash=node.Hash,page},ct);
        if((await store.GetGraphInfoAsync(projectId,ct))?.Revision!=info.Revision||result.GetProperty("sourceHash").GetString()!=node.Hash)
            throw new InvalidOperationException("PDF 색인이 변경되었습니다.");
        return Convert.FromBase64String(result.GetProperty("data").GetString()!);
    }
}
