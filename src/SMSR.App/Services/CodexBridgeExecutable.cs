using System.Diagnostics;
using System.IO;

namespace SMSR.App.Services;

internal static class CodexBridgeExecutable
{
    private const ushort ConsoleSubsystem = 3;

    public static string Ensure(string applicationPath)
    {
        var source = Path.GetFullPath(applicationPath);
        if (Path.GetFileName(source).Equals("SMSR.Bridge.exe", StringComparison.OrdinalIgnoreCase)) return source;
        var target = Path.Combine(Path.GetDirectoryName(source)!, "SMSR.Bridge.exe");
        if (IsCurrent(source, target)) return target;
        var temporary = target + ".tmp";
        try
        {
            File.Copy(source, temporary, true);
            SetSubsystem(temporary, ConsoleSubsystem);
            File.Move(temporary, target, true);
            return target;
        }
        catch
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            if (File.Exists(target)) return target;
            throw;
        }
    }

    public static string DashboardPath(string processPath)
    {
        var path = Path.GetFullPath(processPath);
        if (!Path.GetFileName(path).Equals("SMSR.Bridge.exe", StringComparison.OrdinalIgnoreCase)) return path;
        var dashboard = Path.Combine(Path.GetDirectoryName(path)!, "SMSR.App.exe");
        return File.Exists(dashboard) ? dashboard : path;
    }

    internal static ushort ReadSubsystem(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream);
        stream.Position = 0x3c;
        var peOffset = reader.ReadInt32();
        stream.Position = peOffset;
        if (reader.ReadUInt32() != 0x00004550) throw new InvalidDataException("실행 파일 PE 서명이 올바르지 않습니다.");
        stream.Position = peOffset + 24;
        var magic = reader.ReadUInt16();
        if (magic is not 0x10b and not 0x20b) throw new InvalidDataException("지원하지 않는 PE 형식입니다.");
        stream.Position = peOffset + 24 + 68;
        return reader.ReadUInt16();
    }

    private static bool IsCurrent(string source, string target)
    {
        try
        {
            return File.Exists(target) && ReadSubsystem(target) == ConsoleSubsystem
                && File.GetLastWriteTimeUtc(target) >= File.GetLastWriteTimeUtc(source)
                && FileVersionInfo.GetVersionInfo(target).FileVersion == FileVersionInfo.GetVersionInfo(source).FileVersion;
        }
        catch { return false; }
    }

    private static void SetSubsystem(string path, ushort subsystem)
    {
        _ = ReadSubsystem(path);
        using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        stream.Position = 0x3c;
        var peOffset = reader.ReadInt32();
        stream.Position = peOffset + 24 + 68;
        using var writer = new BinaryWriter(stream);
        writer.Write(subsystem);
    }
}
