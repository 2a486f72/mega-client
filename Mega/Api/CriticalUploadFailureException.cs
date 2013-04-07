namespace Mega.Api
{
	using System;

	/// <summary>
	/// Raised when something goes very wrong and the entire upload must be restarted.
	/// </summary>
	public class CriticalUploadFailureException : MegaException
	{
		public CriticalUploadFailureException()
		{
		}

		public CriticalUploadFailureException(string message) : base(message)
		{
		}

		public CriticalUploadFailureException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}