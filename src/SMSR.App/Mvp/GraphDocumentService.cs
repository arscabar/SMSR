using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

public sealed record GraphDocumentBlock(string Location, string Text, int Line);
public sealed record GraphDocument(string SourceHash, string ExtractorVersion,
    GraphDocumentBlock[] Blocks, string Status, int ExcludedBlocks, int Revision = 0,string? ExtractionFingerprint=null);

public sealed partial class GraphDocumentService(EventStore store, GraphWorker worker,GraphMediaAnalysisService? mediaAnalysis=null)
{
    private readonly GraphMediaAnalysisService _media=mediaAnalysis??new(store,worker);
    internal static bool Binary(string path) => Path.GetExtension(path).ToLowerInvariant() is ".pdf" or ".docx" or ".xlsx";

    public async Task<GraphDocument> ReadAsync(string projectId, string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024 || path.Contains('\\')
            || path.Any(char.IsControl) || GraphFilePolicy.Sensitive(path))
            throw new ArgumentException("문서 경로가 올바르지 않습니다.");
        if (EventValidation.ValidateWorkflowIds(projectId, "graph") is { } error) throw new ArgumentException(error);
        if(GraphMediaTypes.TryGet(path,out _))
        {
            var media=await _media.ReadAsync(projectId,path,ct);
            return new(media.SourceHash,media.ExtractorVersion,media.Blocks,media.Status,media.ExcludedBlocks,media.Revision,media.ExtractionFingerprint);
        }
        if (!Binary(path) && !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            if (Path.GetExtension(path).ToLowerInvariant() is not (".md" or ".markdown" or ".mdx" or ".txt"))
                throw new ArgumentException("지원하는 문서 형식이 아닙니다.");
            return GraphDocumentText.Convert(path, await new GraphSourceService(store).ReadAsync(projectId, path, ct));
        }
        var info = await store.GetGraphInfoAsync(projectId, ct) ?? throw new KeyNotFoundException("색인이 없습니다.");
        var node = await store.GetGraphNodeAsync(projectId, "file:" + path, ct, info.Revision);
        if (node is null || (node.Kind != "document" && !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            || node.SourcePath != path) throw new KeyNotFoundException("색인된 문서가 아닙니다.");
        var request=new { operation = "document", root = info.RootPath,path=Path.GetFullPath(Path.Combine(info.RootPath,path)),hash=node.Hash };
        var fingerprint=path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase)?GraphMediaRuntime.Fingerprint():null;
        var result=path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase)?await worker.RunPdfAsync(request,ct):await worker.RunDocumentAsync(request,ct);
        var document = result.Deserialize<GraphDocument>(GraphWorker.Json) ?? throw new InvalidOperationException("문서 변환 실패");
        if ((await store.GetGraphInfoAsync(projectId, ct))?.Revision != info.Revision)
            throw new InvalidOperationException("변환 중 색인이 변경되었습니다.");
        var safe = document.Blocks.Where(b => !GraphDocumentRedaction.Excluded(b.Text)).ToArray();
        if(fingerprint is not null&&fingerprint!=GraphMediaRuntime.Fingerprint())throw new InvalidOperationException("변환 중 로컬 모델·도구가 변경됐습니다.");
        return document with { Revision = info.Revision, Blocks = safe, ExcludedBlocks = document.ExcludedBlocks + document.Blocks.Length - safe.Length,ExtractionFingerprint=fingerprint };
    }
}
