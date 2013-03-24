namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if something goes seriously wrong on the Mega side.
	/// </summary>
	[Serializable]
	public class InternalServerErrorException : MegaException
	{
		public InternalServerErrorException()
		{
		}

		public InternalServerErrorException(string message) : base(message)
		{
		}

		public InternalServerErrorException(string message, Exception inner) : base(message, inner)
		{
		}

		protected InternalServerErrorException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}