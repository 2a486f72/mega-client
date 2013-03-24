namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown when you attempt to perform an operation that you are not allowed to perform (e.g. write to a read-only share).
	/// </summary>
	[Serializable]
	public class AccessDeniedException : MegaException
	{
		public AccessDeniedException()
		{
		}

		public AccessDeniedException(string message) : base(message)
		{
		}

		public AccessDeniedException(string message, Exception inner) : base(message, inner)
		{
		}

		protected AccessDeniedException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}