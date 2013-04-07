namespace Mega.Cryptography
{
	using System;

	public class CryptoException
		: Exception
	{
		public CryptoException()
		{
		}

		public CryptoException(
			string message)
			: base(message)
		{
		}

		public CryptoException(
			string message,
			Exception exception)
			: base(message, exception)
		{
		}
	}
}