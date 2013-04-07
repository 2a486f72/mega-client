namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if the item you are accessing is incomplete.
	/// </summary>
	public class IncompleteItemException : MegaException
	{
		public IncompleteItemException()
		{
		}

		public IncompleteItemException(string message) : base(message)
		{
		}

		public IncompleteItemException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}