namespace AsyncCoordinationPrimitives.Tests
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class AsyncManualResetEventTests
	{
		[TestMethod]
		public void ItReturnsTaskWhenWaitAsyncIsCalled()
		{
			var asyncMRE = new AsyncManualResetEvent();
			Assert.IsInstanceOfType(asyncMRE.WaitAsync(), typeof(Task));
		}

		[TestMethod]
		public void ItCompletesTheTaskWhenSetIsCalled()
		{
			var asyncMRE = new AsyncManualResetEvent();
			var task = asyncMRE.WaitAsync();
			asyncMRE.Set();
			task.Wait(TimeSpan.FromSeconds(1));
			Assert.IsTrue(task.IsCompleted);
		}

		[TestMethod]
		public void ItReturnsNewUncompletedTaskIfResetIsCalled()
		{
			var asyncMRE = new AsyncManualResetEvent();
			var task = asyncMRE.WaitAsync();
			asyncMRE.Set();
			asyncMRE.Reset();
			var newTask = asyncMRE.WaitAsync();
			Assert.IsTrue(newTask != task && !newTask.IsCompleted);
		}

		[TestMethod]
		public void ItReturnsTaskWithNoInnerDependencies()
		{
			var asyncMRE = new AsyncManualResetEvent();
			asyncMRE.Set();
			asyncMRE.WaitAsync().ContinueWith(_ => asyncMRE.Reset()).Wait();

			var task = asyncMRE.WaitAsync();
			Assert.IsFalse(task.IsCompleted);
		}
	}
}