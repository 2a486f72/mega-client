namespace AsyncCoordinationPrimitives.Tests
{
	using System;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class AsyncCountdownEventTests
	{
		private const int SampleTimes = 5;

		[TestMethod]
		public void ItReturnsTaskWhenWaitAsyncIsCalled()
		{
			var asyncACE = new AsyncCountdownEvent(SampleTimes);
			Assert.IsInstanceOfType(asyncACE.WaitAsync(), typeof(Task));
		}

		[TestMethod]
		public void ItCompletesTheTaskWhenSignalIsCalledSampleTimes()
		{
			var asyncACE = new AsyncCountdownEvent(SampleTimes);
			var task = asyncACE.WaitAsync();
			for (int i = 0; i < SampleTimes; i++)
			{
				asyncACE.Signal();
			}
			task.Wait(TimeSpan.FromSeconds(1));
			Assert.IsTrue(task.IsCompleted);
		}
	}
}