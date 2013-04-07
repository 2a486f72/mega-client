namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown if your account is blocked from Mega.
	/// </summary>
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
	}
}