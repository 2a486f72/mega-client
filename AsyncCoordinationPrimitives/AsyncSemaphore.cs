using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncCoordinationPrimitives
{
    public class AsyncSemaphore
    {
        private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
        private int m_currentCount;
        private AsyncLock @lock = new AsyncLock();

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
            m_currentCount = initialCount;
        }

        public async Task WaitAsync()
        {
            TaskCompletionSource<bool> tcs;

            using(await @lock.LockAsync())
            {
                if (m_currentCount > 0)
                {
                    --m_currentCount;
                    return;
                }
                else
                {
                    tcs = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(tcs);
                }
            }

            if (tcs != null)
            {
                await tcs.Task;
            }
        }

        public async void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            using (await @lock.LockAsync())
            {
                if (m_waiters.Count > 0)
                    toRelease = m_waiters.Dequeue();
                else
                    ++m_currentCount;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }
}
