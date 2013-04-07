namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if you have gone over your usage quota.
	/// </summary>
	public class QuotaExceededException : MegaException
	{
		public QuotaExceededException()
		{
		}

		public QuotaExceededException(string message) : base(message)
		{
		}

		public QuotaExceededException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}