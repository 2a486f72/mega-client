namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	public class ItemTemporarilyUnavailableException : TryAgainException
	{
		public ItemTemporarilyUnavailableException()
		{
		}

		public ItemTemporarilyUnavailableException(string message) : base(message)
		{
		}

		public ItemTemporarilyUnavailableException(string message, Exception inner) : base(message, inner)
		{
		}

		protected ItemTemporarilyUnavailableException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}