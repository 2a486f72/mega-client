namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// A request made to Mega was not correctly formatted. Some data was probably missing or invalid.
	/// </summary>
	[Serializable]
	public class InvalidRequestException : MegaException
	{
		public InvalidRequestException()
		{
		}

		public InvalidRequestException(string message) : base(message)
		{
		}

		public InvalidRequestException(string message, Exception inner) : base(message, inner)
		{
		}

		protected InvalidRequestException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}