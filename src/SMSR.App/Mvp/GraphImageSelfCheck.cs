using System.Diagnostics;
using System.IO;

namespace SMSR.App.Mvp;

internal static class GraphImageSelfCheck
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "smsr-image-" + Guid.NewGuid().ToString("N"));
        var database = root + ".db";
        Directory.CreateDirectory(Path.Combine(root, "docs"));
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        try
        {
            var git = new ProcessStartInfo("git") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true };
            git.ArgumentList.Add("init"); git.ArgumentList.Add("-q");
            using var process = Process.Start(git) ?? throw new InvalidOperationException("Git 실행 실패");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new InvalidOperationException("Git 저장소 생성 실패");
            await File.WriteAllTextAsync(Path.Combine(root, "docs", "readme.md"), "# 그림\n![diagram](../assets/diagram.png)\n");
            await File.WriteAllBytesAsync(Path.Combine(root, "assets", "diagram.png"), [137, 80, 78, 71, 0, 1]);
            await File.WriteAllBytesAsync(Path.Combine(root, "assets", "sound.wav"), [82, 73, 70, 70, 0, 0, 0, 0]);
            await File.WriteAllBytesAsync(Path.Combine(root, "assets", "clip.mp4"), [0, 0, 0, 8, 102, 116, 121, 112]);
            await File.WriteAllTextAsync(Path.Combine(root, "assets", "unsafe.svg"), "<svg><script>alert(1)</script></svg>");
            var store = new EventStore(database); await store.InitializeAsync();
            await new GraphIndexService(store).IndexAsync("images", root);
            var image = await store.GetGraphNodeAsync("images", "file:assets/diagram.png");
            var path = await new GraphQueryService(store).PathAsync("images", "file:docs/readme.md", "file:assets/diagram.png");
            if (image?.Kind != "image" || !path.Found || path.Edges.Last().Relation != "LINKS_TO")
                throw new InvalidOperationException("이미지 종류·문서 연결 색인 실패");
            await GraphMediaSelfCheck.VerifyAsync(store, root);
        }
        finally
        {
            Directory.Delete(root, true);
            foreach (var file in new[] { database, database + "-wal", database + "-shm" })
                if (File.Exists(file)) File.Delete(file);
        }
    }
}
