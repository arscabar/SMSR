namespace SMSR.App.Mvp;

internal static class GraphRolePreviewFixture
{
    internal static async Task CreateAsync(EventStore store)
    {
        var service = new GraphRoleService(store);
        var context = await service.ContextAsync("documents", "file:src/service.py");
        await service.SubmitAsync(new("documents", context.Node.NodeId, context.Revision,
            context.Fingerprint, "host-agent-reviewed-fixture",
            [new("ROLE", "save와 audit로 구성된 확인용 코드입니다. 실제 저장 처리는 구현하지 않았습니다.", "EXTRACTED", ["e0"], "return True"),
             new("BEHAVIOR", "save는 True를 반환하고 audit는 save의 반환값을 그대로 돌려줍니다.", "EXTRACTED", ["e0"], "return save()") ]));
    }
}
