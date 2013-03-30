namespace Mega.Tests.Api
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Mega.Api;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Newtonsoft.Json.Linq;

	[TestClass]
	public sealed class TransactionTests
	{
		/// <summary>
		/// Dummy transaction executor that returns a list of JObjects counting numbers.
		/// </summary>
		private Task<JArray> DummyExecutor(object[] commands, CancellationToken cancellationToken)
		{
			var results = new List<JObject>();

			for (int i = 0; i < commands.Length; i++)
				results.Add(JObject.FromObject(new
				{
					c = i
				}));

			return Task.FromResult(new JArray(results));
		}

		public class FakeResultObject
		{
			public int Data { get; set; }
		}

		private void AssetIsDummyResult(JArray result, int expectedResults)
		{
			Assert.AreEqual(expectedResults, result.Count);

			for (int i = 0; i < expectedResults; i++)
				Assert.AreEqual(i, result[i].Value<int>("c"));
		}

		[TestMethod]
		public void TransactionExecution_SeemsToWork()
		{
			var transaction = new Transaction(DummyExecutor);
			transaction.AddCommand(JObject.FromObject(new { first = "command" }));
			transaction.AddCommand(JObject.FromObject(new { second = "command" }));

			var result = transaction.ExecuteAsync().Result;

			AssetIsDummyResult(result, 2);
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void DoubleExecution_Fails()
		{
			var transaction = new Transaction(DummyExecutor);
			transaction.ExecuteAsync().Wait();
			transaction.ExecuteAsync().Wait();
		}

		[TestMethod]
		public void EmptyTransaction_IsExecutedFine()
		{
			var transaction = new Transaction(DummyExecutor);
			transaction.ExecuteAsync().Wait();
		}

		[TestMethod]
		public void ResultHandelrCalling_SeemsToWork()
		{
			var transaction = new Transaction((commands, ct) => Task.FromResult(new JArray(JObject.FromObject(new { Data = 666 }))));

			bool handlerWasCalled = false;
			FakeResultObject result = null;

			transaction.AddCommand(JObject.FromObject(new { my = "command" }), (FakeResultObject r) =>
			{
				handlerWasCalled = true;
				result = r;
			});

			transaction.ExecuteAsync().Wait();

			Assert.IsTrue(handlerWasCalled);
			Assert.IsNotNull(result);
			Assert.AreEqual(666, result.Data);
		}
	}
}