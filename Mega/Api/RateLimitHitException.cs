namespace Mega.Api
{
	using System;

	/// <summary>
	/// You have hit your rate limit. Try again a bit later.
	/// </summary>
	public class RateLimitHitException : TryAgainException
	{
		public RateLimitHitException()
		{
		}

		public RateLimitHitException(string message) : base(message)
		{
		}

		public RateLimitHitException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}