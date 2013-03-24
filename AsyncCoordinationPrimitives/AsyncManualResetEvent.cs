using System.Threading;
using System.Threading.Tasks;

public class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

    public async Task WaitAsync() { await m_tcs.Task; }

    public async void Set() 
    {
        var tcs = m_tcs;
        await Task.Factory.StartNew(s =>
                  ((TaskCompletionSource<bool>)s).TrySetResult(true),
                  tcs, 
                  CancellationToken.None, 
                  TaskCreationOptions.PreferFairness, 
                  TaskScheduler.Default); 
    }

    public void Reset()
    {
        while (true)
        {
            var tcs = m_tcs;
            if (!tcs.Task.IsCompleted ||
                Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                return;
        }
    }
}