using System.IO;

namespace SMSR.App.Mvp;

// Installed-server profile. Never accepts a command from a repository/API.
internal static class GraphJdtProfile
{
    internal static object Options => new { settings=new { java=new {
        autobuild=new {enabled=false}, sharedIndexes=new {enabled="off"},
        @import=new {maven=new {enabled=false,offline=new {enabled=true}},
            gradle=new {enabled=false,wrapper=new {enabled=false},offline=new {enabled=true}}},
        configuration=new {updateBuildConfiguration="disabled"},
        implementationsCodeLens=new {enabled=false},referencesCodeLens=new {enabled=false}
    }}};
    internal static GraphLspSession Start(string extension,string root,string project,CancellationToken ct=default)
    {
        if(!Path.IsPathFullyQualified(extension)) throw new ArgumentException("An installed extension absolute path is required");
        var server=Path.Combine(extension,"server");
        var java=Directory.GetFiles(Path.Combine(extension,"jre"),"java.exe",SearchOption.AllDirectories).Single();
        var launcher=Directory.GetFiles(Path.Combine(server,"plugins"),"org.eclipse.equinox.launcher_*.jar").Single();
        var config=Path.Combine(root,"config");Directory.CreateDirectory(config);
        var original=Path.Combine(server,"config_win");
        foreach(var file in Directory.GetFiles(original,"*",SearchOption.AllDirectories))
        {
            var target=Path.Combine(config,Path.GetRelativePath(original,file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target);
        }
        var home=Path.Combine(root,"home");var temp=Path.Combine(root,"tmp");
        Directory.CreateDirectory(home);Directory.CreateDirectory(temp);
        return new(java,["-Xmx512m","-XX:-UsePerfData","-Duser.home="+home,"-Djava.io.tmpdir="+temp,
            "-Declipse.application=org.eclipse.jdt.ls.core.id1","-Declipse.product=org.eclipse.jdt.ls.core.product",
            "-Dosgi.bundles.defaultStartLevel=4","-Dosgi.install.area="+new Uri(server+Path.DirectorySeparatorChar).AbsoluteUri,
            "--add-modules=ALL-SYSTEM","--add-opens","java.base/java.util=ALL-UNNAMED",
            "--add-opens","java.base/java.lang=ALL-UNNAMED","-jar",launcher,
            "-configuration",config,"-data",Path.Combine(root,"data")],project,ct);
    }
}
