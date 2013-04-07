namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if the upload link you are trying to use has expired.
	/// </summary>
	public class UploadExpiredException : MegaException
	{
		public UploadExpiredException()
		{
		}

		public UploadExpiredException(string message) : base(message)
		{
		}

		public UploadExpiredException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}