using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;

namespace AsyncCoordinationPrimitives.Tests
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [TestMethod]
        public void ItReturnsTaskWhenWaitAsyncIsCalled()
        {
            var asyncARE = new AsyncAutoResetEvent();
            Assert.IsInstanceOfType(asyncARE.WaitAsync(), typeof(Task));
        }

        [TestMethod]
        public void ItCompletesTheTaskWhenSetIsCalled()
        {
            var asyncARE = new AsyncAutoResetEvent();
            var task = asyncARE.WaitAsync();
            asyncARE.Set();
            task.Wait(TimeSpan.FromSeconds(1));
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public void ItReturnsNewUncompletedTaskAfterExecution()
        {
            var asyncARE = new AsyncAutoResetEvent();
            var task = asyncARE.WaitAsync();
            task.ContinueWith(_ => { });
            asyncARE.Set();
            var newTask = asyncARE.WaitAsync();
            Assert.IsTrue(newTask != task && !newTask.IsCompleted);
        }
    }
}
