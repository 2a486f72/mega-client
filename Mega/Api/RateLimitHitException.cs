namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// You have hit your rate limit. Try again a bit later.
	/// </summary>
	[Serializable]
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

		protected RateLimitHitException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}