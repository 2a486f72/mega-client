namespace Mega.Api
{
	using System;

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
	}
}