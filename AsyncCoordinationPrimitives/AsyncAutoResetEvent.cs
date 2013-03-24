using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncCoordinationPrimitives
{
    public class AsyncAutoResetEvent
    {
        private readonly Queue<TaskCompletionSource<bool>> m_waits = new Queue<TaskCompletionSource<bool>>();
        private bool m_signaled;
        private AsyncLock @lock = new AsyncLock();

        public async Task WaitAsync()
        {
            TaskCompletionSource<bool> tcs;

            using(await @lock.LockAsync())
            {
                if (m_signaled)
                {
                    m_signaled = false;
                    return;
                }
                else
                {
                    tcs = new TaskCompletionSource<bool>();
                    m_waits.Enqueue(tcs);
                }
            }

            if (tcs != null)
            {
                await tcs.Task;
            }
        }

        public async void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            using(await @lock.LockAsync())
            {
                if (m_waits.Count > 0)
                    toRelease = m_waits.Dequeue();
                else if (!m_signaled)
                    m_signaled = true;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }
}
