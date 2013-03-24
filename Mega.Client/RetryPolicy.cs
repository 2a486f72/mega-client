namespace Mega.Client
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Net;
	using System.Net.Http;
	using Api;
	using Useful;

	/// <summary>
	/// General purpose retry system configuration object.
	/// </summary>
	public sealed class RetryPolicy
	{
		/// <summary>
		/// Gets or sets the list of exceptions that should cause a failured operation to be retried.
		/// Any exception not in this list will be considered a fatal error and will not lead to a retry.
		/// </summary>
		public IReadOnlyCollection<Type> RetryOnExceptions { get; private set; }

		/// <summary>
		/// Gets the list of retry intervals. After a retriable failure, the retry helper
		/// will wait for this amoun of time before retrying the operation.
		/// </summary>
		public IReadOnlyList<TimeSpan> RetryIntervals { get; private set; }

		public RetryPolicy(ICollection<Type> retryOnExceptions, IList<TimeSpan> retryIntervals)
		{
			Argument.ValidateIsNotNull(retryOnExceptions, "retryOnExceptions");
			Argument.ValidateIsNotNull(retryIntervals, "retryIntervals");

			RetryOnExceptions = retryOnExceptions.ToImmutableList();
			RetryIntervals = retryIntervals.ToImmutableList();
		}

		/// <summary>
		/// You want to retry it immediately and several times but do not want to wait for whatever problem exists to get fixed.
		/// </summary>
		public static readonly IList<TimeSpan> FastAndAggressiveIntervals = new[]
		{
			TimeSpan.FromSeconds(1),
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(10),
		};

		/// <summary>
		/// You want to retry it after some time and at fairly large intervals.
		/// </summary>
		public static readonly IList<TimeSpan> CarefulIntervals = new[]
		{
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(30),
			TimeSpan.FromSeconds(60),
		};

		public static readonly ICollection<Type> ThirdPartyFaultExceptions = new[]
		{
			typeof(IOException),
			typeof(WebException),
			typeof(HttpRequestException),
			typeof(TryAgainException)
		};
	}
}