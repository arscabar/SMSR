using System.Text.Json;
namespace SMSR.App.Mvp;
internal static class GraphQuestionBudget
{
    internal static GraphQuestionEvidence Pack(int revision,GraphNode[] nodes,GraphEdge[] edges,
        GraphFeedbackLesson[] lessons,bool truncated,int budget)
    {
        GraphQuestionEvidence result;
        while(true)
        {
            var ids=nodes.Select(n=>n.NodeId).ToHashSet();edges=edges.Where(e=>ids.Contains(e.SourceId)&&ids.Contains(e.TargetId)).ToArray();
            lessons=lessons.Where(l=>ids.Contains(l.Feedback.SourceId)).ToArray();
            result=new(revision,nodes,edges,lessons,truncated,0,budget,"UTF-8 JSON 바이트 수를 토큰 상한으로 사용한 보수적 예산; 실제 토크나이저 아님",
                "색인 스냅샷 근거·피드백 안내. 원문 상태는 열기에서 재확인. 질문 미저장, 후보 별도 취급, 실제 실행 순서 아님.");
            var count=JsonSerializer.SerializeToUtf8Bytes(result,GraphWorker.Json).Length;
            result=result with {ByteCount=count+16};
            count=JsonSerializer.SerializeToUtf8Bytes(result,GraphWorker.Json).Length;
            if(count<=budget)return result;
            truncated=true;
            if(lessons.Length>0)lessons=lessons[..^1];
            else if(edges.Length>0)edges=edges[..^1];
            else if(nodes.Length>0)nodes=nodes[..^1];
            else throw new ArgumentException("근거 예산이 응답 메타데이터보다 작습니다.");
        }
    }
}
