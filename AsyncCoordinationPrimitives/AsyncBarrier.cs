using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncCoordinationPrimitives
{
    public class AsyncBarrier
    {
        private readonly int m_participantCount;
        private int m_remainingParticipants;
        private ConcurrentStack<TaskCompletionSource<bool>> m_waiters;

        public AsyncBarrier(int participantCount)
        {
            if (participantCount <= 0) throw new ArgumentOutOfRangeException("participantCount");
            m_remainingParticipants = m_participantCount = participantCount;
            m_waiters = new ConcurrentStack<TaskCompletionSource<bool>>();
        }

        public Task SignalAndWait()
        {
            var tcs = new TaskCompletionSource<bool>();
            m_waiters.Push(tcs);
            if (Interlocked.Decrement(ref m_remainingParticipants) == 0)
            {
                m_remainingParticipants = m_participantCount;
                var waiters = m_waiters;
                m_waiters = new ConcurrentStack<TaskCompletionSource<bool>>();
                Parallel.ForEach(waiters, w => w.SetResult(true));
            }
            return tcs.Task;
        }
    }
}
