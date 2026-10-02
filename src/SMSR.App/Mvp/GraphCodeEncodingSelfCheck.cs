using System.IO;
using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphCodeEncodingSelfCheck
{
    internal static async Task RunAsync(string root, EventStore store, GraphIndexService index,
        int revision, string targetId)
    {
        var path = Path.Combine(root, "b.py");
        await File.WriteAllBytesAsync(path, [0xff, 0xfe, 0xff]);
        try { await index.IndexAsync("graphify-test", root); throw new Exception("Invalid encoding was indexed"); }
        catch (DecoderFallbackException) { }
        if ((await store.GetGraphInfoAsync("graphify-test"))!.Revision != revision
            || (await store.GetGraphNodeAsync("graphify-test", targetId)) is null)
            throw new Exception("Failed index overwrote previous facts");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        await File.WriteAllBytesAsync(path, Encoding.GetEncoding(949).GetBytes("# 한글 원문\ndef target(x):\n return x\n"));
        await index.IndexAsync("graphify-test", root);
        if ((await store.GetGraphNodeAsync("graphify-test", targetId)) is null
            || !(await new GraphSourceService(store).ReadAsync("graphify-test", "b.py", default)).Text.Contains("한글 원문"))
            throw new Exception("CP949 symbol or original source missing");
    }
}
