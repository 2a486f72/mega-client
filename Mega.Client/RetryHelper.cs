namespace Mega.Client
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	internal static class RetryHelper
	{
		/// <summary>
		/// Executes an action, retrying according to a retry policy if it fails.
		/// </summary>
		public static Task ExecuteWithRetryAsync(Func<Task> function, RetryPolicy retryPolicy, IFeedbackChannel feedback, CancellationToken cancellationToken)
		{
			return ExecuteWithRetryAsync<object>(async delegate
			{
				await function();
				return null;
			}, retryPolicy, feedback, cancellationToken);
		}

		/// <summary>
		/// Executes an action, retrying according to a retry policy if it fails.
		/// </summary>
		public static async Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> function, RetryPolicy retryPolicy, IFeedbackChannel feedback, CancellationToken cancellationToken)
		{
			for (int retryCount = 0; retryCount <= retryPolicy.RetryIntervals.Count; retryCount++)
			{
				try
				{
					return await function();
				}
				catch (Exception ex)
				{
					// If the error cannot be retried, re-throw;
					if (!retryPolicy.RetryOnExceptions.Any(e => e.IsInstanceOfType(ex)))
						throw;

					feedback.WriteWarning(ex.Message);

					if (retryCount == retryPolicy.RetryIntervals.Count)
						throw;
				}

				feedback.Progress = null;
				feedback.Status = string.Format("Retrying in {0:N0} seconds", retryPolicy.RetryIntervals[retryCount].TotalSeconds);

				var stopwatch = Stopwatch.StartNew();

				while (stopwatch.Elapsed < retryPolicy.RetryIntervals[retryCount])
				{
					await Task.Delay(1000);

					cancellationToken.ThrowIfCancellationRequested();
				}

				feedback.Status = null;
			}

			throw new LogicException("Unreachable point.");
		}
	}
}