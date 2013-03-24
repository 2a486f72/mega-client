namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Raised when something goes very wrong and the entire upload must be restarted.
	/// </summary>
	[Serializable]
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

		protected CriticalUploadFailureException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}