namespace Mega
{
	using System;

	/// <summary>
	/// Thrown if there is a problem with data integrity - checksums did not match or expected data was not found.
	/// </summary>
	public class DataIntegrityException : MegaException
	{
		public DataIntegrityException()
		{
		}

		public DataIntegrityException(string message) : base(message)
		{
		}

		public DataIntegrityException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}