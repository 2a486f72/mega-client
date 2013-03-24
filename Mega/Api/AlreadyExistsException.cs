namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if you attempt to create an item that already exists.
	/// </summary>
	[Serializable]
	public class AlreadyExistsException : MegaException
	{
		public AlreadyExistsException()
		{
		}

		public AlreadyExistsException(string message) : base(message)
		{
		}

		public AlreadyExistsException(string message, Exception inner) : base(message, inner)
		{
		}

		protected AlreadyExistsException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}