namespace SMSR.App.Mvp;

public sealed record OperatorInstruction(string Action, string Instruction, DateTimeOffset CreatedAt);

public sealed class OperatorInstructionQueue
{
    private readonly Dictionary<string, OperatorInstruction> _pending = [];
    private readonly object _gate = new();

    public OperatorInstruction Set(string projectId, string workflowId, string nodeId, string action)
    {
        var instruction = new OperatorInstruction(action, action switch
        {
            "accelerate" => "정확성과 완료 기준은 유지하고, 불필요한 대기 제거와 병렬 가능한 하위 작업 분리로 이 노드를 더 빠르게 진행하세요.",
            "redesign" => "현재 접근과 지연 원인을 다시 검토하고, 필요하면 실행 가능한 하위 작업으로 재설계한 뒤 이 노드를 계속 진행하세요.",
            _ => "저장된 진행 상태와 최근 활동을 확인하고 이 노드의 미완료 작업을 이어서 진행하세요."
        }, DateTimeOffset.UtcNow);
        lock (_gate) _pending[Key(projectId, workflowId, nodeId)] = instruction;
        return instruction;
    }

    public OperatorInstruction? Take(string projectId, string workflowId, string nodeId)
    {
        lock (_gate)
        {
            var key = Key(projectId, workflowId, nodeId);
            if (!_pending.Remove(key, out var value)) return null;
            return value;
        }
    }

    public void Remove(string projectId, string workflowId, string nodeId)
    {
        lock (_gate) _pending.Remove(Key(projectId, workflowId, nodeId));
    }

    private static string Key(string projectId, string workflowId, string nodeId)
        => $"{projectId}\n{workflowId}\n{nodeId}";
}

public sealed record OperatorInstructionRequest(string ProjectId, string WorkflowId, string NodeId, string Action);
