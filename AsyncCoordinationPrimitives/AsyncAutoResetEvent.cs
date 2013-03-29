namespace AsyncCoordinationPrimitives
{
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public class AsyncAutoResetEvent
	{
		private readonly Queue<TaskCompletionSource<bool>> _waits = new Queue<TaskCompletionSource<bool>>();
		private bool _signaled;
		private AsyncLock _lock = new AsyncLock();

		public async Task WaitAsync()
		{
			TaskCompletionSource<bool> tcs;

			using (await _lock.LockAsync())
			{
				if (_signaled)
				{
					_signaled = false;
					return;
				}
				else
				{
					tcs = new TaskCompletionSource<bool>();
					_waits.Enqueue(tcs);
				}
			}

			if (tcs != null)
			{
				await tcs.Task;
			}
		}

		public async void Set()
		{
			TaskCompletionSource<bool> toRelease = null;

			using (await _lock.LockAsync())
			{
				if (_waits.Count > 0)
					toRelease = _waits.Dequeue();
				else if (!_signaled)
					_signaled = true;
			}

			if (toRelease != null)
				toRelease.SetResult(true);
		}
	}
}