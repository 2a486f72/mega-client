namespace Mega
{
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;

	public static class HttpClientExtensions
	{
		/// <summary>
		/// Default GetAsync can throw TaskCanceledException for random internal tasks, which is stupid. This one does not.
		/// </summary>
		public static async Task<HttpResponseMessage> GetAsyncCancellationSafe(this HttpClient instance, string url, CancellationToken cancellationToken)
		{
			try
			{
				return await instance.GetAsync(url, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Some inner garbage exception. Replace with better generic one.
				throw new HttpRequestException("Failed to perform HTTP request.");
			}
		}

		/// <summary>
		/// Default PostAsync can throw TaskCanceledException for random internal tasks, which is stupid. This one does not.
		/// </summary>
		public static async Task<HttpResponseMessage> PostAsyncCancellationSafe(this HttpClient instance, string url, HttpContent content, CancellationToken cancellationToken)
		{
			try
			{
				return await instance.PostAsync(url, content, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Some inner garbage exception. Replace with better generic one.
				throw new HttpRequestException("Failed to perform HTTP request.");
			}
		}

		/// <summary>
		/// Default PostAsJsonAsync can throw TaskCanceledException for random internal tasks, which is stupid. This one does not.
		/// </summary>
		public static async Task<HttpResponseMessage> PostAsJsonAsyncCancellationSafe<TContent>(this HttpClient instance, string url, TContent content, CancellationToken cancellationToken)
		{
			try
			{
				return await instance.PostAsJsonAsync(url, content, cancellationToken);
			}
			catch (TaskCanceledException)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Some inner garbage exception. Replace with better generic one.
				throw new HttpRequestException("Failed to perform HTTP request.");
			}
		}
	}
}