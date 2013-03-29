namespace AsyncCoordinationPrimitives
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public class AsyncCountdownEvent
	{
		private readonly AsyncManualResetEvent _amre = new AsyncManualResetEvent();
		private int _count;

		public AsyncCountdownEvent(int initialCount)
		{
			if (initialCount <= 0) throw new ArgumentOutOfRangeException("initialCount");
			_count = initialCount;
		}

		public async Task WaitAsync()
		{
			await _amre.WaitAsync();
		}

		public void Signal()
		{
			if (_count <= 0)
				throw new InvalidOperationException();

			int newCount = Interlocked.Decrement(ref _count);
			if (newCount == 0)
				_amre.Set();
			else if (newCount < 0)
				throw new InvalidOperationException();
		}
	}
}