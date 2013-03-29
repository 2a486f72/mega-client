namespace AsyncCoordinationPrimitives
{
	using System.Threading;
	using System.Threading.Tasks;

	public class AsyncManualResetEvent
	{
		private volatile TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

		public async Task WaitAsync()
		{
			await _tcs.Task;
		}

		public async void Set()
		{
			var tcs = _tcs;
			await Task.Factory.StartNew(s =>
				((TaskCompletionSource<bool>)s).TrySetResult(true),
				tcs,
				CancellationToken.None,
				TaskCreationOptions.PreferFairness,
				TaskScheduler.Default);
		}

		public void Reset()
		{
			while (true)
			{
				var tcs = _tcs;
#pragma warning disable 420
				if (!tcs.Task.IsCompleted || Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
#pragma warning restore 420
					return;
			}
		}
	}
}