using System.Security.Cryptography;
using System.Text.Json;

namespace SMSR.App.Mvp;

internal sealed record GraphJdtRequest(string ProjectId,string[] Paths,string Path,int Line,int Character,string[]? SourceRoots=null)
{
    internal GraphJdtRequest Normalize()
    {
        var paths=new GraphJavaRequest(ProjectId,Paths).Normalize().Paths;
        if(paths.Any(p=>p.Split('/').Any(s=>s.TrimEnd(' ','.')!=s)))
            throw new ArgumentException("Windows에서 같은 파일로 해석되는 경로는 허용하지 않습니다.");
        var roots=SourceRoots??[""];
        if(!paths.Contains(Path,StringComparer.Ordinal) || Line<0 || Character<0 || roots.Length is <1 or >20 ||
            roots.Any(r=>r is null || r.Length>1024 || r.Any(char.IsControl) || r.Contains('\\') || r.Contains(':') ||
                r.Length>0 && r.Split('/').Any(x=>x is "" or "." or "..")) ||
            roots.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=roots.Length ||
            roots.Any(r=>roots.Any(other=>other!=r && (other=="" || r.StartsWith(other+"/",StringComparison.OrdinalIgnoreCase)))) ||
            paths.Any(p=>!roots.Any(r=>r=="" || p.StartsWith(r+"/",StringComparison.Ordinal))))
            throw new ArgumentException("조회 파일/0부터 시작하는 좌표/중첩 없는 소스 루트를 확인하세요.");
        return this with {Paths=paths,SourceRoots=roots.Order(StringComparer.Ordinal).ToArray()};
    }
    internal string Key=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {Paths,Path,Line,Character,SourceRoots})));
}
