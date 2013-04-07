namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if you attempt to create an item that already exists.
	/// </summary>
	public class AlreadyExistsException : MegaException
	{
		public AlreadyExistsException()
		{
		}

		public AlreadyExistsException(string message) : base(message)
		{
		}

		public AlreadyExistsException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}