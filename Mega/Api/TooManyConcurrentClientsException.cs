namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if too many concurrent client IP addresses are uploading files at the same time.
	/// </summary>
	public class TooManyConcurrentClientsException : MegaException
	{
		public TooManyConcurrentClientsException()
		{
		}

		public TooManyConcurrentClientsException(string message) : base(message)
		{
		}

		public TooManyConcurrentClientsException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}