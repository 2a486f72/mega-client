namespace Mega
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if there is a problem with data integrity - checksums did not match or expected data was not found.
	/// </summary>
	[Serializable]
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

		protected DataIntegrityException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}