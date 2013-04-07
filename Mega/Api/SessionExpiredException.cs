namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if your Mega session has expired or does not exist. You should fire up a new session.
	/// </summary>
	public class SessionExpiredException : MegaException
	{
		public SessionExpiredException()
		{
		}

		public SessionExpiredException(string message) : base(message)
		{
		}

		public SessionExpiredException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}