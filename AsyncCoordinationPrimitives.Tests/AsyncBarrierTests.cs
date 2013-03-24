using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace AsyncCoordinationPrimitives.Tests
{
    [TestClass]
    public class AsyncBarrierTests
    {
        private static int SampleTimes = 5;

        [TestMethod]
        public void ItReturnsTaskWhenSignalAndWaitIsCalled()
        {
            var asyncBarrier = new AsyncBarrier(SampleTimes);
            Assert.IsInstanceOfType(asyncBarrier.SignalAndWait(), typeof(Task));
        }

        [TestMethod]
        public void ItCompletesTheTaskWhenSignalAnsWaitIsCalledSampleTimes()
        {
            var asyncBarrier = new AsyncBarrier(SampleTimes);
            var tasks = new Task[SampleTimes];
            for (int i = 0; i < SampleTimes; i++)
            {
                tasks[i] = asyncBarrier.SignalAndWait();
            }
            var task = Task.WhenAll(tasks);
            task.Wait(TimeSpan.FromSeconds(1));
            Assert.IsTrue(task.IsCompleted);
        }
    }
}
