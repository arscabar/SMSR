using System.IO;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal static class GraphJdtInstallation
{
    // Verified profile, not a signature check. Trust is the user's installed VS Code extension.
    internal const string Version="1.56.0";
    internal static string Resolve()
    {
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".vscode","extensions","redhat.java-"+Version+"-win32-x64");
        if(!Directory.Exists(root)) throw new InvalidOperationException("검증된 VS Code Red Hat Java 1.56.0 win32-x64 설치가 없습니다. 자동 설치하지 않습니다.");
        for(var parent=new DirectoryInfo(root);parent is not null;parent=parent.Parent)
            if(parent.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("연결된 JDT 설치 경로는 허용하지 않습니다.");
        var folders=new Queue<string>();folders.Enqueue(root);
        while(folders.TryDequeue(out var folder))
            foreach(var path in Directory.EnumerateFileSystemEntries(folder))
            {
                var attributes=File.GetAttributes(path);
                if(attributes.HasFlag(FileAttributes.ReparsePoint)) throw new InvalidOperationException("연결 파일이 있는 JDT 설치는 허용하지 않습니다.");
                if(attributes.HasFlag(FileAttributes.Directory)) folders.Enqueue(path);
            }
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"package.json")));
        var data=manifest.RootElement;
        if(data.GetProperty("publisher").GetString()!="redhat" || data.GetProperty("name").GetString()!="java" ||
            data.GetProperty("version").GetString()!=Version)
            throw new InvalidOperationException("JDT 설치 식별자가 일치하지 않습니다.");
        return root;
    }
}
