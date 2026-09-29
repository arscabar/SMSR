using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SMSR.App.Mvp;

internal sealed partial class GraphLspJob
{
    internal void Terminate()
    {
        if(!TerminateJobObject(_handle,1)) throw new Win32Exception();
    }
    internal async Task WaitForEmptyAsync()
    {
        var timer=Stopwatch.StartNew();
        while(true)
        {
            if(!QueryInformationJobObject(_handle,1,out var info,(uint)Marshal.SizeOf<Accounting>(),IntPtr.Zero))
                throw new Win32Exception();
            if(info.ActiveProcesses==0) return;
            if(timer.Elapsed>TimeSpan.FromSeconds(5)) throw new TimeoutException("LSP job did not exit");
            await Task.Delay(20);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Accounting
    {
        public long UserTime,KernelTime,PeriodUserTime,PeriodKernelTime;
        public uint PageFaults,TotalProcesses,ActiveProcesses,TerminatedProcesses;
    }
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateJobObject(SafeFileHandle job,uint exitCode);
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryInformationJobObject(SafeFileHandle job,int infoClass,out Accounting info,uint size,IntPtr length);
}
