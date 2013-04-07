namespace Mega.Api
{
	using System;

	/// <summary>
	/// You are attempting to perform an operation on an item that does not exist.
	/// </summary>
	public class ItemNotFoundException : MegaException
	{
		public ItemNotFoundException()
		{
		}

		public ItemNotFoundException(string message) : base(message)
		{
		}

		public ItemNotFoundException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}