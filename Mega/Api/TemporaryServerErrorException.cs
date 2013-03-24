namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Something is wrong on the Mega side but might get better soon. Let's try again.
	/// </summary>
	[Serializable]
	public class TemporaryServerErrorException : TryAgainException
	{
		public TemporaryServerErrorException()
		{
		}

		public TemporaryServerErrorException(string message) : base(message)
		{
		}

		public TemporaryServerErrorException(string message, Exception inner) : base(message, inner)
		{
		}

		protected TemporaryServerErrorException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}