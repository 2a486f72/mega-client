namespace Mega
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Something went wrong during an API request to Mega.
	/// Different subclasses are used for different scenarios.
	/// </summary>
	[Serializable]
	public class MegaException : Exception
	{
		public MegaException()
		{
		}

		public MegaException(string message) : base(message)
		{
		}

		public MegaException(string message, Exception inner) : base(message, inner)
		{
		}

		protected MegaException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}