namespace Mega.Api
{
	using System;

	/// <summary>
	/// The request to Mega failed and we should try again later. Subclasses are defined for specific scenarios.
	/// </summary>
	public class TryAgainException : MegaException
	{
		public TryAgainException()
		{
		}

		public TryAgainException(string message) : base(message)
		{
		}

		public TryAgainException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}