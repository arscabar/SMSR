using System.IO;
namespace SMSR.App.Mvp;
internal static class GraphWatchPath
{
    internal static bool Allowed(string root,string absolute)
    {
        var path=Path.GetFullPath(absolute);root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))return false;
        var relative=Path.GetRelativePath(root,path).Replace('\\','/');
        if(GraphFileScanner.Allowed(relative))return true;
        // Directory rename/deletion can change many indexed children; the scanner still enforces Git/sensitive/reparse rules.
        return !Path.HasExtension(relative)&&!GraphFilePolicy.Sensitive(relative)
            &&!relative.Split('/').Any(p=>new[]{".git","bin","obj","node_modules","dist",".venv",".cache","graphify-out","secrets"}.Contains(p,StringComparer.OrdinalIgnoreCase));
    }
}
