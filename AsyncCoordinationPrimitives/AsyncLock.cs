namespace AsyncCoordinationPrimitives
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public sealed class AsyncLock
	{
		private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
		private readonly Task<IDisposable> m_releaser;

		public AsyncLock()
		{
			m_releaser = Task.FromResult((IDisposable)new Releaser(this));
		}

		public Task<IDisposable> LockAsync()
		{
			var wait = m_semaphore.WaitAsync();
			return wait.IsCompleted ?
				m_releaser :
				wait.ContinueWith((_, state) => (IDisposable)state,
					m_releaser.Result, CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		public Task<IDisposable> TryLockAsync(TimeSpan timeout)
		{
			var wait = m_semaphore.WaitAsync(timeout);
			return wait.IsCompleted ?
				m_releaser :
				wait.ContinueWith((_, state) => _.Result ? (IDisposable)state : null,
					m_releaser.Result, CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		private sealed class Releaser : IDisposable
		{
			private readonly AsyncLock m_toRelease;

			internal Releaser(AsyncLock toRelease)
			{
				m_toRelease = toRelease;
			}

			public void Dispose()
			{
				m_toRelease.m_semaphore.Release();
			}
		}
	}
}