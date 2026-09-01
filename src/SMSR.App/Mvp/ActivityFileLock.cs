using System.IO;

namespace SMSR.App.Mvp;

internal static class ActivityFileLock
{
    public static T Run<T>(string key, Func<T> action)
    {
        using var mutex = new Mutex(false, $"Local\\SMSR-activity-{key}");
        var entered = false;
        try
        {
            try { entered = mutex.WaitOne(TimeSpan.FromSeconds(3)); }
            catch (AbandonedMutexException) { entered = true; }
            if (!entered) throw new IOException("활동 JSONL 잠금을 얻지 못했습니다.");
            return action();
        }
        finally
        {
            if (entered) mutex.ReleaseMutex();
        }
    }
}
