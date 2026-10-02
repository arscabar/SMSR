using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class DashboardPanelPreviewSelfCheck
{
    internal static async Task RunAsync(string folder)
    {
        Directory.CreateDirectory(folder);
        var store = new EventStore(Path.Combine(folder, "smsr.db"));
        await store.InitializeAsync();
        var plan=new List<PlanNodeDefinition>{new("index","색인과 근거 저장 확인")};
        for(var i=0;i<20;i++)plan.Add(new("step-"+i,"검증 단계 "+(i+1),DependsOn:[i==0?"index":"step-"+(i-1)]));
        plan.Add(new("ui","우측 패널 접기와 카드 통일 확인",DependsOn:["step-19"]));
        await store.SavePlanAsync("preview", "panel-check",plan);
        await store.SaveWorkflowContextAsync(new("preview", "panel-check", "작업 이력이 길어 상세가 잘 보이지 않아 접기와 카드 통일이 필요합니다.",
            "기존 데이터를 유지하고 공통 접이식 카드와 저장된 펼침 상태를 사용합니다.", "기본 펼침과 선택적 접기, 화면 갱신 후 상태 유지 검증 중입니다.", DateTimeOffset.UtcNow));
        await store.RecordAsync(new("index-ok", "preview", "panel-check", "index", "preview-agent",
            "NODE_STATUS_CHANGED", "SUCCESS", "심벌 정보와 관계 근거 저장 확인", null, null,
            ["docs/graphify-foundation-status-2026-10-01.html"]));
        await store.RecordAsync(new("ui-active", "preview", "panel-check", "ui", "preview-agent",
            "NODE_STATUS_CHANGED", "IN_PROGRESS", "접기·펼치기와 키보드 조작 확인", null, null, null));
        var source=Path.Combine(folder,"fixture");Directory.CreateDirectory(source);
        using(var git=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git"){ArgumentList={"-C",source,"init","-q"},UseShellExecute=false,CreateNoWindow=true})!){await git.WaitForExitAsync();}
        await File.WriteAllTextAsync(Path.Combine(source,"store.py"),"def save():\n return True\ndef run():\n return save()\n");
        await File.WriteAllTextAsync(Path.Combine(source,"plan.md"),"# 근거 저장\n[코드](store.py)에서 이력을 저장합니다.\n");
        await new GraphIndexService(store).IndexAsync("preview",source);
        await using var host = await LocalServer.StartAsync(folder, 0);
        await File.WriteAllTextAsync(Path.Combine(folder, "ready.json"), JsonSerializer.Serialize(new {
            url = host.Address + "/dashboard?projectId=preview&workflowId=panel-check&selectedNodeId=ui" }));
        var deadline = DateTimeOffset.UtcNow.AddMinutes(5);
        while (DateTimeOffset.UtcNow < deadline && !File.Exists(Path.Combine(folder, "stop")))
            await Task.Delay(250);
    }
}
