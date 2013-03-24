namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if the upload link you are trying to use has expired.
	/// </summary>
	[Serializable]
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

		protected UploadExpiredException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}