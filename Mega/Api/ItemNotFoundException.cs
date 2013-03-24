namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// You are attempting to perform an operation on an item that does not exist.
	/// </summary>
	[Serializable]
	public class ItemNotFoundException : MegaException
	{
		public ItemNotFoundException()
		{
		}

		public ItemNotFoundException(string message) : base(message)
		{
		}

		public ItemNotFoundException(string message, Exception inner) : base(message, inner)
		{
		}

		protected ItemNotFoundException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}