namespace Mega.Tests.Client
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Mega.Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Useful;

	[TestClass]
	public sealed class RetryHelperTests
	{
		public static readonly RetryPolicy EverythingInThreesPolicy = new RetryPolicy(new[]
		{
			typeof(Exception)
		}, new[]
		{
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1)
		});

		public static readonly RetryPolicy NothingInThreesPolicy = new RetryPolicy(new Type[0], new[]
		{
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(1)
		});


		[TestMethod]
		public async Task Execute_WithoutRetry_WorksFine()
		{
			bool executed = false;

			using (var feedback = new DebugFeedbackChannel(""))
				await RetryHelper.ExecuteWithRetryAsync(async delegate
				{
					await Task.Yield();
					executed = true;
				}, EverythingInThreesPolicy, feedback, CancellationToken.None);

			Assert.IsTrue(executed);
		}

		[TestMethod]
		public async Task Execute_WithOneRetry_WorksFine()
		{
			int executionCount = 0;

			using (var feedback = new DebugFeedbackChannel(""))
				await RetryHelper.ExecuteWithRetryAsync(async delegate
				{
					await Task.Yield();

					if (executionCount++ == 0)
						throw new Exception("First time failure.");
				}, EverythingInThreesPolicy, feedback, CancellationToken.None);

			Assert.AreEqual(2, executionCount);
		}

		[TestMethod]
		[ExpectedException(typeof(ContractException))]
		public async Task Execute_WithAlwaysFailingFunc_WorksFine()
		{
			using (var feedback = new DebugFeedbackChannel(""))
				await RetryHelper.ExecuteWithRetryAsync(async delegate
				{
					await Task.Yield();

					throw new ContractException("Always a failure.");
				}, EverythingInThreesPolicy, feedback, CancellationToken.None);
		}

		[TestMethod]
		public async Task Execute_CancelledBeforeFirstRetry_WorksFine()
		{
			var cts = new CancellationTokenSource();

			int executionCount = 0;

			try
			{
				using (var feedback = new DebugFeedbackChannel(""))
					await RetryHelper.ExecuteWithRetryAsync(async delegate
					{
						executionCount++;

						await Task.Yield();

						cts.Cancel();

						throw new Exception("Always a failure.");
					}, EverythingInThreesPolicy, feedback, cts.Token);

				Assert.Fail("Did not see OperationCanceledException");
			}
			catch (OperationCanceledException)
			{
			}

			Assert.AreEqual(1, executionCount);
		}

		[TestMethod]
		public async Task Execute_ThrowingNonretriableException_IsNotRetries()
		{
			int executionCount = 0;

			try
			{
				using (var feedback = new DebugFeedbackChannel(""))
					await RetryHelper.ExecuteWithRetryAsync(async delegate
					{
						await Task.Yield();

						if (executionCount++ == 0)
							throw new InvalidOperationException("Cannot retry this");
					}, NothingInThreesPolicy, feedback, CancellationToken.None);

				Assert.Fail("Exception with nonretriable failure was not thrown.");
			}
			catch (InvalidOperationException ex)
			{
				Assert.AreEqual("Cannot retry this", ex.Message);
			}

			Assert.AreEqual(1, executionCount);
		}
	}
}