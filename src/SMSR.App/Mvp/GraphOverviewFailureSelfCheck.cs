namespace SMSR.App.Mvp;

internal static class GraphOverviewFailureSelfCheck
{
    internal static void Run()
    {
        foreach(var (error,code) in new (Exception,string)[]{
            (new OperationCanceledException(),"ANALYSIS_CANCELLED"),
            (new OutOfMemoryException(),"MEMORY_LIMIT"),
            (new TimeoutException(),"TIME_LIMIT"),
            (new ArgumentException("password=do-not-record"),"INPUT_LIMIT"),
            (new InvalidOperationException("secret=do-not-record"),"WORKER_FAILURE")})
        {
            var result=GraphOverviewFailure.Describe(Task.FromException(error));
            if(!result.StartsWith(code,StringComparison.Ordinal)||result.Contains("do-not-record"))
                throw new Exception("Overview failure category or privacy failed");
        }
        if(!GraphOverviewFailure.Describe(Task.FromCanceled(new CancellationToken(true))).StartsWith("ANALYSIS_CANCELLED"))
            throw new Exception("Cancelled overview not classified");
    }
}
