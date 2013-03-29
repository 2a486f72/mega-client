namespace AsyncCoordinationPrimitives
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public class AsyncSemaphore
	{
		private readonly Queue<TaskCompletionSource<bool>> _waiters = new Queue<TaskCompletionSource<bool>>();
		private int _currentCount;
		private AsyncLock _lock = new AsyncLock();

		public AsyncSemaphore(int initialCount)
		{
			if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
			_currentCount = initialCount;
		}

		public async Task WaitAsync()
		{
			TaskCompletionSource<bool> tcs;

			using (await _lock.LockAsync())
			{
				if (_currentCount > 0)
				{
					--_currentCount;
					return;
				}
				else
				{
					tcs = new TaskCompletionSource<bool>();
					_waiters.Enqueue(tcs);
				}
			}

			if (tcs != null)
			{
				await tcs.Task;
			}
		}

		public async void Release()
		{
			TaskCompletionSource<bool> toRelease = null;
			using (await _lock.LockAsync())
			{
				if (_waiters.Count > 0)
					toRelease = _waiters.Dequeue();
				else
					++_currentCount;
			}
			if (toRelease != null)
				toRelease.SetResult(true);
		}
	}
}