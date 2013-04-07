namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if something goes seriously wrong on the Mega side.
	/// </summary>
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
	}
}