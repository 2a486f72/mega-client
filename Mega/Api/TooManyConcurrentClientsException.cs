namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if too many concurrent client IP addresses are uploading files at the same time.
	/// </summary>
	[Serializable]
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

		protected TooManyConcurrentClientsException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}