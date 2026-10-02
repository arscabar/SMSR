namespace SMSR.App.Mvp;

internal static class GraphStructurePaths
{
    internal static string Normalize(string? path)
    {
        var value=(path??"").Replace('\\','/').TrimEnd('/');
        if(value.Length>4096||value.StartsWith('/')||value.Contains(':')
            ||value.Split('/').Any(p=>p is "." or ".."))
            throw new ArgumentException("프로젝트 안의 상대 경로를 선택하세요.");
        return value;
    }
    internal static bool Inside(string source,string path,bool file)
        =>file?source==path:path.Length==0||source.StartsWith(path+"/",StringComparison.Ordinal);
    internal static GraphStructureItem Bucket(GraphNode n,string focus,bool file,bool inside)
    {
        var path=Normalize(n.SourcePath);
        if(path.Length==0)return new("unknown:","","unknown","출처 없음",0,0);
        if(file&&inside)return new(n.NodeId,path,n.Kind,n.Label,0,0,n);
        var parts=path.Split('/');var depth=inside?(focus.Length==0?0:focus.Split('/').Length):0;
        if(!inside&&focus.Length>0){var f=focus.Split('/');
            while(depth<Math.Min(f.Length,parts.Length-1)&&f[depth]==parts[depth])depth++;}
        var folder=depth<parts.Length-1;var key=string.Join('/',parts.Take(depth+1));
        return new((folder?"folder:":"file:")+key,key,folder?"folder":n.NodeId.StartsWith("file:",StringComparison.Ordinal)?n.Kind:"file",
            parts[depth],0,0,folder?null:n.NodeId.StartsWith("file:",StringComparison.Ordinal)?n:null);
    }
}
