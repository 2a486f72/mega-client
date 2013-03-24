namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if your account is blocked from Mega.
	/// </summary>
	[Serializable]
	public class BanhammerHasFallenException : MegaException
	{
		public BanhammerHasFallenException()
		{
		}

		public BanhammerHasFallenException(string message) : base(message)
		{
		}

		public BanhammerHasFallenException(string message, Exception inner) : base(message, inner)
		{
		}

		protected BanhammerHasFallenException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}