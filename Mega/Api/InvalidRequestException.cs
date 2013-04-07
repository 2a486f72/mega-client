namespace Mega.Api
{
	using System;

	/// <summary>
	/// A request made to Mega was not correctly formatted. Some data was probably missing or invalid.
	/// </summary>
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
	}
}