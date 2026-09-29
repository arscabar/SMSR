using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphMediaSelfCheck
{
    internal static async Task VerifyAsync(EventStore store, string root)
    {
        if ((await store.GetGraphNodeAsync("images", "file:assets/sound.wav"))?.Kind != "audio"
            || (await store.GetGraphNodeAsync("images", "file:assets/clip.mp4"))?.Kind != "video")
            throw new InvalidOperationException("영상·음원 종류 색인 실패");
        var media = new GraphMediaService(store);
        var opened = await media.OpenAsync("images", "assets/sound.wav", CancellationToken.None);
        await using (opened.Stream)
            if (opened.Stream.Length != 8 || opened.Mime != "audio/wav")
                throw new InvalidOperationException("음원 미리보기 실패");
        try { await media.OpenAsync("images", "assets/unsafe.svg", CancellationToken.None); throw new InvalidOperationException("SVG 차단 실패"); }
        catch (ArgumentException) { }
        await File.WriteAllBytesAsync(Path.Combine(root, "assets", "sound.wav"), [1, 2, 3]);
        try { await media.OpenAsync("images", "assets/sound.wav", CancellationToken.None); throw new InvalidOperationException("변경된 미디어 차단 실패"); }
        catch (InvalidOperationException error) when (error.Message.Contains("색인 이후", StringComparison.Ordinal)) { }
    }
}
