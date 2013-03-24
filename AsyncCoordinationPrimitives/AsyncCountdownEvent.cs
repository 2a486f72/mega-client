using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncCoordinationPrimitives
{
    public class AsyncCountdownEvent
    {
        private readonly AsyncManualResetEvent m_amre = new AsyncManualResetEvent();
        private int m_count;

        public AsyncCountdownEvent(int initialCount)
        {
            if (initialCount <= 0) throw new ArgumentOutOfRangeException("initialCount");
            m_count = initialCount;
        }

        public async Task WaitAsync() { await m_amre.WaitAsync(); }

        public void Signal()
        {
            if (m_count <= 0)
                throw new InvalidOperationException();

            int newCount = Interlocked.Decrement(ref m_count);
            if (newCount == 0)
                m_amre.Set();
            else if (newCount < 0)
                throw new InvalidOperationException();
        }
    }
}
