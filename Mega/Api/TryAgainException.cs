namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The request to Mega failed and we should try again later. Subclasses are defined for specific scenarios.
	/// </summary>
	[Serializable]
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

		protected TryAgainException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}