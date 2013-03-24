using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace AsyncCoordinationPrimitives.Tests
{
    [TestClass]
    public class AsyncSemaphoreTests 
    {
        private static int SampleTimes = 5;

        [TestMethod]
        public void ItReturnsTaskWhenWaitAsyncIsCalled()
        {
            var asyncSemaphore = new AsyncSemaphore(SampleTimes);
            Assert.IsInstanceOfType(asyncSemaphore.WaitAsync(), typeof(Task));
        }

        [TestMethod]
        public void ItCompletesTheTasksWhenWaitAsyncIsCalledSampleTimes()
        {
            var asyncSemaphore = new AsyncSemaphore(SampleTimes);
            var tasks = new Task[SampleTimes];
            for (int i = 0; i < SampleTimes; i++)
            {
                tasks[i] = asyncSemaphore.WaitAsync();
            }
            var task = Task.WhenAll(tasks);
            task.Wait(TimeSpan.FromSeconds(1));
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void ItReturnsNewUncompletedTasksWhenWaitAsyncIsCalledSamplePlusOneTimes()
        {
            var asyncSemaphore = new AsyncSemaphore(SampleTimes);
            var tasks = new Task[SampleTimes];
            for (int i = 0; i < SampleTimes; i++)
            {
                tasks[i] = asyncSemaphore.WaitAsync();
            }

            var task = Task.WhenAll(tasks);
            task.Wait(TimeSpan.FromSeconds(1));
            var newTask = asyncSemaphore.WaitAsync();
            Assert.IsTrue(newTask != task && task.IsCompleted && !newTask.IsCompleted);
        }
    }
}
