using System.IO;
namespace SMSR.App.Mvp;
internal static class GraphMediaRuntime
{
    internal static string Fingerprint()
    {
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SMSR","models");
        var model=Environment.GetEnvironmentVariable("SMSR_WHISPER_MODEL")??Path.Combine(root,"whisper-tiny");
        var paths=new[]{Path.Combine(model,"model.bin"),Path.Combine(model,"smsr-model-manifest.json"),
            Path.Combine(root,"tessdata","smsr-model-manifest.json"),
            Path.Combine(root,"tessdata","eng.traineddata"),Path.Combine(root,"tessdata","kor.traineddata"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Tesseract-OCR","tesseract.exe")};
        var state=string.Join(";",paths.Select(p=>{var file=new FileInfo(p);return file.Exists?$"{file.Length}:{file.LastWriteTimeUtc.Ticks}":"missing";}));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("media-v1:"+state)));
    }
}
