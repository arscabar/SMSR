using System.Threading;

namespace SMSR.App.Services;

internal sealed class MainInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\SMSR.App.Main";
    private const string ActivationName = @"Local\SMSR.App.Activate";
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activation;
    private RegisteredWaitHandle? _listener;

    private MainInstanceGuard(Mutex mutex)
    {
        _mutex = mutex;
        _activation = new(false, EventResetMode.AutoReset, ActivationName);
    }

    public static MainInstanceGuard? TryAcquire()
    {
        var mutex = new Mutex(true, MutexName, out var created);
        if (created) return new(mutex);
        mutex.Dispose();
        return null;
    }

    public static bool IsRunning()
    {
        try { using var mutex = Mutex.OpenExisting(MutexName); return true; }
        catch (WaitHandleCannotBeOpenedException) { return false; }
    }

    public static void RequestActivation()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try { using var activation = EventWaitHandle.OpenExisting(ActivationName); activation.Set(); return; }
            catch (WaitHandleCannotBeOpenedException) { Thread.Sleep(50); }
        }
    }

    public void Listen(Action activate)
    {
        _listener = ThreadPool.RegisterWaitForSingleObject(
            _activation, (_, _) => activate(), null, Timeout.Infinite, false);
    }

    public void Dispose()
    {
        _listener?.Unregister(null);
        _activation.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
