namespace Mega
{
	using System;

	/// <summary>
	/// Something went wrong during an API request to Mega.
	/// Different subclasses are used for different scenarios.
	/// </summary>
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
	}
}