namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if you have gone over your usage quota.
	/// </summary>
	[Serializable]
	public class QuotaExceededException : MegaException
	{
		public QuotaExceededException()
		{
		}

		public QuotaExceededException(string message) : base(message)
		{
		}

		public QuotaExceededException(string message, Exception inner) : base(message, inner)
		{
		}

		protected QuotaExceededException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}